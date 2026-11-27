using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
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
                    lobbyChangeStatus = LobbyChangeStatus.NotInRoom;

                    if (hasRoom && currentRoomId == roomId)
                    {
                        lobbyChangeStatus = LobbyChangeStatus.Applied;
                    }
                }
                else if (!hasChannel && !hasRoom)
                {
                    lobbyChangeStatus = LobbyChangeStatus.NotInRoom;
                }
            }

            return lobbyChangeStatus;
        }

        private LobbyMembershipChange ChangePlayerReadinessInLock(int userId, int roomId, bool isReady)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange;

            if (lobbyStartState.ReadyUserIds.Contains(userId) == isReady)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId,
                    new LobbyRoomUpdate(roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId])));
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

        private void RefreshLobbyCountdownInLock(int roomId)
        {
            InvalidatePendingMatchStartInLock(roomId);
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

        private bool AreLobbyPlayersReadyInLock(int roomId)
        {
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            bool hasRequiredPlayerCount = lobbyMembers.Count >= ValidationRules.MinimumPlayersPerMatchCount
                && lobbyMembers.Count <= ValidationRules.MaximumPlayersPerMatchCount;
            bool areReady = hasRequiredPlayerCount;

            for (int memberIndex = 0; areReady && memberIndex < lobbyMembers.Count; memberIndex++)
            {
                areReady = lobbyStartState.ReadyUserIds.Contains(lobbyMembers[memberIndex].UserId);
            }

            return areReady;
        }

        private LobbyMembershipChange ChangeCardsPerPlayerInLock(int userId, int roomId, int cardsPerPlayerCount)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange;

            if (lobbyStartState.CardsPerPlayerCount == cardsPerPlayerCount)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId,
                    new LobbyRoomUpdate(roomId, CreateRoomSnapshot(roomId, lobbyMembers)));
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
                    new LobbyRoomUpdate(roomId, CreateRoomSnapshot(roomId, lobbyMembers), CopyCallbacksExceptUser(lobbyMembers, userId)));
            }

            return lobbyMembershipChange;
        }

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

        private LobbyMembershipChange CopyLobbyChangeInLock(int userId, int roomId)
        {
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                userId,
                new LobbyRoomUpdate(roomId, CreateRoomSnapshot(roomId, lobbyMembers), CopyCallbacks(lobbyMembers)));

            return lobbyMembershipChange;
        }

        private LobbyStartSnapshot CreateStartSnapshotInLock(int roomId)
        {
            return CreateStartSnapshotInLock(_startsByRoomId[roomId]);
        }

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

        private static LobbyStartState CreateAuthorizedStart(
            LobbyStartState currentStart,
            Guid matchId,
            DateTime nowUtc)
        {
            LobbyStartState authorizedStart = new LobbyStartState
            {
                RoomScopeId = currentStart.RoomScopeId,
                KickVoting = currentStart.KickVoting,
                LobbyOpenedAtUtc = currentStart.LobbyOpenedAtUtc,
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
    }
}
