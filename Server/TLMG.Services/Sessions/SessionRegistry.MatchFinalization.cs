using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private static readonly ILog _matchFinalizationLog = LogManager.GetLogger(typeof(SessionRegistry));
        private readonly object _matchFinalizationLock = new object();
        private readonly Dictionary<Guid, GameFinalization> _pendingMatchFinalizations =
            new Dictionary<Guid, GameFinalization>();
        private IGameHistoryRepository _gameHistoryRepository;

        public void ConfigureMatchHistory(IGameHistoryRepository gameHistoryRepository)
        {
            _gameHistoryRepository = gameHistoryRepository;
        }

        private void QueueMatchFinalizationInLock(MatchRecord matchRecord, bool isRoomRemoved = false)
        {
            bool isMatchFinished = matchRecord.MatchState.Phase == RoundPhase.MatchFinished;

            if (!matchRecord.HasQueuedFinalization && (isMatchFinished || isRoomRemoved))
            {
                GameFinalization gameFinalization = new GameFinalization
                {
                    MatchId = matchRecord.MatchId,
                    EndedAtUtc = _readUtcNow(),
                    IsAbandoned = !isMatchFinished || matchRecord.MatchState.Winner == null,
                    Participants = Array.Empty<GameParticipantRecord>()
                };

                if (TryPopulateParticipantRecords(matchRecord, gameFinalization))
                {
                    _pendingMatchFinalizations.Add(gameFinalization.MatchId, gameFinalization);
                    matchRecord.HasQueuedFinalization = true;
                }
                else
                {
                    _matchFinalizationLog.ErrorFormat("Invalid final standings for match {0}.", matchRecord.MatchId);
                }
            }
        }

        private static bool TryPopulateParticipantRecords(MatchRecord matchRecord, GameFinalization gameFinalization)
        {
            bool hasValidRecords = false;

            if (gameFinalization.IsAbandoned)
            {
                hasValidRecords = TryBuildParticipantRecords(matchRecord, gameFinalization, Array.Empty<PlayerStanding>());
            }
            else
            {
                FinalStandings finalStandings = FinalStandings.CalculateFromMatch(
                    matchRecord.MatchState, matchRecord.GuestUserIds);

                if (finalStandings != null && finalStandings.Status == FinalStandingsStatus.Calculated
                    && finalStandings.Standings != null)
                {
                    hasValidRecords = TryBuildParticipantRecords(matchRecord, gameFinalization, finalStandings.Standings);
                }
            }

            return hasValidRecords;
        }

        private static bool TryBuildParticipantRecords(MatchRecord matchRecord,
            GameFinalization gameFinalization, IEnumerable<PlayerStanding> standings)
        {
            List<GameParticipantRecord> participantRecords = new List<GameParticipantRecord>();
            List<MatchPlayer> registeredPlayers = (from player in matchRecord.MatchState.Players
                                                   where !matchRecord.GuestUserIds.Contains(player.PlayerId)
                                                   select player).ToList();
            bool hasValidRecords = true;

            for (int playerIndex = 0; hasValidRecords && playerIndex < registeredPlayers.Count; playerIndex++)
            {
                MatchPlayer player = registeredPlayers[playerIndex];
                PlayerStanding standing = null;

                if (!gameFinalization.IsAbandoned)
                {
                    standing = (from finalStanding in standings
                                where finalStanding.PlayerId == player.PlayerId
                                select finalStanding).FirstOrDefault();
                }

                GameParticipantFinalStatus finalStatus = ResolveParticipantFinalStatus(
                    matchRecord.MatchState, player, gameFinalization.IsAbandoned);
                hasValidRecords = (gameFinalization.IsAbandoned || standing != null)
                    && (!player.HasLeftMatch || finalStatus != GameParticipantFinalStatus.Unclassified);

                if (hasValidRecords)
                {
                    participantRecords.Add(CreateParticipantRecord(player, standing, finalStatus));
                }
            }

            if (hasValidRecords)
            {
                gameFinalization.Participants = participantRecords.AsReadOnly();
            }

            return hasValidRecords;
        }

        private static GameParticipantRecord CreateParticipantRecord(MatchPlayer player, PlayerStanding standing,
            GameParticipantFinalStatus finalStatus)
        {
            GameParticipantRecord participantRecord = new GameParticipantRecord
            {
                UserId = player.PlayerId,
                Points = ValidationRules.InitialStatisticValue,
                FinalStatus = finalStatus
            };

            if (standing != null)
            {
                participantRecord.Position = standing.Position;
                participantRecord.Points = standing.Points;
            }

            if (player.HasLeftMatch)
            {
                participantRecord.LeftAtUtc = player.Exit.EffectiveAtUtc;
            }

            return participantRecord;
        }

        private static GameParticipantFinalStatus ResolveParticipantFinalStatus(MatchState matchState,
            MatchPlayer player, bool isAbandoned)
        {
            GameParticipantFinalStatus participantFinalStatus = GameParticipantFinalStatus.Unclassified;

            if (player.HasLeftMatch)
            {
                PlayerExitCause cause = player.Exit.Cause;

                if (cause == PlayerExitCause.Voluntary)
                {
                    participantFinalStatus = GameParticipantFinalStatus.Abandoned;
                }
                else if (cause == PlayerExitCause.Disconnection || cause == PlayerExitCause.VotedOut
                    || cause == PlayerExitCause.Sanctioned)
                {
                    participantFinalStatus = GameParticipantFinalStatus.Kicked;
                }
            }
            else if (!isAbandoned)
            {
                if (ReferenceEquals(matchState.Winner, player))
                {
                    participantFinalStatus = GameParticipantFinalStatus.Winner;
                }
                else if (player.IsEliminated)
                {
                    participantFinalStatus = GameParticipantFinalStatus.Eliminated;
                }
                else
                {
                    participantFinalStatus = GameParticipantFinalStatus.Survivor;
                }
            }

            return participantFinalStatus;
        }

        private void PersistPendingMatchFinalizations()
        {
            bool hasPendingFinalizations;

            lock (_sessionsLock)
            {
                hasPendingFinalizations = _pendingMatchFinalizations.Count > EmptyCount;
            }

            if (hasPendingFinalizations)
            {
                if (_gameHistoryRepository == null)
                {
                    _matchFinalizationLog.Error("Match history repository is not configured.");
                }
                else
                {
                    lock (_matchFinalizationLock)
                    {
                        List<GameFinalization> pendingFinalizations;

                        lock (_sessionsLock)
                        {
                            pendingFinalizations = (from finalization in _pendingMatchFinalizations.Values
                                                    orderby finalization.EndedAtUtc, finalization.MatchId
                                                    select finalization).ToList();
                        }

                        bool canContinue = true;

                        for (int finalizationIndex = 0;
                            canContinue && finalizationIndex < pendingFinalizations.Count;
                            finalizationIndex++)
                        {
                            canContinue = PersistMatchFinalization(pendingFinalizations[finalizationIndex]);
                        }
                    }
                }
            }
        }

        private bool PersistMatchFinalization(GameFinalization gameFinalization)
        {
            GameFinalizationStatus finalizationStatus = _gameHistoryRepository.FinalizeMatch(gameFinalization);
            bool canContinue = false;

            if (finalizationStatus == GameFinalizationStatus.Saved
                || finalizationStatus == GameFinalizationStatus.AlreadySaved)
            {
                lock (_sessionsLock)
                {
                    _pendingMatchFinalizations.Remove(gameFinalization.MatchId);
                }

                canContinue = true;
            }
            else if (finalizationStatus == GameFinalizationStatus.InvalidFinalization
                || finalizationStatus == GameFinalizationStatus.FinalizationConflict)
            {
                _matchFinalizationLog.ErrorFormat("Match finalization rejected for {0}: {1}.",
                    gameFinalization.MatchId, finalizationStatus);
                canContinue = true;
            }
            else
            {
                _matchFinalizationLog.ErrorFormat("Match finalization could not be saved for {0}: {1}.",
                    gameFinalization.MatchId, finalizationStatus);
            }

            return canContinue;
        }
    }
}