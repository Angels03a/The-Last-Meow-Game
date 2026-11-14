using System;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IUsersManager
    {
        public OperationResult RegisterPlayerAccount(
            PlayerAccountRegistration registrationPlayerAccount)
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

                throw new FaultException<SystemFault>(
                    fault,
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return operationRegisterPlayerAccountResult;
        }

        public OperationResult RequestAccountVerificationCode(
            PlayerAccountRegistration playerAccountRegistration,
            string cultureCode)
        {
            OperationResult operationResult;

            try
            {
                operationResult =
                    _playerAccountRegistrationService.RequestVerificationCode(
                        playerAccountRegistration,
                        cultureCode);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al solicitar el código de verificación.", outOfMemoryException);

                SystemFault fault = new SystemFault
                {
                    SystemErrorMessage = ErrorKeys.Common.OutOfMemory
                };

                throw new FaultException<SystemFault>(
                    fault,
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return operationResult;
        }

        public OperationResult CompletePlayerAccountRegistration(PlayerAccountRegistration playerAccountRegistration,
            string verificationCode)
        {
            OperationResult operationResult;

            try
            {
                operationResult =
                    _playerAccountRegistrationService.CompleteRegistration(
                        playerAccountRegistration,
                        verificationCode);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al completar el registro de la cuenta.", outOfMemoryException);

                SystemFault fault = new SystemFault
                {
                    SystemErrorMessage = ErrorKeys.Common.OutOfMemory
                };

                throw new FaultException<SystemFault>(
                    fault,
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return operationResult;
        }

        public OperationResult ResendAccountVerificationCode(string email, string cultureCode)
        {
            OperationResult operationResult;

            try
            {
                operationResult =
                    _playerAccountRegistrationService.ResendVerificationCode(email, cultureCode);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al reenviar el código de verificación.", outOfMemoryException);

                SystemFault fault = new SystemFault
                {
                    SystemErrorMessage = ErrorKeys.Common.OutOfMemory
                };

                throw new FaultException<SystemFault>(
                    fault,
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return operationResult;
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

                throw new FaultException<SystemFault>(
                    fault,
                    new FaultReason("Memoria insuficiente en el servidor."));
            }

            return result;
        }
    }
}