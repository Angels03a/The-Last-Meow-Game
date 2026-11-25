using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Accounts;

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

                SystemFault fault = new SystemFault
                {
                    SystemErrorMessage = ErrorKeys.Common.OutOfMemory
                };

                throw new FaultException<SystemFault>(
                    fault,
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return result;
        }
    }
}