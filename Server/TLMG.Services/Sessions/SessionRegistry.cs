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
                    replacedSession = EndSessionInLock(
                        _sessionsByToken[previousToken],
                        RoomExitCause.SessionReplaced);
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
                        sessionEnd = EndSessionInLock(endedSession, RoomExitCause.SessionEnded);
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