using System;
using System.Collections.Generic;
using TLMG.Domain.Authorization;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry : IMatchStartRegistry
    {
        private const int MinimumPersistedGameId = 1;
        private readonly Dictionary<int, PendingMatchStart> _pendingMatchStartsByRoomId =
            new Dictionary<int, PendingMatchStart>();
        private readonly HashSet<Guid> _cancelledMatchIds = new HashSet<Guid>();

        public MatchStartPreparation PrepareMatchStart(LobbyCaller caller, int roomId, long countdownId)
        {
            MatchStartPreparation macthStartPreparation;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus == LobbyChangeStatus.Applied)
                {
                    macthStartPreparation = PrepareMatchStartInLock(playerSession.UserId, roomId, countdownId);
                }
                else
                {
                    macthStartPreparation = new MatchStartPreparation(CreateRejectedWithoutUser(lobbyChangeStatus), null);
                }
            }

            return macthStartPreparation;
        }

        public LobbyMembershipChange CompleteMatchStart(
            LobbyCaller caller, MatchStartPreparation matchStartPreparation, GameRegistrationResult gameRegistrationResult)
        {
            LobbyMembershipChange lobbyMemberChange;

            lock (_sessionsLock)
            {
                lobbyMemberChange = CompleteMatchStartInLock(caller, matchStartPreparation, gameRegistrationResult);
            }

            return lobbyMemberChange;
        }

        public IReadOnlyList<Guid> FindCancelledMatchIds()
        {
            IReadOnlyList<Guid> cancelledMatchIds;

            lock (_sessionsLock)
            {
                cancelledMatchIds = new List<Guid>(_cancelledMatchIds).AsReadOnly();
            }

            return cancelledMatchIds;
        }

        public void ConfirmCancelledMatch(Guid matchId)
        {
            lock (_sessionsLock)
            {
                _cancelledMatchIds.Remove(matchId);
            }
        }

        private MatchStartPreparation PrepareMatchStartInLock(int userId, int roomId, long countdownId)
        {
            GameRegistration gameRegistration = null;
            LobbyMembershipChange lobbyMembershipChange = CopyCurrentMatchStartInLock(userId, roomId);
            DateTime nowUtc = _readUtcNow();

            if (CanAuthorizeMatchStartInLock(roomId, countdownId, nowUtc))
            {
                PendingMatchStart pendingMatchStart;

                if (!_pendingMatchStartsByRoomId.TryGetValue(roomId, out pendingMatchStart))
                {
                    pendingMatchStart = new PendingMatchStart(CreateRegistrationInLock(roomId, nowUtc));
                    _pendingMatchStartsByRoomId.Add(roomId, pendingMatchStart);
                }

                if (pendingMatchStart.HasDefinitiveRejection)
                {
                    lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                        LobbyChangeStatus.MatchStartPersistenceFailed, userId);
                }
                else
                {
                    gameRegistration = pendingMatchStart.GameRegistration;
                }
            }

            MatchStartPreparation matchStartPreparation = new MatchStartPreparation(lobbyMembershipChange, gameRegistration);

            return matchStartPreparation;
        }

        private GameRegistration CreateRegistrationInLock(int roomId, DateTime nowUtc)
        {
            LobbyStartState startState = _startsByRoomId[roomId];
            List<int> registeredUserIds = new List<int>();

            foreach (LobbyMember member in _membersByRoomId[roomId])
            {
                if (!GuestIdentity.IsGuestUserId(member.UserId))
                {
                    registeredUserIds.Add(member.UserId);
                }
            }

            GameRegistration gameRegistration = new GameRegistration
            {
                MatchId = Guid.NewGuid(),
                LobbyOpenedAtUtc = startState.LobbyOpenedAtUtc,
                StartedAtUtc = nowUtc,
                CardsPerPlayerCount = startState.CardsPerPlayerCount,
                RegisteredUserIds = registeredUserIds.AsReadOnly()
            };

            return gameRegistration;
        }

        private LobbyMembershipChange CompleteMatchStartInLock(
            LobbyCaller caller, MatchStartPreparation matchStartPreparation, GameRegistrationResult gameRegistrationResult)
        {
            LobbyMembershipChange lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.NotAuthorized);

            if (matchStartPreparation != null && matchStartPreparation.GameRegistration != null)
            {
                int roomId = matchStartPreparation.LobbyMembershipChange.RoomId;
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);
                PendingMatchStart pendingMatchStart;
                bool isCurrentReservation = _pendingMatchStartsByRoomId.TryGetValue(roomId, out pendingMatchStart)
                    && ReferenceEquals(pendingMatchStart.GameRegistration, matchStartPreparation.GameRegistration);
                GameRegistrationStatus registrationStatus = ResolveRegistrationStatus(gameRegistrationResult);

                lobbyMembershipChange = null;

                if (isCurrentReservation)
                {
                    bool canPublish = lobbyChangeStatus == LobbyChangeStatus.Applied
                        && CanAuthorizeMatchStartInLock(roomId, matchStartPreparation.LobbyMembershipChange.Room.Start.CountdownId, _readUtcNow());

                    if (canPublish && !pendingMatchStart.HasDefinitiveRejection)
                    {
                        lobbyMembershipChange = ApplyRegistrationResultInLock(playerSession.UserId, roomId, registrationStatus);
                    }
                    else if (!canPublish)
                    {
                        InvalidatePendingMatchStartInLock(roomId);
                    }
                }

                if (lobbyMembershipChange == null)
                {
                    lobbyMembershipChange = lobbyChangeStatus == LobbyChangeStatus.Applied
                        ? CopyCurrentMatchStartInLock(playerSession.UserId, roomId)
                        : CreateRejectedWithoutUser(lobbyChangeStatus);
                }

                if (registrationStatus == GameRegistrationStatus.RegistrationConflict)
                {
                    _cancelledMatchIds.Remove(matchStartPreparation.GameRegistration.MatchId);
                }
            }

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange ApplyRegistrationResultInLock(
            int userId, int roomId, GameRegistrationStatus gameRegistrationStatus)
        {
            LobbyMembershipChange lobbyMembershipChange;
            PendingMatchStart pendingMatchStart = _pendingMatchStartsByRoomId[roomId];

            if (gameRegistrationStatus == GameRegistrationStatus.Created
                || gameRegistrationStatus == GameRegistrationStatus.AlreadyRegistered)
            {
                lobbyMembershipChange = PublishRegisteredMatchInLock(userId, roomId, pendingMatchStart.GameRegistration);
                _pendingMatchStartsByRoomId.Remove(roomId);
            }
            else if (gameRegistrationStatus == GameRegistrationStatus.DataFailure)
            {
                lobbyMembershipChange = CopyCurrentMatchStartInLock(userId, roomId);
            }
            else
            {
                pendingMatchStart.HasDefinitiveRejection = true;
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(LobbyChangeStatus.MatchStartPersistenceFailed, userId);
            }

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange PublishRegisteredMatchInLock(int userId, int roomId, GameRegistration gameRegistration)
        {
            LobbyStartState startState = _startsByRoomId[roomId];
            List<LobbyMember> members = _membersByRoomId[roomId];
            DateTime publicationTimeUtc = _readUtcNow();
            MatchRecord matchRecord = CreateMatchRecordInLock(gameRegistration.MatchId, roomId, publicationTimeUtc);
            LobbyStartState authorizedStart = CreateAuthorizedStart(startState, gameRegistration.MatchId, gameRegistration.StartedAtUtc);
            LobbyMembershipChange lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                userId, roomId, CreateRoomSnapshot(roomId, members, authorizedStart), CopyCallbacks(members));

            _matchesById[gameRegistration.MatchId] = matchRecord;
            _startsByRoomId[roomId] = authorizedStart;

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange CopyCurrentMatchStartInLock(int userId, int roomId)
        {
            LobbyMembershipChange lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                userId, roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId]));

            return lobbyMembershipChange;
        }

        private void InvalidatePendingMatchStartInLock(int roomId)
        {
            PendingMatchStart pendingMatchStart;

            if (_pendingMatchStartsByRoomId.TryGetValue(roomId, out pendingMatchStart))
            {
                _pendingMatchStartsByRoomId.Remove(roomId);

                if (!pendingMatchStart.HasDefinitiveRejection)
                {
                    _cancelledMatchIds.Add(pendingMatchStart.GameRegistration.MatchId);
                }
            }
        }

        private static GameRegistrationStatus ResolveRegistrationStatus(GameRegistrationResult gameRegistrationResult)
        {
            GameRegistrationStatus gameRegistrationStatus = GameRegistrationStatus.DataFailure;

            if (gameRegistrationResult != null
                && Enum.IsDefined(typeof(GameRegistrationStatus), gameRegistrationResult.GameRegistrationStatus))
            {
                gameRegistrationStatus = gameRegistrationResult.GameRegistrationStatus;

                if ((gameRegistrationStatus == GameRegistrationStatus.Created || gameRegistrationStatus == GameRegistrationStatus.AlreadyRegistered)
                    && gameRegistrationResult.GameId < MinimumPersistedGameId)
                {
                    gameRegistrationStatus = GameRegistrationStatus.DataFailure;
                }
            }

            return gameRegistrationStatus;
        }

        private sealed class PendingMatchStart
        {
            internal PendingMatchStart(GameRegistration gameRegistration)
            {
                GameRegistration = gameRegistration;
            }

            internal GameRegistration GameRegistration { get; }
            internal bool HasDefinitiveRejection { get; set; }
        }
    }
}
