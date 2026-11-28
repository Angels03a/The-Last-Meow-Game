using TLMG.Domain.Authorization;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    internal sealed class IdentityAuthorizer : ISessionAuthorizer
    {
        private readonly ISessionIdentityProvider _identity;

        internal IdentityAuthorizer(ISessionIdentityProvider identity)
        {
            _identity = identity;
        }

        public SessionAuthorization Authorize(string sessionToken, SystemPermission requiredPermission)
        {
            SessionAuthorization decision = SessionAuthorization.CreateInvalidSession();
            if (_identity.TryGetUserId(sessionToken, out int userId))
            {
                PlayerSession session = PlayerSession.CreateRegistered(userId, "Player");
                if (RolePermissionMap.IsGranted(session.Role, requiredPermission))
                {
                    decision = SessionAuthorization.CreateGranted(session);
                }
                else
                {
                    decision = SessionAuthorization.CreateDenied(session);
                }
            }

            return decision;
        }
    }
}
