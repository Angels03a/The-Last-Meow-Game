using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        /// <summary>
        /// Authorized matches by MatchId. Keeps them while their room exists even when members leave.
        /// ReleaseMatchOfRemovedRoomInLock releases them when the room is removed.
        /// </summary>
        private readonly Dictionary<Guid, MatchRecord> _matchesById = new Dictionary<Guid, MatchRecord>();

        /// <summary>First match view version. A higher version always means a newer view.</summary>
        private const int InitialMatchVersion = 1;

        private static readonly IReadOnlyList<MatchViewDelivery> _noMatchViewDeliveries =
            new List<MatchViewDelivery>().AsReadOnly();

        /// <summary>
        /// Copies basic state for a player who belonged to the match at start. MatchState stays under the lock. Initial
        /// membership alone grants no permission to act. Also check the session token, identity, current participation,
        /// final exit and applicable reconnection window.
        /// </summary>
        /// <param name="matchId">Match to look up.</param>
        /// <param name="userId">Player requesting the snapshot.</param>
        /// <returns>Snapshot, or null if the match is missing or the player was not a participant.</returns>
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

        /// <summary>
        /// Releases the match in the same critical section that removes its room (AUD-002). This is the only release
        /// point. Each departing player's match callback is already removed when they leave the room.
        /// </summary>
        /// <param name="removedStart">Start state of the room being removed.</param>
        private void ReleaseMatchOfRemovedRoomInLock(LobbyStartState removedStart)
        {
            if (removedStart.MatchId.HasValue)
            {
                _matchesById.Remove(removedStart.MatchId.Value);
            }
        }

        public ResultWithNotification<OperationRecoveryContextResult> GetRecoveryContext(string sessionToken)
        {
            var result = new OperationRecoveryContextResult
            {
                ErrorKey = TLMG.Domain.Constants.ErrorKeys.Common.InvalidSession
            };
            var notifications = new List<Action>();

            lock (_sessionsLock)
            {
                DateTime nowUtc = _readUtcNow();
                if (!string.IsNullOrEmpty(sessionToken) && _sessionsByToken.ContainsKey(sessionToken))
                {
                    EvaluateHeartbeatDeadlinesInLock(nowUtc, notifications, true);
                }

                if (!string.IsNullOrEmpty(sessionToken)
                    && _sessionsByToken.TryGetValue(sessionToken, out PlayerSession session) && session.IsRegistered)
                {
                    session.LastActivityUtc = nowUtc;
                    result.IsSuccess = true;
                    result.ErrorKey = null;
                    MatchRecord match = FindRecoverableMatchInLock(session.UserId);
                    if (match != null)
                    {
                        result.RoomId = match.RoomId;
                        result.MatchId = match.MatchId;
                    }
                }
            }

            return new ResultWithNotification<OperationRecoveryContextResult>(result, notifications.AsReadOnly());
        }

        private MatchRecord FindRecoverableMatchInLock(int userId)
        {
            if (_roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId is Guid matchId
                && _matchesById.TryGetValue(matchId, out MatchRecord match)
                && match.MatchState.Players.Any(player => player.PlayerId == userId && !player.HasLeftMatch))
            {
                return match;
            }

            return null;
        }

        private bool HasActiveParticipationInLock(int userId)
        {
            return _roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId is Guid matchId
                && _matchesById.TryGetValue(matchId, out MatchRecord match)
                && match.MatchState.Phase != RoundPhase.MatchFinished
                && match.MatchState.Players.Any(player => player.PlayerId == userId && player.IsInPlay);
        }


        /// <summary>
        /// Checks match deadlines under _sessionsLock using server UTC time. Handles missing first connections and
        /// expired reconnection windows, and checks game deadlines in time order. An expired punishment removes a
        /// uniformly chosen card after ValidationRules.PunishmentChoiceSeconds (10 seconds). Match operations,
        /// subscriptions, voluntary exits and heartbeat checks can advance deadlines without server timers (D-54,
        /// BL-058).
        /// </summary>
        /// <param name="matchRecord">Match whose deadlines are being checked.</param>
        /// <param name="nowUtc">Server UTC time read once per operation.</param>
        /// <returns>True if a deadline expired and changed match state.</returns>
        private const int ReconnectGraceSeconds = 15;
        private const int FirstMatchLinkSeconds = 9;

        private bool ExpireMatchDeadlinesInLock(MatchRecord match, DateTime nowUtc)
        {
            bool changed = false;
            if (match.AwaitingFirstLink.Count != 0 && match.FirstLinkDeadlineUtc <= nowUtc)
            {
                changed |= ExpireGameDeadlinesInLock(match, match.FirstLinkDeadlineUtc);
                foreach (int userId in match.AwaitingFirstLink)
                {
                    changed |= OpenMatchGraceInLock(match, userId, match.FirstLinkDeadlineUtc);
                }
                match.AwaitingFirstLink.Clear();
            }

            var expired = match.Disconnections.Where(entry => entry.Value.EffectiveAtUtc <= nowUtc)
                .GroupBy(entry => entry.Value.EffectiveAtUtc).OrderBy(group => group.Key).ToArray();
            foreach (var group in expired)
            {
                changed |= ExpireGameDeadlinesInLock(match, group.Key);
                var exits = group.Where(entry => match.MatchState.Players.Any(player =>
                    player.PlayerId == entry.Key && player.IsInPlay)).ToDictionary(entry => entry.Key, entry => entry.Value);
                foreach (var entry in group)
                {
                    match.Disconnections.Remove(entry.Key);
                }

                if (exits.Count != 0 && match.MatchState.Phase != RoundPhase.MatchFinished)
                {
                    PlayerExitStatus status = exits.Count == 1
                        ? match.MatchState.RemovePlayer(exits.Keys.Single(), exits.Values.Single(), _pickRandomIndex)
                        : match.MatchState.RemoveSimultaneousPlayers(exits, _pickRandomIndex);
                    UpdateChallengeAfterExit(match, status);
                    DiscardEndedBlindSlots(match);
                }
                UpdateKittenClockInLock(match, group.Key);
                changed = true;
            }

            return ExpireGameDeadlinesInLock(match, nowUtc) || changed;
        }

        private static bool OpenMatchGraceInLock(MatchRecord match, int userId, DateTime nowUtc)
        {

            if (match.MatchState.Phase == RoundPhase.MatchFinished
                || match.Disconnections.ContainsKey(userId)
                || !match.MatchState.Players.Any(player => player.PlayerId == userId && player.IsInPlay))
            {
                return false;
            }

            match.Disconnections.Add(userId, new PlayerExit(nowUtc, nowUtc.AddSeconds(ReconnectGraceSeconds)));
            match.MatchState.SuspendPunishment(userId, nowUtc);
            UpdateKittenClockInLock(match, nowUtc);
            return true;
        }

        private static bool UpdateKittenClockInLock(MatchRecord match, DateTime nowUtc)
        {
            return match.Kitten != null && match.MatchState.Phase != RoundPhase.MatchFinished
                && match.Kitten.SetPaused(match.AwaitingFirstLink.Count != 0 || match.Disconnections.Count != 0,
                    nowUtc);
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

        /// <summary>
        /// Applies an immediate voluntary exit under _sessionsLock (Rules No. 3 and 4.0.6). Restarts an ordinary round
        /// and clears its bid. During punishment the effect depends on who leaves, including cancellation or a random
        /// choice. A sole active player wins.
        /// </summary>
        /// <param name="userId">Player leaving.</param>
        /// <param name="matchId">Match in the room being left.</param>
        /// <returns>One view per subscriber if state changed, otherwise empty.</returns>
        private IReadOnlyList<MatchViewDelivery> ApplyVoluntaryMatchExitInLock(int userId, Guid matchId)
        {
            IReadOnlyList<MatchViewDelivery> viewDeliveries = _noMatchViewDeliveries;
            MatchRecord matchRecord;

            if (_matchesById.TryGetValue(matchId, out matchRecord))
            {
                DateTime nowUtc = _readUtcNow();
                bool hasChanged = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
                PlayerExitStatus exitStatus = matchRecord.MatchState.RemovePlayer(
                    userId,
                    PlayerExit.Immediate(nowUtc),
                    _pickRandomIndex);
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

        /// <summary>Drops the blind slot mapping when punishment is no longer pending.</summary>
        private static void DiscardEndedBlindSlots(MatchRecord matchRecord)
        {
            if (matchRecord.MatchState.PendingPunishment == null)
            {
                matchRecord.BlindSlotOrder = null;
            }
        }

        private static bool HasExitChangedMatch(PlayerExitStatus exitStatus)
        {
            bool hasChanged = true;

            switch (exitStatus)
            {
                case PlayerExitStatus.EliminatedPlayerLeft:
                case PlayerExitStatus.MatchAlreadyFinished:
                case PlayerExitStatus.PlayerNotInMatch:
                case PlayerExitStatus.PlayerAlreadyLeft:
                    hasChanged = false;
                    break;
            }

            return hasChanged;
        }

        /// <summary>
        /// Updates the recorded outcome after an exit. A round restart clears an unfinished challenge. A pending
        /// punishment can be recorded as canceled or resolved at random.
        /// </summary>
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

        /// <summary>
        /// Prepares the room's match without publishing it. Uses the room mode and fixes participants in entry order.
        /// Requires _sessionsLock and may throw if the engine rejects the settings.
        /// </summary>
        private MatchRecord CreateMatchRecordInLock(Guid matchId, int roomId, DateTime nowUtc)
        {
            int cardsPerPlayerCount = _startsByRoomId[roomId].CardsPerPlayerCount;
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            IReadOnlyList<int> participantUserIds =
                _membersByRoomId[roomId].Select(member => member.UserId).ToList().AsReadOnly();
            MatchState matchState = new MatchState(modeRules, participantUserIds);
            MatchRecord matchRecord = new MatchRecord(
                matchId,
                roomId,
                matchState,
                participantUserIds,
                cardsPerPlayerCount,
                CopyNicknamesInLock(participantUserIds),
                participantUserIds.All(userId => _sessionsByToken[_tokensByUserId[userId]].IsRegistered),
                nowUtc);

            return matchRecord;
        }

        /// <summary>Copies participant nicknames at start under _sessionsLock.</summary>
        private Dictionary<int, string> CopyNicknamesInLock(IReadOnlyList<int> participantUserIds)
        {
            Dictionary<int, string> nicknamesByUserId = new Dictionary<int, string>();

            foreach (int userId in participantUserIds)
            {
                nicknamesByUserId[userId] = _sessionsByToken[_tokensByUserId[userId]].Nickname;
            }

            return nicknamesByUserId;
        }

        /// <summary>Immutable match snapshot. Requires _sessionsLock.</summary>
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
                matchState.PlayerInTurn.PlayerId);

            return matchSnapshot;
        }

        /// <summary>Authorized match. Change its engine state only through MatchState under _sessionsLock.</summary>
        private sealed class MatchRecord
        {
            public MatchRecord(
                Guid matchId,
                int roomId,
                MatchState matchState,
                IReadOnlyList<int> participantUserIds,
                int cardsPerPlayerCount,
                IReadOnlyDictionary<int, string> participantNicknames,
                bool allParticipantsRegistered,
                DateTime nowUtc)
            {
                MatchId = matchId;
                RoomId = roomId;
                MatchState = matchState;
                ParticipantUserIds = participantUserIds;
                CardsPerPlayerCount = cardsPerPlayerCount;
                ParticipantNicknames = participantNicknames;
                AllParticipantsRegistered = allParticipantsRegistered;
                Version = InitialMatchVersion;
                FirstLinkDeadlineUtc = nowUtc.AddSeconds(FirstMatchLinkSeconds);
                AwaitingFirstLink = new HashSet<int>(participantUserIds);

                if (matchState.ModeRules.AllowsExtraLife)
                {
                    Kitten = new FugitiveKittenState(nowUtc);
                }
            }

            /// <summary>Match ID shared with the room.</summary>
            public Guid MatchId { get; }

            /// <summary>Room that authorized the match.</summary>
            public int RoomId { get; }

            /// <summary>Authoritative engine state kept under the lock.</summary>
            public MatchState MatchState { get; }

            /// <summary>Participants fixed at authorization in room entry order.</summary>
            public IReadOnlyList<int> ParticipantUserIds { get; }

            /// <summary>Cards per player at authorization.</summary>
            public int CardsPerPlayerCount { get; }

            /// <summary>Nicknames recorded at authorization and used in views even after a player leaves.</summary>
            public IReadOnlyDictionary<int, string> ParticipantNicknames { get; }

            /// <summary>Whether all original participants had registered accounts.</summary>
            public bool AllParticipantsRegistered { get; }

            public FugitiveKittenState Kitten { get; }

            public Dictionary<int, PlayerExit> Disconnections { get; } = new Dictionary<int, PlayerExit>();

            public DateTime FirstLinkDeadlineUtc { get; }

            public HashSet<int> AwaitingFirstLink { get; }

            /// <summary>Match version, increased for each accepted action under _sessionsLock.</summary>
            public int Version { get; set; }

            /// <summary>
            /// Latest challenge outcome. Null before the first challenge and cleared by the next opening bid or card
            /// placement. Changes only under _sessionsLock.
            /// </summary>
            public MatchChallengeRecord LastChallenge { get; set; }

            /// <summary>
            /// Server's private mapping from displayed blind slot to actual hand index. Null without pending blind
            /// punishment. Never leaves the registry.
            /// </summary>
            public int[] BlindSlotOrder { get; set; }
        }
    }
}
