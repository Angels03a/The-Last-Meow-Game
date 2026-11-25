using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Domain.Authorization;
using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Protects shared session, room, match and friendship state with one lock.
    /// Database calls and notifications are performed outside the session lock.
    /// </summary>
    public partial class SessionRegistry : ISessionRegistry
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SessionRegistry));

        private readonly object _sessionsLock = new object();
        private readonly Func<DateTime> _readUtcNow;
        private readonly Func<int, int> _pickRandomIndex;
        private readonly Dictionary<string, PlayerSession> _sessionsByToken =
            new Dictionary<string, PlayerSession>();
        private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();

        /// <summary>Creates the registry with the system UTC clock.</summary>
        public SessionRegistry() : this(ReadSystemUtcNow)
        {
        }

        internal SessionRegistry(Func<DateTime> readUtcNow) : this(readUtcNow, new Random().Next)
        {
        }

        internal SessionRegistry(Func<DateTime> readUtcNow, Func<int, int> pickRandomIndex)
        {
            _readUtcNow = readUtcNow;
            _pickRandomIndex = pickRandomIndex;
        }

        /// <inheritdoc />
        public string StartRegisteredSession(int userId, string nickname, out SessionEnd replacedSession)
        {
            string sessionToken = SessionTokenHelper.GenerateSessionToken();
            PlayerSession newSession = PlayerSession.CreateRegistered(userId, nickname);
            replacedSession = null;

            lock (_sessionsLock)
            {
                string previousToken;

                if (_tokensByUserId.TryGetValue(userId, out previousToken))
                {
                    PlayerSession previous = _sessionsByToken[previousToken];
                    IReadOnlyList<MatchViewDelivery> matchViews;
                    replacedSession = PreserveMatchOnReplacementInLock(previous, out matchViews)
                        ?? EndSessionInLock(previous, RoomExitCause.SessionReplaced);
                    replacedSession.MatchViews = matchViews;
                }

                newSession.LastActivityUtc = _readUtcNow();
                _sessionsByToken[sessionToken] = newSession;
                _tokensByUserId[userId] = sessionToken;
            }

            if (replacedSession != null)
            {
                _log.InfoFormat("La sesión anterior del jugador {0} fue reemplazada por un nuevo inicio.", userId);
                LogDepartureOfEndedSession(replacedSession);
            }

            _log.InfoFormat("Sesión iniciada para el jugador {0}.", userId);

            return sessionToken;
        }

        /// <inheritdoc />
        public bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession)
        {
            bool wasFound = false;
            playerSession = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    wasFound = _sessionsByToken.TryGetValue(sessionToken, out playerSession);

                    if (wasFound)
                    {
                        playerSession.LastActivityUtc = _readUtcNow();
                    }
                }
            }

            return wasFound;
        }

        /// <inheritdoc />
        public bool EndSession(string sessionToken, out SessionEnd sessionEnd)
        {
            bool wasEnded = EndSession(sessionToken, false, out sessionEnd, out _);

            return wasEnded;
        }

        /// <inheritdoc />
        public bool TryLogOut(string sessionToken, out SessionEnd sessionEnd, out bool hasActiveParticipation)
        {
            bool wasEnded = EndSession(sessionToken, true, out sessionEnd, out hasActiveParticipation);

            return wasEnded;
        }

        private static void LogDepartureOfEndedSession(SessionEnd sessionEnd)
        {
            LobbyMembershipChange roomDeparture = sessionEnd.RoomDeparture;

            if (roomDeparture.RoomId != LobbyMembershipChange.NoRoomId)
            {
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1} por la causa {2}.",
                    sessionEnd.UserId,
                    roomDeparture.RoomId,
                    roomDeparture.ExitCause);
                SessionNotificationHelper.LogMatchLeftUntouched(roomDeparture);
            }
        }

        private static DateTime ReadSystemUtcNow()
        {
            return DateTime.UtcNow;
        }

        private bool EndSession(string sessionToken, bool protectParticipation, out SessionEnd sessionEnd,
            out bool hasActiveParticipation)
        {
            hasActiveParticipation = false;
            sessionEnd = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    PlayerSession endedSession;

                    if (_sessionsByToken.TryGetValue(sessionToken, out endedSession))
                    {
                        hasActiveParticipation = protectParticipation &&
                            HasActiveParticipationInLock(endedSession.UserId);

                        if (!hasActiveParticipation)
                        {
                            sessionEnd = EndSessionInLock(endedSession, RoomExitCause.SessionEnded);
                        }
                    }
                }
            }

            bool wasEnded = sessionEnd != null;

            if (wasEnded)
            {
                _log.InfoFormat("Sesión cerrada para el jugador {0}.", sessionEnd.UserId);
                LogDepartureOfEndedSession(sessionEnd);
            }

            return wasEnded;
        }

        private SessionEnd PreserveMatchOnReplacementInLock(PlayerSession previous,
            out IReadOnlyList<MatchViewDelivery> matchViews)
        {
            SessionEnd replacedSession = null;
            matchViews = _noMatchViewDeliveries;
            int userId = previous.UserId;

            if (_roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId is Guid matchId
                && _matchesById.TryGetValue(matchId, out MatchRecord match))
            {
                DateTime nowUtc = _readUtcNow();
                bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);

                if (FindRecoverableMatchInLock(userId) != null)
                {
                    if (_matchChannelsByUserId.ContainsKey(userId))
                    {
                        changed |= OpenMatchGraceInLock(match, userId, nowUtc);
                    }

                    RemoveMatchChannelOfUserInLock(userId);

                    if (changed)
                    {
                        match.Version++;
                        matchViews = CreateViewDeliveriesInLock(match);
                    }

                    RemoveChatChannelOfUserInLock(userId);
                    DetachStartedLobbyChannelInLock(userId);
                    IReadOnlyList<IFriendsManagerCallback> recipients = FindPresenceRecipientsOfUser(userId);
                    _friendsChannelsByUserId.TryGetValue(userId, out FriendsChannel friendsChannel);
                    RemoveFriendsChannelOfUser(userId);
                    RemoveInvitationsOfUser(userId);
                    _authorizedRoomIdsByUserId.Remove(userId);
                    _sessionsByToken.Remove(_tokensByUserId[userId]);
                    replacedSession = new SessionEnd(userId, previous.Nickname,
                        LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotInRoom, userId),
                        FindCallbackOfChannel(friendsChannel), recipients);
                }
                else if (changed)
                {
                    match.Version++;
                    matchViews = CreateViewDeliveriesInLock(match);
                }
            }

            return replacedSession;
        }

        private SessionEnd EndSessionInLock(PlayerSession endedSession, RoomExitCause exitCause)
        {
            _sessionsByToken.Remove(_tokensByUserId[endedSession.UserId]);
            _tokensByUserId.Remove(endedSession.UserId);
            SessionEnd sessionEnd = RemoveSessionStateOfUser(endedSession, exitCause);

            if (GuestIdentity.IsGuestUserId(endedSession.UserId))
            {
                RemoveGuestFriendshipsOfUserInLock(endedSession.UserId);
            }

            return sessionEnd;
        }
    }
}
