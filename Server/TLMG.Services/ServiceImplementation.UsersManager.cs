using System;
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
                throw CreateOutOfMemoryFault();
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
                throw CreateOutOfMemoryFault();
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
                throw CreateOutOfMemoryFault();
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
                throw CreateOutOfMemoryFault();
            }

            return operationResult;
        }

        public OperationRecoveryContextResult GetRecoveryContext(string sessionToken)
        {
            ResultWithNotification<OperationRecoveryContextResult> result = _sessionRegistry.GetRecoveryContext(sessionToken);
            SafeNotifyAll(result.Notifications);
            return result.OperationResult;
        }

        public OperationResult CheckSession(string sessionToken)
        {
            SafeNotifyAll(_sessionRegistry.EvaluateConnectionDeadlines());
            bool isValid = _sessionRegistry.TryFindSessionByToken(sessionToken, out _);
            string errorKey = ErrorKeys.Common.InvalidSession;
            if (isValid)
            {
                errorKey = string.Empty;
            }

            return new OperationResult
            {
                IsSuccess = isValid,
                ErrorKey = errorKey
            };
        }

        public LogOutResult LogOut(string sessionToken)
        {
            SafeNotifyAll(_sessionRegistry.EvaluateConnectionDeadlines());
            bool wasRevoked = _sessionRegistry.TryLogOut(sessionToken, out SessionEnd sessionEnd,
                out bool hasActiveParticipation);
            if (wasRevoked)
            {
                SafeNotifyAll(Helpers.SessionNotificationHelper.CreateSessionEndNotifications(sessionEnd));
            }

            string errorKey = ErrorKeys.Common.InvalidSession;
            if (wasRevoked)
            {
                errorKey = string.Empty;
            }
            else if (hasActiveParticipation)
            {
                errorKey = ErrorKeys.Common.LogOutActiveMatch;
            }

            return new LogOutResult
            {
                IsSuccess = wasRevoked,
                ErrorKey = errorKey
            };
        }

        public OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest)
        {
            SafeNotifyAll(_sessionRegistry.EvaluateConnectionDeadlines());
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
                throw CreateOutOfMemoryFault();
            }

            return result;
        }

        public OperationPlayerProfileResult GetPlayerProfile(string sessionToken)
        {
            OperationPlayerProfileResult profileResult;

            try
            {
                profileResult = _playerProfileService.GetProfile(sessionToken);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente en el servidor al obtener un perfil.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return profileResult;
        }

        public OperationResult UpdatePlayerProfile(OperationUpdatePlayerProfileRequest updateProfileRequest)
        {
            OperationResult updateResult;

            try
            {
                updateResult = _playerProfileService.UpdateProfile(updateProfileRequest);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente en el servidor al actualizar un perfil.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return updateResult;
        }
    }
}
