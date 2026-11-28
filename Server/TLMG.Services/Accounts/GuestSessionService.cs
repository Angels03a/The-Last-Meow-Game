using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Sessions;

namespace TLMG.Services.Accounts
{
    public sealed class GuestSessionService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(GuestSessionService));

        private readonly IGuestSessionRegistry _guestSessionRegistry;

        public GuestSessionService(IGuestSessionRegistry guestSessionRegistry)
        {
            _guestSessionRegistry = guestSessionRegistry;
        }

        public OperationLogInResult StartGuestSession()
        {
            GuestSessionStart guestSessionStart = _guestSessionRegistry.StartGuestSession();
            OperationLogInResult operationLogInResult = new OperationLogInResult();

            if (guestSessionStart.IsStarted)
            {
                operationLogInResult.IsSuccess = true;
                operationLogInResult.ErrorKey = string.Empty;
                operationLogInResult.SessionToken = guestSessionStart.SessionToken;
                operationLogInResult.UserId = guestSessionStart.UserId;
                operationLogInResult.Nickname = guestSessionStart.Nickname;
                operationLogInResult.IsGuest = true;
            }
            else
            {
                operationLogInResult.ErrorKey = ErrorKeys.Login.GuestUnavailable;
                _log.Warn("No hay números de invitado disponibles; se rechazó el inicio de sesión como invitado.");
            }

            return operationLogInResult;
        }
    }
}