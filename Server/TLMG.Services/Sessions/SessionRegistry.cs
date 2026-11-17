using System;
using System.Collections.Generic;

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
        private readonly Dictionary<string, PlayerSession> _sessionsByToken =
            new Dictionary<string, PlayerSession>();
        private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();

        /// <summary>Crea el registro con el reloj UTC del sistema.</summary>
        public SessionRegistry() : this(ReadSystemUtcNow)
        {
        }

        /// <summary>
        /// Crea el registro con la fuente de hora UTC indicada; permite a las pruebas vencer la cuenta regresiva sin
        /// esperar. El servidor usa el constructor sin parámetros.
        /// </summary>
        /// <param name="readUtcNow">Devuelve la hora UTC actual; se lee solo bajo _sessionsLock.</param>
        internal SessionRegistry(Func<DateTime> readUtcNow)
        {
            _readUtcNow = readUtcNow;
        }

        /// <inheritdoc />
        public string StartSession(int userId, string nickname, out SessionEnd replacedSession)
        {
            string sessionToken = SessionTokenHelper.GenerateSessionToken();
            PlayerSession newSession = new PlayerSession(userId, nickname);
            replacedSession = null;

            lock (_sessionsLock)
            {
                string previousToken;

                if (_tokensByUserId.TryGetValue(userId, out previousToken))
                {
                    replacedSession = EndSessionInLock(_sessionsByToken[previousToken]);
                }

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
                }
            }

            return wasFound;
        }

        /// <inheritdoc />
        public bool EndSession(string sessionToken, out SessionEnd sessionEnd)
        {
            sessionEnd = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    PlayerSession endedSession;

                    if (_sessionsByToken.TryGetValue(sessionToken, out endedSession))
                    {
                        sessionEnd = EndSessionInLock(endedSession);
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
            if (sessionEnd.RoomDeparture.RoomId != LobbyMembershipChange.NoRoomId)
            {
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1} porque su sesión terminó.",
                    sessionEnd.UserId,
                    sessionEnd.RoomDeparture.RoomId);
            }
        }

        private SessionEnd EndSessionInLock(PlayerSession endedSession)
        {
            _sessionsByToken.Remove(_tokensByUserId[endedSession.UserId]);
            _tokensByUserId.Remove(endedSession.UserId);
            SessionEnd sessionEnd = RemoveSessionStateOfUser(endedSession);

            return sessionEnd;
        }

        private static DateTime ReadSystemUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}