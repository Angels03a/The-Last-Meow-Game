using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private readonly Dictionary<Guid, MatchRecord> _matchesById = new Dictionary<Guid, MatchRecord>();

        private const int InitialMatchVersion = 1;

        private static readonly IReadOnlyList<MatchViewDelivery> _noMatchViewDeliveries =
            new List<MatchViewDelivery>().AsReadOnly();

        internal MatchSnapshot FindMatchOfParticipant(Guid matchId, int userId)
        {
            MatchSnapshot matchSnapshot = null;

            lock (_sessionsLock)
            {
                MatchRecord matchRecord;

                if (_matchesById.TryGetValue(matchId, out matchRecord)
                    && matchRecord.ParticipantUserIds.Contains(userId))
                {
                    matchSnapshot = CreateMatchSnapshotInLock(matchRecord);
                }
            }

            return matchSnapshot;
        }

        public ResultWithNotification<OperationRecoveryContextResult> GetRecoveryContext(string sessionToken)
        {
            OperationRecoveryContextResult recoveryContextResult = new OperationRecoveryContextResult
            {
                ErrorKey = TLMG.Domain.Constants.ErrorKeys.Common.InvalidSession
            };
            List<Action> notifications = new List<Action>();

            lock (_sessionsLock)
            {
                DateTime nowUtc = _readUtcNow();
                if (!string.IsNullOrEmpty(sessionToken) && _sessionsByToken.ContainsKey(sessionToken))
                {
                    EvaluateHeartbeatDeadlinesInLock(nowUtc, notifications, true);
                }

                if (!string.IsNullOrEmpty(sessionToken)
                    && _sessionsByToken.TryGetValue(sessionToken, out PlayerSession session))
                {
                    session.LastActivityUtc = nowUtc;
                    recoveryContextResult.IsSuccess = true;
                    recoveryContextResult.ErrorKey = string.Empty;
                    MatchRecord match = FindRecoverableMatchInLock(session.UserId);
                    if (match != null)
                    {
                        recoveryContextResult.RoomId = match.RoomId;
                        recoveryContextResult.MatchId = match.MatchId;
                    }
                }
            }

            PersistPendingMatchFinalizations();
            ResultWithNotification<OperationRecoveryContextResult> recoveryContext =
                new ResultWithNotification<OperationRecoveryContextResult>(recoveryContextResult, notifications.AsReadOnly());

            return recoveryContext;
        }

        private const int ReconnectGraceSeconds = 15;
        private const int FirstMatchLinkSeconds = 9;

        private bool ExpireMatchDeadlinesInLock(MatchRecord match, DateTime nowUtc)
        {
            bool hasChanged = false;
            if (match.AwaitingFirstLink.Count != 0 && match.FirstLinkDeadlineUtc <= nowUtc)
            {
                hasChanged |= ExpireGameDeadlinesInLock(match, match.FirstLinkDeadlineUtc);
                foreach (int userId in match.AwaitingFirstLink)
                {
                    hasChanged |= OpenMatchGraceInLock(match, userId, match.FirstLinkDeadlineUtc);
                }
                match.AwaitingFirstLink.Clear();
            }

            IGrouping<DateTime, KeyValuePair<int, PlayerExit>>[] expiredGroups =
                (from disconnection in match.Disconnections
                 where disconnection.Value.EffectiveAtUtc <= nowUtc
                 group disconnection by disconnection.Value.EffectiveAtUtc into expiredGroup
                 orderby expiredGroup.Key
                 select expiredGroup).ToArray();

            foreach (IGrouping<DateTime, KeyValuePair<int, PlayerExit>> expiredGroup in expiredGroups)
            {
                ExpireDisconnectedPlayersInLock(match, expiredGroup);
                hasChanged = true;
            }

            hasChanged = ExpireGameDeadlinesInLock(match, nowUtc) || hasChanged;

            return hasChanged;
        }

        private void ExpireDisconnectedPlayersInLock(MatchRecord match,
            IGrouping<DateTime, KeyValuePair<int, PlayerExit>> expiredGroup)
        {
            const int SingleExitCount = 1;
            ExpireGameDeadlinesInLock(match, expiredGroup.Key);
            Dictionary<int, PlayerExit> activePlayerExits = new Dictionary<int, PlayerExit>();

            foreach (KeyValuePair<int, PlayerExit> disconnection in expiredGroup)
            {
                bool hasActivePlayer = (from player in match.MatchState.Players
                                        where player.PlayerId == disconnection.Key && player.IsInPlay
                                        select player).Any();
                if (hasActivePlayer)
                {
                    activePlayerExits.Add(disconnection.Key, disconnection.Value);
                }

                match.Disconnections.Remove(disconnection.Key);
            }

            if (activePlayerExits.Count != EmptyCount && match.MatchState.Phase != RoundPhase.MatchFinished)
            {
                PlayerExitStatus exitStatus;

                if (activePlayerExits.Count == SingleExitCount)
                {
                    KeyValuePair<int, PlayerExit> activePlayerExit = activePlayerExits.Single();
                    exitStatus = match.MatchState.RemovePlayer(activePlayerExit.Key, activePlayerExit.Value, _pickRandomIndex);
                }
                else
                {
                    exitStatus = match.MatchState.RemoveSimultaneousPlayers(activePlayerExits, _pickRandomIndex);
                }
                UpdateChallengeAfterExit(match, exitStatus);
                DiscardEndedBlindSlots(match);
            }

            UpdateKittenClockInLock(match, expiredGroup.Key);
        }

        private static bool OpenMatchGraceInLock(MatchRecord match, int userId, DateTime nowUtc)
        {
            bool wasGraceOpened = match.MatchState.Phase != RoundPhase.MatchFinished
                && !match.Disconnections.ContainsKey(userId)
                && (from player in match.MatchState.Players
                    where player.PlayerId == userId && player.IsInPlay
                    select player).Any();

            if (wasGraceOpened)
            {
                match.Disconnections.Add(userId, PlayerExit.CreateDisconnection(nowUtc, nowUtc.AddSeconds(ReconnectGraceSeconds)));
                match.MatchState.SuspendPunishment(userId, nowUtc);
                UpdateKittenClockInLock(match, nowUtc);
            }

            return wasGraceOpened;
        }

        private static bool UpdateKittenClockInLock(MatchRecord match, DateTime nowUtc)
        {
            bool hasChanged = match.Kitten != null && match.MatchState.Phase != RoundPhase.MatchFinished
                && match.Kitten.SetPaused(match.AwaitingFirstLink.Count != 0 || match.Disconnections.Count != 0,
                    nowUtc);

            return hasChanged;
        }

        private static void DiscardEndedBlindSlots(MatchRecord matchRecord)
        {
            if (matchRecord.MatchState.PendingPunishment == null)
            {
                matchRecord.BlindSlotOrder = null;
            }
        }

        private static bool HasExitChangedMatch(PlayerExitStatus exitStatus)
        {
            bool hasChanged = exitStatus == PlayerExitStatus.RoundRestarted
                || exitStatus == PlayerExitStatus.MatchFinished
                || exitStatus == PlayerExitStatus.PunishmentCancelled
                || exitStatus == PlayerExitStatus.PunishmentResolvedAtRandom
                || exitStatus == PlayerExitStatus.ExitRecorded;

            return hasChanged;
        }

        private static void UpdateChallengeAfterExit(MatchRecord matchRecord, PlayerExitStatus exitStatus)
        {
            MatchChallengeRecord challenge = matchRecord.LastChallenge;
            bool endsRound = exitStatus == PlayerExitStatus.RoundRestarted
                || exitStatus == PlayerExitStatus.MatchFinished;

            if (challenge != null)
            {
                if (challenge.Outcome == MatchChallengeOutcome.InProgress && endsRound)
                {
                    matchRecord.LastChallenge = null;
                }
                else if (challenge.PunishmentResolution == MatchPunishmentResolution.Pending)
                {
                    ResolvePendingPunishmentAfterExit(challenge, exitStatus);
                }
            }
        }

        private static void ResolvePendingPunishmentAfterExit(
            MatchChallengeRecord challenge,
            PlayerExitStatus exitStatus)
        {
            if (exitStatus == PlayerExitStatus.PunishmentResolvedAtRandom)
            {
                challenge.ResolvePunishment(MatchPunishmentResolution.ChosenAtRandom);
            }
            else if (exitStatus == PlayerExitStatus.PunishmentCancelled
                || exitStatus == PlayerExitStatus.MatchFinished)
            {
                challenge.ResolvePunishment(MatchPunishmentResolution.Cancelled);
            }
        }

        private static MatchSnapshot CreateMatchSnapshotInLock(MatchRecord matchRecord)
        {
            MatchState matchState = matchRecord.MatchState;
            MatchSnapshot matchSnapshot = new MatchSnapshot(
                matchRecord.MatchId,
                matchRecord.RoomId,
                matchRecord.ParticipantUserIds,
                matchState.ModeRules.CardsPerPlayerCount,
                matchState.RoundNumber,
                matchState.Phase,
                matchState.PlayerInTurn.PlayerId,
                matchRecord.GuestUserIds);

            return matchSnapshot;
        }

        private void ReleaseMatchOfRemovedRoomInLock(LobbyStartState removedStart)
        {
            if (removedStart.MatchId.HasValue)
            {
                MatchRecord matchRecord;

                if (_matchesById.TryGetValue(removedStart.MatchId.Value, out matchRecord))
                {
                    QueueMatchFinalizationInLock(matchRecord, true);
                    _matchesById.Remove(removedStart.MatchId.Value);
                }
            }
        }

        private MatchRecord FindRecoverableMatchInLock(int userId)
        {
            MatchRecord recoverableMatch = null;

            if (_roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId is Guid matchId
                && _matchesById.TryGetValue(matchId, out MatchRecord match))
            {
                bool hasRecoverablePlayer = (from player in match.MatchState.Players
                                             where player.PlayerId == userId && !player.HasLeftMatch
                                             select player).Any();
                if (hasRecoverablePlayer)
                {
                    recoverableMatch = match;
                }
            }

            return recoverableMatch;
        }

        private bool HasActiveParticipationInLock(int userId)
        {
            bool hasActiveParticipation = _roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId is Guid matchId
                && _matchesById.TryGetValue(matchId, out MatchRecord match)
                && match.MatchState.Phase != RoundPhase.MatchFinished
                && (from player in match.MatchState.Players
                    where player.PlayerId == userId && player.IsInPlay
                    select player).Any();

            return hasActiveParticipation;
        }

        private bool ExpireGameDeadlinesInLock(MatchRecord matchRecord, DateTime nowUtc)
        {
            PunishmentStatus punishmentStatus = matchRecord.MatchState.ExpirePunishment(nowUtc, _pickRandomIndex);
            bool wasExpired = punishmentStatus == PunishmentStatus.CardRemoved;

            if (wasExpired && matchRecord.LastChallenge != null)
            {
                matchRecord.LastChallenge.ResolvePunishment(MatchPunishmentResolution.ChosenAtRandom);
            }

            if (matchRecord.Kitten != null && matchRecord.Kitten.Expire(nowUtc))
            {
                wasExpired = true;
            }

            DiscardEndedBlindSlots(matchRecord);

            return wasExpired;
        }

        private IReadOnlyList<MatchViewDelivery> ApplyImmediateMatchExitInLock(int userId, Guid matchId, PlayerExit playerExit)
        {
            IReadOnlyList<MatchViewDelivery> viewDeliveries = _noMatchViewDeliveries;
            MatchRecord matchRecord;

            if (_matchesById.TryGetValue(matchId, out matchRecord))
            {
                DateTime nowUtc = playerExit.EffectiveAtUtc;
                bool hasChanged = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
                PlayerExitStatus exitStatus = matchRecord.MatchState.RemovePlayer(
                    userId, playerExit, _pickRandomIndex);
                matchRecord.AwaitingFirstLink.Remove(userId);
                hasChanged |= matchRecord.Disconnections.Remove(userId);
                hasChanged |= UpdateKittenClockInLock(matchRecord, nowUtc);
                hasChanged = hasChanged || HasExitChangedMatch(exitStatus);
                UpdateChallengeAfterExit(matchRecord, exitStatus);
                DiscardEndedBlindSlots(matchRecord);

                if (hasChanged)
                {
                    matchRecord.Version++;
                    viewDeliveries = CreateViewDeliveriesInLock(matchRecord);
                }
            }

            return viewDeliveries;
        }

        private MatchRecord CreateMatchRecordInLock(Guid matchId, int roomId, DateTime nowUtc)
        {
            int cardsPerPlayerCount = _startsByRoomId[roomId].CardsPerPlayerCount;
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            IReadOnlyList<int> participantUserIds =
                (from member in _membersByRoomId[roomId]
                 select member.UserId).ToList().AsReadOnly();
            IReadOnlyList<int> guestUserIds = participantUserIds
                .Where(GuestIdentity.IsGuestUserId)
                .ToList()
                .AsReadOnly();
            MatchState matchState = new MatchState(modeRules, participantUserIds);
            MatchRecord matchRecord = new MatchRecord(
                matchId,
                roomId,
                matchState,
                participantUserIds,
                cardsPerPlayerCount,
                CopyNicknamesInLock(participantUserIds),
                guestUserIds,
                nowUtc);

            return matchRecord;
        }

        private Dictionary<int, string> CopyNicknamesInLock(IReadOnlyList<int> participantUserIds)
        {
            Dictionary<int, string> nicknamesByUserId = new Dictionary<int, string>();

            foreach (int userId in participantUserIds)
            {
                nicknamesByUserId[userId] = _sessionsByToken[_tokensByUserId[userId]].Nickname;
            }

            return nicknamesByUserId;
        }

        private sealed class MatchRecord
        {
            public MatchRecord(
                Guid matchId,
                int roomId,
                MatchState matchState,
                IReadOnlyList<int> participantUserIds,
                int cardsPerPlayerCount,
                IReadOnlyDictionary<int, string> participantNicknames,
                IReadOnlyList<int> guestUserIds,
                DateTime nowUtc)
            {
                MatchId = matchId;
                RoomId = roomId;
                MatchState = matchState;
                ParticipantUserIds = participantUserIds;
                CardsPerPlayerCount = cardsPerPlayerCount;
                ParticipantNicknames = participantNicknames;
                GuestUserIds = guestUserIds;
                Version = InitialMatchVersion;
                FirstLinkDeadlineUtc = nowUtc.AddSeconds(FirstMatchLinkSeconds);
                AwaitingFirstLink = new HashSet<int>(participantUserIds);

                if (matchState.ModeRules.AllowsExtraLife)
                {
                    Kitten = new FugitiveKittenState(nowUtc);
                }
            }

            public Guid MatchId { get; }

            public int RoomId { get; }

            public MatchState MatchState { get; }

            public IReadOnlyList<int> ParticipantUserIds { get; }

            public int CardsPerPlayerCount { get; }

            public IReadOnlyDictionary<int, string> ParticipantNicknames { get; }

            public IReadOnlyList<int> GuestUserIds { get; }

            public FugitiveKittenState Kitten { get; }

            public Dictionary<int, PlayerExit> Disconnections { get; } = new Dictionary<int, PlayerExit>();

            public DateTime FirstLinkDeadlineUtc { get; }

            public HashSet<int> AwaitingFirstLink { get; }

            public int Version { get; set; }

            public bool HasQueuedFinalization { get; set; }

            public MatchChallengeRecord LastChallenge { get; set; }

            public int[] BlindSlotOrder { get; set; }
        }
    }
}
