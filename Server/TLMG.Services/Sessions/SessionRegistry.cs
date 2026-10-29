using System.Collections.Generic;

using log4net;

using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Guarda las sesiones iniciadas. El candado protege solo los dos diccionarios y dentro de él no hay datos,
    /// callbacks, log ni esperas; el log se escribe siempre después de salir del candado.
    /// </summary>
    public class SessionRegistry : ISessionRegistry
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

            lock (_sessionsLock)
            {
                wasReplaced = _tokensByUserId.TryGetValue(userId, out previousToken);

                if (wasReplaced)
                {
                    _sessionsByToken.Remove(previousToken);
                }

                _sessionsByToken[sessionToken] = newSession;
                _tokensByUserId[userId] = sessionToken;
            }

            if (wasReplaced)
            {
                _log.InfoFormat("La sesión anterior del jugador {0} fue reemplazada por un nuevo inicio.", userId);
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

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    if (_sessionsByToken.TryGetValue(sessionToken, out endedSession))
                    {
                        _sessionsByToken.Remove(sessionToken);
                        _tokensByUserId.Remove(endedSession.UserId);
                    }
                }
            }

            bool wasEnded = endedSession != null;

            if (wasEnded)
            {
                _log.InfoFormat("Sesión cerrada para el jugador {0}.", endedSession.UserId);
            }

            return wasEnded;
        }
    }
}