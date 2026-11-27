using TLMG.Domain.Authorization;
using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry : ISessionAuthorizer, IGuestSessionRegistry
    {
        private const int NoGuestNumber = 0;

        private int _lastGuestNumber;

        public SessionAuthorization Authorize(string sessionToken, SystemPermission requiredPermission)
        {
            SessionAuthorization authorization = SessionAuthorization.CreateInvalidSession();

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    PlayerSession playerSession;

                    if (_sessionsByToken.TryGetValue(sessionToken, out playerSession))
                    {
                        playerSession.LastActivityUtc = _readUtcNow();
                        authorization = CreateAuthorization(playerSession, requiredPermission);
                    }
                }
            }

            return authorization;
        }

        public GuestSessionStart StartGuestSession()
        {
            GuestSessionStart guestSessionStart = GuestSessionStart.CreateRejected();
            string sessionToken = SessionTokenHelper.GenerateSessionToken();

            lock (_sessionsLock)
            {
                int guestNumber = AllocateGuestNumberInLock();

                if (guestNumber != NoGuestNumber)
                {
                    int guestUserId = GuestIdentity.CreateUserId(guestNumber);
                    PlayerSession guestSession =
                        PlayerSession.CreateGuest(guestUserId, GuestIdentity.CreateNickname(guestNumber));
                    guestSession.LastActivityUtc = _readUtcNow();
                    _sessionsByToken[sessionToken] = guestSession;
                    _tokensByUserId[guestUserId] = sessionToken;
                    guestSessionStart = GuestSessionStart.CreateStarted(sessionToken, guestSession);
                }
            }

            if (guestSessionStart.IsStarted)
            {
                _log.InfoFormat("Sesión de invitado iniciada para el jugador {0}.", guestSessionStart.UserId);
            }

            return guestSessionStart;
        }

        private static SessionAuthorization CreateAuthorization(
            PlayerSession playerSession,
            SystemPermission requiredPermission)
        {
            SessionAuthorization authorization;

            if (RolePermissionMap.IsGranted(playerSession.Role, requiredPermission))
            {
                authorization = SessionAuthorization.CreateGranted(playerSession);
            }
            else
            {
                authorization = SessionAuthorization.CreateDenied(playerSession);
            }

            return authorization;
        }

        private int AllocateGuestNumberInLock()
        {
            int guestNumber = NoGuestNumber;
            int attemptCount = 0;

            while (guestNumber == NoGuestNumber && attemptCount < GuestIdentity.MaximumGuestNumber)
            {
                _lastGuestNumber = _lastGuestNumber % GuestIdentity.MaximumGuestNumber
                    + GuestIdentity.MinimumGuestNumber;
                attemptCount++;

                if (!_tokensByUserId.ContainsKey(GuestIdentity.CreateUserId(_lastGuestNumber)))
                {
                    guestNumber = _lastGuestNumber;
                }
            }

            return guestNumber;
        }
    }
}