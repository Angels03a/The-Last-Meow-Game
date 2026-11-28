using System;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ISessionManager
    {
        public OperationLogInResult StartGuestSession()
        {
            SafeNotifyAll(_sessionRegistry.EvaluateConnectionDeadlines());
            OperationLogInResult result;

            try
            {
                result = _guestSessionService.StartGuestSession();
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al iniciar una sesión de invitado.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return result;
        }
    }
}