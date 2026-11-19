using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Guarda las sesiones iniciadas, las salas (SessionRegistry.Lobbies.cs) y los canales de amigos y las invitaciones
    /// (SessionRegistry.Friends.cs). Un solo candado protege todo el estado compartido y dentro de él no hay datos,
    /// callbacks, log ni esperas: solo se lee o modifica memoria, se copia lo necesario y el log y los avisos se hacen
    /// siempre después de salir del candado.
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

        /// <summary>Crea el registro con el reloj UTC del sistema.</summary>
        public SessionRegistry() : this(ReadSystemUtcNow)
        {
        }

        /// <summary>
        /// Crea el registro con la fuente de hora UTC indicada y el azar del sistema. El generador solo se usa bajo
        /// _sessionsLock, así que una sola instancia de Random es suficiente.
        /// </summary>
        /// <param name="readUtcNow">Devuelve la hora UTC actual; se lee solo bajo _sessionsLock.</param>
        internal SessionRegistry(Func<DateTime> readUtcNow) : this(readUtcNow, new Random().Next)
        {
        }

        /// <summary>
        /// Crea el registro con la fuente de hora UTC y el azar indicados; permite a las pruebas vencer la cuenta
        /// regresiva y el plazo del castigo sin esperar. El servidor usa el constructor sin parámetros.
        /// </summary>
        /// <param name="readUtcNow">Devuelve la hora UTC actual; se lee solo bajo _sessionsLock.</param>
        /// <param name="pickRandomIndex">Devuelve un índice al azar entre 0 y la cantidad dada, sin incluirla.</param>
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
                    replacedSession = PreserveMatchOnReplacementInLock(previous, out var matchViews)
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
            return EndSession(sessionToken, false, out sessionEnd, out _);
        }

        public bool TryLogOut(string sessionToken, out SessionEnd sessionEnd, out bool hasActiveParticipation)
        {
            return EndSession(sessionToken, true, out sessionEnd, out hasActiveParticipation);
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

        private SessionEnd PreserveMatchOnReplacementInLock(PlayerSession previous,
            out IReadOnlyList<MatchViewDelivery> matchViews)
        {
            matchViews = _noMatchViewDeliveries;
            int userId = previous.UserId;
            if (!_roomIdsByUserId.TryGetValue(userId, out int roomId)
                || !(_startsByRoomId[roomId].MatchId is Guid matchId)
                || !_matchesById.TryGetValue(matchId, out MatchRecord match))
            {
                return null;
            }

            DateTime nowUtc = _readUtcNow();
            bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);
            if (FindRecoverableMatchInLock(userId) == null)
            {
                if (changed)
                {
                    match.Version++;
                    matchViews = CreateViewDeliveriesInLock(match);
                }
                return null;
            }

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
            var recipients = FindPresenceRecipientsOfUser(userId);
            _friendsChannelsByUserId.TryGetValue(userId, out FriendsChannel friendsChannel);
            RemoveFriendsChannelOfUser(userId);
            RemoveInvitationsOfUser(userId);
            _authorizedRoomIdsByUserId.Remove(userId);
            _sessionsByToken.Remove(_tokensByUserId[userId]);
            return new SessionEnd(userId, previous.Nickname,
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotInRoom, userId),
                FindCallbackOfChannel(friendsChannel), recipients);
        }

        private SessionEnd EndSessionInLock(PlayerSession endedSession, RoomExitCause exitCause)
        {
            _sessionsByToken.Remove(_tokensByUserId[endedSession.UserId]);
            _tokensByUserId.Remove(endedSession.UserId);
            SessionEnd sessionEnd = RemoveSessionStateOfUser(endedSession, exitCause);

            return sessionEnd;
        }

        private static DateTime ReadSystemUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}