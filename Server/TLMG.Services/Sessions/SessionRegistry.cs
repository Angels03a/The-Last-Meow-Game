using System.Collections.Generic;

using log4net;

using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Guarda las sesiones iniciadas y las salas (SessionRegistry.Lobbies.cs). Un solo candado protege todo el estado
    /// compartido y dentro de él no hay datos, callbacks, log ni esperas: solo se lee o modifica memoria, se copia lo
    /// necesario y el log y los avisos se hacen siempre después de salir del candado.
    /// </summary>
    public partial class SessionRegistry : ISessionRegistry
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SessionRegistry));

        private readonly object _sessionsLock = new object();
        private readonly Dictionary<string, PlayerSession> _sessionsByToken =
            new Dictionary<string, PlayerSession>();
        private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();

        /// <inheritdoc />
        public string StartSession(int userId, string nickname)
        {
            string sessionToken = SessionTokenHelper.GenerateSessionToken();
            PlayerSession newSession = new PlayerSession(userId, nickname);
            string previousToken;
            bool wasReplaced;
            int departedRoomId = LobbyMembershipChange.NoRoomId;

            lock (_sessionsLock)
            {
                wasReplaced = _tokensByUserId.TryGetValue(userId, out previousToken);

                if (wasReplaced)
                {
                    _sessionsByToken.Remove(previousToken);
                    departedRoomId = RemoveLobbyStateOfUser(userId);
                }

                _sessionsByToken[sessionToken] = newSession;
                _tokensByUserId[userId] = sessionToken;
            }

            if (wasReplaced)
            {
                _log.InfoFormat("La sesión anterior del jugador {0} fue reemplazada por un nuevo inicio.", userId);
                LogDepartureOfEndedSession(userId, departedRoomId);
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
        public bool EndSession(string sessionToken)
        {
            PlayerSession endedSession = null;
            int departedRoomId = LobbyMembershipChange.NoRoomId;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    if (_sessionsByToken.TryGetValue(sessionToken, out endedSession))
                    {
                        _sessionsByToken.Remove(sessionToken);
                        _tokensByUserId.Remove(endedSession.UserId);
                        departedRoomId = RemoveLobbyStateOfUser(endedSession.UserId);
                    }
                }
            }

            bool wasEnded = endedSession != null;

            if (wasEnded)
            {
                _log.InfoFormat("Sesión cerrada para el jugador {0}.", endedSession.UserId);
                LogDepartureOfEndedSession(endedSession.UserId, departedRoomId);
            }

            return wasEnded;
        }

        private static void LogDepartureOfEndedSession(int userId, int departedRoomId)
        {
            if (departedRoomId != LobbyMembershipChange.NoRoomId)
            {
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1} porque su sesión terminó.",
                    userId,
                    departedRoomId);
            }
        }
    }
}