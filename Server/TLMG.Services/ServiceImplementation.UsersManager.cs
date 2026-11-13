using System;
using System.ServiceModel;
using TLMG.Contracts.DataContracts;
using TLMG.Contracts;
using TLMG.Domain.Constants;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IUsersManager
    {
        public OperationResult RegisterPlayerAccount(PlayerAccountRegistration 
            registrationPlayerAccount)
        {
            OperationResult operationRegisterPlayerAccountResult;

            try
            {
                operationRegisterPlayerAccountResult = 
                    _playerAccountRegistrationService.RegisterPlayerAccount(registrationPlayerAccount);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante el registro de cuenta de jugador.", outOfMemoryException);

                SystemFault fault = new SystemFault
                {
                    SystemErrorMessage = ErrorKeys.Common.OutOfMemory
                };

                throw new FaultException<SystemFault>(fault, 
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return operationRegisterPlayerAccountResult;
        }

        public OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest)
        {
            OperationLogInResult result;

            try
            {
                ResultWithNotification<OperationLogInResult> loginResult = 
                    _playerLogInService.LogIn(operationLogInRequest);

                SafeNotifyAll(loginResult.Notifications);

                result = loginResult.OperationResult;
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante el inicio de sesión.", outOfMemoryException);

                SystemFault fault = new SystemFault
                {
                    SystemErrorMessage = ErrorKeys.Common.OutOfMemory
                };

                throw new FaultException<SystemFault>(fault, 
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return result;
        }
    }
}