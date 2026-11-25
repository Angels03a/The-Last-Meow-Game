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
                bool isAbandoned = !isMatchFinished || matchRecord.MatchState.Winner == null;
                IReadOnlyList<GameParticipantRecord> participantRecords =
                    CreateParticipantRecordsInLock(matchRecord, isAbandoned);

                if (participantRecords != null)
                {
                    GameFinalization gameFinalization = new GameFinalization
                    {
                        MatchId = matchRecord.MatchId,
                        EndedAtUtc = _readUtcNow(),
                        IsAbandoned = isAbandoned,
                        Participants = participantRecords
                    };

                    _pendingMatchFinalizations.Add(gameFinalization.MatchId, gameFinalization);
                    matchRecord.HasQueuedFinalization = true;
                }
            }
        }

        private static IReadOnlyList<GameParticipantRecord> CreateParticipantRecordsInLock(
            MatchRecord matchRecord, bool isAbandoned)
        {
            IReadOnlyList<GameParticipantRecord> registeredParticipantRecords = null;
            FinalStandings finalStandings = null;

            if (!isAbandoned)
            {
                finalStandings = FinalStandings.CalculateFromMatch(matchRecord.MatchState, matchRecord.GuestUserIds);
            }

            if (isAbandoned || finalStandings.Status == FinalStandingsStatus.Calculated)
            {
                registeredParticipantRecords = BuildParticipantRecords(matchRecord, finalStandings);
            }

            if (registeredParticipantRecords == null)
            {
                _matchFinalizationLog.ErrorFormat("Invalid final standings for match {0}.", matchRecord.MatchId);
            }

            return registeredParticipantRecords;
        }

        private static IReadOnlyList<GameParticipantRecord> BuildParticipantRecords(
            MatchRecord matchRecord, FinalStandings finalStandings)
        {
            List<GameParticipantRecord> participantRecords = new List<GameParticipantRecord>();
            bool isAbandoned = finalStandings == null;
            bool hasValidStandings = true;
            IEnumerable<MatchPlayer> registeredPlayers = from player in matchRecord.MatchState.Players
                                                         where !matchRecord.GuestUserIds.Contains(player.PlayerId)
                                                         select player;

            foreach (MatchPlayer player in registeredPlayers)
            {
                PlayerStanding standing = null;

                if (!isAbandoned)
                {
                    standing = (from finalStanding in finalStandings.Standings
                                where finalStanding.PlayerId == player.PlayerId
                                select finalStanding).FirstOrDefault();
                }

                if (!isAbandoned && standing == null)
                {
                    hasValidStandings = false;
                    break;
                }

                GameParticipantRecord participantRecord = CreateParticipantRecord(matchRecord.MatchState, player, standing);

                participantRecords.Add(participantRecord);
            }

            IReadOnlyList<GameParticipantRecord> registeredParticipantRecords = hasValidStandings
                ? participantRecords.AsReadOnly() : null;

            return registeredParticipantRecords;
        }

        private static GameParticipantRecord CreateParticipantRecord(MatchState matchState, MatchPlayer player,
            PlayerStanding standing)
        {
            GameParticipantRecord participantRecord = new GameParticipantRecord
            {
                UserId = player.PlayerId,
                Position = standing != null ? (int?)standing.Position : null,
                Points = standing != null ? standing.Points : ValidationRules.InitialStatisticValue,
                FinalStatus = ResolveParticipantFinalStatus(matchState, player, standing == null),
                LeftAtUtc = player.HasLeftMatch ? (DateTime?)player.Exit.EffectiveAtUtc : null
            };

            return participantRecord;
        }

        private static GameParticipantFinalStatus ResolveParticipantFinalStatus(MatchState matchState,
            MatchPlayer player, bool isAbandoned)
        {
            GameParticipantFinalStatus participantFinalStatus = GameParticipantFinalStatus.Unclassified;

            if (player.HasLeftMatch)
            {
                participantFinalStatus = player.Exit.EffectiveAtUtc > player.Exit.StartedAtUtc
                    ? GameParticipantFinalStatus.Kicked : GameParticipantFinalStatus.Abandoned;
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
                hasPendingFinalizations = _pendingMatchFinalizations.Count > 0;
            }

            if (_gameHistoryRepository != null && hasPendingFinalizations)
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

                    foreach (GameFinalization gameFinalization in pendingFinalizations)
                    {
                        GameFinalizationStatus finalizationStatus = _gameHistoryRepository.FinalizeMatch(gameFinalization);
                        if (finalizationStatus == GameFinalizationStatus.Saved
                            || finalizationStatus == GameFinalizationStatus.AlreadySaved)
                        {
                            lock (_sessionsLock)
                            {
                                _pendingMatchFinalizations.Remove(gameFinalization.MatchId);
                            }
                        }
                        else if (finalizationStatus == GameFinalizationStatus.InvalidFinalization
                            || finalizationStatus == GameFinalizationStatus.FinalizationConflict)
                        {
                            _matchFinalizationLog.ErrorFormat("Match finalization rejected for {0}: {1}.",
                                gameFinalization.MatchId, finalizationStatus);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
