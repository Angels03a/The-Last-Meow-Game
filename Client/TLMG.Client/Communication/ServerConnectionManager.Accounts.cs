using System;
using System.ServiceModel;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager : IAccountsConnection
    {
        private const string LogInConnectionErrorKey = "Login_NetworkError";
        private const string LogInUnexpectedErrorKey = "Login_DataModelError";
        private const string RegistrationConnectionErrorKey = "CreateAccount_NetworkError";
        private const string RegistrationUnexpectedErrorKey = "CreateAccount_DataModelError";

        public OperationRecoveryContextResult GetRecoveryContext()
        {
            return SendUsersManagerRequest(client => client.GetRecoveryContext(_sessionToken),
                "consultar el contexto recuperable", LogInConnectionErrorKey, LogInUnexpectedErrorKey);
        }

        public LogOutResult LogOut()
        {
            LogOutResult result = SendUsersManagerRequest(
                client => client.LogOut(_sessionToken), "cerrar sesión",
                "MainMenu_LogOutUnconfirmed", "MainMenu_LogOutUnconfirmed");

            if (!result.IsSuccess && result.ErrorKey == "MainMenu_LogOutUnconfirmed")
            {
                OperationResult validity = SendUsersManagerRequest(
                    client => client.CheckSession(_sessionToken), "comprobar el cierre de sesión",
                    "MainMenu_LogOutUnconfirmed", "MainMenu_LogOutUnconfirmed");
                if (validity.ErrorKey == "Common_InvalidSessionError")
                {
                    result = new LogOutResult { IsSuccess = true };
                }
            }

            if (result.IsSuccess || result.ErrorKey == "Common_InvalidSessionError")
            {
                Disconnect();
                result = new LogOutResult { IsSuccess = true };
            }

            return result;
        }

        public OperationLogInResult LogIn(string email, string password)
        {
            OperationLogInRequest operationLogInRequest = new OperationLogInRequest
            {
                Email = email,
                Password = password
            };

            OperationLogInResult operationLogInResult = SendUsersManagerRequest(
                usersClient => usersClient.LogIn(operationLogInRequest),
                "iniciar sesión",
                LogInConnectionErrorKey,
                LogInUnexpectedErrorKey);

            if (operationLogInResult.IsSuccess)
            {
                operationLogInResult = StartSessionFromLogIn(operationLogInResult);
            }

            return operationLogInResult;
        }

        public OperationResult RegisterPlayerAccount(
            PlayerAccountRegistration playerAccountRegistration)
        {
            OperationResult operationRegisterPlayerAccountResult = SendUsersManagerRequest(
                usersClient => usersClient.RegisterPlayerAccount(playerAccountRegistration),
                "registrar la cuenta",
                RegistrationConnectionErrorKey,
                RegistrationUnexpectedErrorKey);

            return operationRegisterPlayerAccountResult;
        }

        private OperationLogInResult StartSessionFromLogIn(OperationLogInResult operationLogInResult)
        {
            OperationLogInResult sessionResult = operationLogInResult;
            bool wasSessionStarted = StartRegisteredSession(operationLogInResult);

            if (wasSessionStarted)
            {
                operationLogInResult.SessionToken = null;
            }
            else
            {
                sessionResult = CreateFailedResult<OperationLogInResult>(LogInConnectionErrorKey);
            }

            return sessionResult;
        }

        private ResultType SendUsersManagerRequest<ResultType>(
            Func<UsersManagerClient, ResultType> sendRequest,
            string requestDescription,
            string connectionErrorKey,
            string unexpectedErrorKey)
            where ResultType : OperationResult, new()
        {
            UsersManagerClient usersClient = null;
            ResultType sendUsersManagerResult;

            try
            {
                usersClient = new UsersManagerClient();
                sendUsersManagerResult = sendRequest(usersClient);
            }
            catch (FaultException<SystemFault> systemFault)
            {
                _log.Error(
                    string.Format(
                        "El servidor reportó un error de infraestructura al {0}: {1}",
                        requestDescription,
                        systemFault.Detail.SystemErrorMessage),
                    systemFault);
                sendUsersManagerResult = CreateFailedResult<ResultType>(unexpectedErrorKey);
            }
            catch (FaultException faultException)
            {
                _log.Error(string.Format("El servidor devolvió una falla inesperada al {0}.", requestDescription),
                    faultException);
                sendUsersManagerResult = CreateFailedResult<ResultType>(unexpectedErrorKey);
            }
            catch (CommunicationException communicationException)
            {
                _log.Error(string.Format("Error de comunicación al {0}.", requestDescription),
                    communicationException);
                sendUsersManagerResult = CreateFailedResult<ResultType>(connectionErrorKey);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0}.", requestDescription), timeoutException);
                sendUsersManagerResult = CreateFailedResult<ResultType>(connectionErrorKey);
            }
            finally
            {
                CloseClientSafely(usersClient);
            }

            if (sendUsersManagerResult == null)
            {
                _log.Error(string.Format("El servidor respondió sin resultado al {0}.", requestDescription));
                sendUsersManagerResult = CreateFailedResult<ResultType>(unexpectedErrorKey);
            }

            return sendUsersManagerResult;
        }

        private static ResultType CreateFailedResult<ResultType>(string errorKey)
            where ResultType : OperationResult, new()
        {
            ResultType failedResult = new ResultType();
            failedResult.IsSuccess = false;
            failedResult.ErrorKey = errorKey;

            return failedResult;
        }

        public OperationResult RequestAccountVerificationCode(
            PlayerAccountRegistration playerAccountRegistration,
            string cultureCode)
        {
            OperationResult operationResult = SendUsersManagerRequest(
                usersClient => usersClient.RequestAccountVerificationCode(
                    playerAccountRegistration,
                    cultureCode),
                "solicitar el código de verificación",
                RegistrationConnectionErrorKey,
                RegistrationUnexpectedErrorKey);

            return operationResult;
        }

        public OperationResult CompletePlayerAccountRegistration(
            PlayerAccountRegistration playerAccountRegistration,
            string verificationCode)
        {
            OperationResult operationResult = SendUsersManagerRequest(
                usersClient => usersClient.CompletePlayerAccountRegistration(
                    playerAccountRegistration,
                    verificationCode),
                "completar el registro de la cuenta",
                RegistrationConnectionErrorKey,
                RegistrationUnexpectedErrorKey);

            return operationResult;
        }

        public OperationResult ResendAccountVerificationCode(string email, string cultureCode)
        {
            OperationResult operationResult = SendUsersManagerRequest(
                usersClient => usersClient.ResendAccountVerificationCode(email, cultureCode),
                "reenviar el código de verificación",
                RegistrationConnectionErrorKey,
                RegistrationUnexpectedErrorKey);

            return operationResult;
        }
    }
}