using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int DefaultCardsPerPlayerCount = ValidationRules.FourCardsPerPlayerCount;
        private const int LobbyStartCountdownSeconds = 3;

        private readonly Dictionary<int, LobbyStartState> _startsByRoomId =
            new Dictionary<int, LobbyStartState>();

        public LobbyMembershipChange ChangePlayerReadiness(SessionChannelCaller caller, int roomId, bool isReady)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = CreateRejectedWithoutUser(lobbyChangeStatus);
                }
                else
                {
                    lobbyMembershipChange = ChangePlayerReadinessInLock(playerSession.UserId, roomId, isReady);
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange ChangeCardsPerPlayer(SessionChannelCaller caller, int roomId, int cardsPerPlayerCount)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = CreateRejectedWithoutUser(lobbyChangeStatus);
                }
                else
                {
                    lobbyMembershipChange =
                        ChangeCardsPerPlayerInLock(playerSession.UserId, roomId, cardsPerPlayerCount);
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange RequestMatchStart(SessionChannelCaller caller, int roomId, long countdownId)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = CreateRejectedWithoutUser(lobbyChangeStatus);
                }
                else
                {
                    lobbyMembershipChange = RequestMatchStartInLock(playerSession.UserId, roomId, countdownId);
                }
            }

            return lobbyMembershipChange;
        }

        /// <summary>Checks identity and channel before looking up the room. Requires _sessionsLock.</summary>
        /// <param name="caller">Token and channel received from the client.</param>
        /// <param name="roomId">Room specified by the client.</param>
        /// <param name="playerSession">Session found, or null for an unknown token.</param>
        /// <returns>
        /// Applied if identity, channel and room match. Otherwise the reason without room state.
        /// </returns>
        private LobbyChangeStatus ValidateLobbyCallerInLock(
            SessionChannelCaller caller,
            int roomId,
            out PlayerSession playerSession)
        {
            LobbyChangeStatus lobbyChangeStatus = LobbyChangeStatus.InvalidSession;
            playerSession = null;
            int channelUserId;
            int currentRoomId;

            if (caller.HasSessionAndChannel && _sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                bool hasChannel = _userIdsByChannelId.TryGetValue(caller.ChannelId, out channelUserId);
                bool hasRoom = _roomIdsByUserId.TryGetValue(playerSession.UserId, out currentRoomId);

                if (hasChannel && channelUserId == playerSession.UserId)
                {
                    lobbyChangeStatus = hasRoom && currentRoomId == roomId
                        ? LobbyChangeStatus.Applied
                        : LobbyChangeStatus.NotInRoom;
                }
                else if (!hasChannel && !hasRoom)
                {
                    lobbyChangeStatus = LobbyChangeStatus.NotInRoom;
                }
            }

            return lobbyChangeStatus;
        }

        /// <summary>
        /// Applies a readiness change. A repeated value only copies state under _sessionsLock.
        /// </summary>
        /// <param name="userId">Player already checked.</param>
        /// <param name="roomId">Player's current room.</param>
        /// <param name="isReady">Requested ready state.</param>
        /// <returns>Change result and recipients, or rejection if the match has started.</returns>
        private LobbyMembershipChange ChangePlayerReadinessInLock(int userId, int roomId, bool isReady)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange;

            if (lobbyStartState.ReadyUserIds.Contains(userId) == isReady)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId]));
            }
            else if (lobbyStartState.MatchId.HasValue)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.MatchAlreadyStarted, userId);
            }
            else
            {
                if (isReady)
                {
                    lobbyStartState.ReadyUserIds.Add(userId);
                }
                else
                {
                    lobbyStartState.ReadyUserIds.Remove(userId);
                }

                RefreshLobbyCountdownInLock(roomId);
                lobbyMembershipChange = CopyLobbyChangeInLock(userId, roomId);
            }

            return lobbyMembershipChange;
        }

        /// <summary>
        /// Authorizes one match when the current countdown generation expires and creates its MatchState under
        /// _sessionsLock. Prepares the ID, match, new start state, recipients, room snapshot and result before changing
        /// anything. Then assigns the match followed by the start state containing its MatchId.
        /// </summary>
        /// <param name="userId">Player already checked.</param>
        /// <param name="roomId">Player's current room.</param>
        /// <param name="countdownId">Generation observed by the client.</param>
        /// <returns>Current state, with recipients only when the match is authorized.</returns>
        private LobbyMembershipChange RequestMatchStartInLock(int userId, int roomId, long countdownId)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            DateTime nowUtc = _readUtcNow();
            LobbyMembershipChange lobbyMembershipChange;

            if (CanAuthorizeMatchStartInLock(roomId, countdownId, nowUtc))
            {
                List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
                Guid matchId = Guid.NewGuid();
                MatchRecord matchRecord = CreateMatchRecordInLock(matchId, roomId, nowUtc);
                LobbyStartState authorizedStart = CreateAuthorizedStart(lobbyStartState, matchId, nowUtc);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId,
                    roomId,
                    CreateRoomSnapshot(roomId, lobbyMembers, authorizedStart),
                    CopyCallbacks(lobbyMembers));

                _matchesById[matchId] = matchRecord;
                _startsByRoomId[roomId] = authorizedStart;
            }
            else
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId]));
            }

            return lobbyMembershipChange;
        }

        /// <summary>Checks that the current countdown has expired before authorizing under _sessionsLock.</summary>
        /// <param name="roomId">Existing room.</param>
        /// <param name="countdownId">Requested generation.</param>
        /// <param name="nowUtc">Server UTC time read under the lock.</param>
        /// <returns>True if the current countdown expired and all members meet readiness requirements.</returns>
        private bool CanAuthorizeMatchStartInLock(int roomId, long countdownId, DateTime nowUtc)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            bool hasActiveCountdown = !lobbyStartState.MatchId.HasValue
                && lobbyStartState.CountdownEndsAtUtc.HasValue;
            bool canAuthorize = false;

            if (hasActiveCountdown)
            {
                bool hasRequestedGeneration = lobbyStartState.CountdownId == countdownId;
                bool hasCountdownExpired = nowUtc >= lobbyStartState.CountdownEndsAtUtc.Value;
                canAuthorize = hasRequestedGeneration && hasCountdownExpired && AreLobbyPlayersReadyInLock(roomId);
            }

            return canAuthorize;
        }

        /// <summary>
        /// Increases the version and recalculates the countdown after an actual change. Requires _sessionsLock.
        /// </summary>
        /// <param name="roomId">Room whose membership or readiness changed.</param>
        private void RefreshLobbyCountdownInLock(int roomId)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            lobbyStartState.Version++;

            if (!lobbyStartState.MatchId.HasValue)
            {
                lobbyStartState.CountdownEndsAtUtc = null;

                if (AreLobbyPlayersReadyInLock(roomId))
                {
                    lobbyStartState.CountdownId++;
                    lobbyStartState.CountdownEndsAtUtc = _readUtcNow().AddSeconds(LobbyStartCountdownSeconds);
                }
            }
        }

        /// <summary>Checks member count and readiness under _sessionsLock.</summary>
        /// <param name="roomId">Existing room to check.</param>
        /// <returns>True if the room has an allowed player count and everyone is ready.</returns>
        private bool AreLobbyPlayersReadyInLock(int roomId)
        {
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            bool hasRequiredPlayerCount = lobbyMembers.Count >= ValidationRules.MinimumPlayersPerMatchCount
                && lobbyMembers.Count <= ValidationRules.MaximumPlayersPerMatchCount;
            bool areReady = hasRequiredPlayerCount
                && lobbyMembers.TrueForAll(member => lobbyStartState.ReadyUserIds.Contains(member.UserId));

            return areReady;
        }

        /// <summary>
        /// Changes cards per player and resets everyone to not ready. Repeating the value only copies state. Requires
        /// _sessionsLock. Copies callbacks and notifies only after releasing the lock.
        /// </summary>
        /// <param name="userId">Player already checked.</param>
        /// <param name="roomId">Player's current room.</param>
        /// <param name="cardsPerPlayerCount">Validated number of cards per player.</param>
        /// <returns>Change result and recipients, or rejection if the match has started.</returns>
        private LobbyMembershipChange ChangeCardsPerPlayerInLock(int userId, int roomId, int cardsPerPlayerCount)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange;

            if (lobbyStartState.CardsPerPlayerCount == cardsPerPlayerCount)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, lobbyMembers));
            }
            else if (lobbyStartState.MatchId.HasValue)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.MatchAlreadyStarted, userId);
            }
            else
            {
                lobbyStartState.CardsPerPlayerCount = cardsPerPlayerCount;
                lobbyStartState.ReadyUserIds.Clear();
                RefreshLobbyCountdownInLock(roomId);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId,
                    roomId,
                    CreateRoomSnapshot(roomId, lobbyMembers),
                    CopyCallbacksExceptUser(lobbyMembers, userId));
            }

            return lobbyMembershipChange;
        }

        /// <summary>Copies the other room members' callbacks under _sessionsLock.</summary>
        /// <param name="lobbyMembers">Room members.</param>
        /// <param name="excludedUserId">Player who caused the change and needs no notification.</param>
        /// <returns>Callbacks for all members except the excluded player.</returns>
        private static IReadOnlyList<ILobbyManagerCallback> CopyCallbacksExceptUser(
            List<LobbyMember> lobbyMembers,
            int excludedUserId)
        {
            List<ILobbyManagerCallback> callbacks = new List<ILobbyManagerCallback>();

            foreach (LobbyMember lobbyMember in lobbyMembers)
            {
                if (lobbyMember.UserId != excludedUserId && lobbyMember.Callback != null)
                {
                    callbacks.Add(lobbyMember.Callback);
                }
            }

            IReadOnlyList<ILobbyManagerCallback> readOnlyCallbacks = callbacks.AsReadOnly();

            return readOnlyCallbacks;
        }

        /// <summary>Copies state and callbacks under _sessionsLock for notification after release.</summary>
        /// <param name="userId">Player who caused the change.</param>
        /// <param name="roomId">Updated room.</param>
        /// <returns>Copied state and recipients, including the requester.</returns>
        private LobbyMembershipChange CopyLobbyChangeInLock(int userId, int roomId)
        {
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                userId, roomId, CreateRoomSnapshot(roomId, lobbyMembers), CopyCallbacks(lobbyMembers));

            return lobbyMembershipChange;
        }

        /// <summary>
        /// Copies readiness and start state without sharing mutable collections. Requires _sessionsLock.
        /// </summary>
        /// <param name="roomId">Existing room to copy.</param>
        /// <returns>Read-only snapshot with server time in UTC.</returns>
        private LobbyStartSnapshot CreateStartSnapshotInLock(int roomId)
        {
            return CreateStartSnapshotInLock(_startsByRoomId[roomId]);
        }

        /// <summary>Copies published or pending start state under _sessionsLock.</summary>
        private LobbyStartSnapshot CreateStartSnapshotInLock(LobbyStartState lobbyStartState)
        {
            LobbyStartSnapshot lobbyStartSnapshot = new LobbyStartSnapshot(
                new List<int>(lobbyStartState.ReadyUserIds).AsReadOnly(),
                lobbyStartState.Version,
                lobbyStartState.CountdownId,
                lobbyStartState.CountdownEndsAtUtc,
                _readUtcNow(),
                lobbyStartState.MatchId,
                lobbyStartState.MatchStartedAtUtc,
                lobbyStartState.CardsPerPlayerCount);

            return lobbyStartSnapshot;
        }

        /// <summary>
        /// Prepares the next start state without publishing it. Keeps readiness and mode, closes the countdown, sets
        /// MatchId and start time, and advances the version.
        /// </summary>
        private static LobbyStartState CreateAuthorizedStart(
            LobbyStartState currentStart,
            Guid matchId,
            DateTime nowUtc)
        {
            LobbyStartState authorizedStart = new LobbyStartState
            {
                Version = currentStart.Version + 1,
                CountdownId = currentStart.CountdownId,
                CountdownEndsAtUtc = null,
                MatchId = matchId,
                MatchStartedAtUtc = nowUtc,
                CardsPerPlayerCount = currentStart.CardsPerPlayerCount
            };
            authorizedStart.ReadyUserIds.UnionWith(currentStart.ReadyUserIds);

            return authorizedStart;
        }

        /// <summary>Private mutable state accessed only under _sessionsLock and never sent in notifications.</summary>
        private sealed class LobbyStartState
        {
            /// <summary>Players currently marked ready.</summary>
            public HashSet<int> ReadyUserIds { get; } = new HashSet<int>();

            /// <summary>Increasing room change version.</summary>
            public long Version { get; set; }

            /// <summary>Latest countdown generation created for this room.</summary>
            public long CountdownId { get; set; }

            /// <summary>Active countdown deadline in UTC, or null without a countdown.</summary>
            public DateTime? CountdownEndsAtUtc { get; set; }

            /// <summary>Authorized match ID, or null before start.</summary>
            public Guid? MatchId { get; set; }

            /// <summary>Match authorization time in UTC, or null before start.</summary>
            public DateTime? MatchStartedAtUtc { get; set; }

            /// <summary>Room's cards per player. New rooms start with the default.</summary>
            public int CardsPerPlayerCount { get; set; } = DefaultCardsPerPlayerCount;
        }
    }
}