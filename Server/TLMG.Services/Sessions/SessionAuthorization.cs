using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public sealed class SessionAuthorization
    {
        private SessionAuthorization(AuthorizationStatus status, int userId, AccountRole role)
        {
            Status = status;
            UserId = userId;
            Role = role;
            ErrorKey = ResolveErrorKey(status);
        }

        public AuthorizationStatus Status { get; }

        public int UserId { get; }

        public AccountRole Role { get; }

        public string ErrorKey { get; }

        internal static SessionAuthorization CreateGranted(PlayerSession playerSession)
        {
            return new SessionAuthorization(AuthorizationStatus.Granted, playerSession.UserId, playerSession.Role);
        }

        internal static SessionAuthorization CreateDenied(PlayerSession playerSession)
        {
            return new SessionAuthorization(
                AuthorizationStatus.PermissionDenied,
                playerSession.UserId,
                playerSession.Role);
        }

        internal static SessionAuthorization CreateInvalidSession()
        {
            return new SessionAuthorization(
                AuthorizationStatus.InvalidSession,
                ValidationRules.NoUserId,
                AccountRole.Guest);
        }

        private static string ResolveErrorKey(AuthorizationStatus status)
        {
            string errorKey;

            switch (status)
            {
                case AuthorizationStatus.Granted:
                    errorKey = null;
                    break;
                case AuthorizationStatus.PermissionDenied:
                    errorKey = ErrorKeys.Common.PermissionDenied;
                    break;
                default:
                    errorKey = ErrorKeys.Common.InvalidSession;
                    break;
            }

            return errorKey;
        }
    }
}