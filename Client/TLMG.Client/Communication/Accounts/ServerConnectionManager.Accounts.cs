using System;
using System.ServiceModel;
using System.Windows;
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
                "consultar el contexto recuperable", LogInConnectionErrorKey, LogInUnexpectedErrorKey, true);
        }

        public LogOutResult LogOut()
        {
            LogOutResult result = SendUsersManagerRequest(
                client => client.LogOut(_sessionToken), "cerrar sesión",
                "MainMenu_LogOutUnconfirmed", "MainMenu_LogOutUnconfirmed");

            NormalizeAccountResult(result, "MainMenu_LogOutUnconfirmed");

            if (!result.IsSuccess && result.ErrorKey == "MainMenu_LogOutUnconfirmed")
            {
                OperationResult validity = SendUsersManagerRequest(
                    client => client.CheckSession(_sessionToken), "comprobar el cierre de sesión",
                    "MainMenu_LogOutUnconfirmed", "MainMenu_LogOutUnconfirmed");
                if (!validity.IsSuccess && validity.ErrorKey == "Common_InvalidSessionError")
                {
                    result = new LogOutResult { IsSuccess = true, ErrorKey = string.Empty };
                }
            }

            if (result.IsSuccess || result.ErrorKey == "Common_InvalidSessionError")
            {
                Disconnect();
                result = new LogOutResult { IsSuccess = true, ErrorKey = string.Empty };
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

            NormalizeAccountResult(operationLogInResult, LogInUnexpectedErrorKey);

            if (operationLogInResult.IsSuccess)
            {
                operationLogInResult = StartSessionFromLogIn(operationLogInResult);
            }

            return operationLogInResult;
        }

        public OperationLogInResult StartGuestSession()
        {
            OperationLogInResult guestResult = SendGuestSessionRequest();
            NormalizeAccountResult(guestResult, LogInUnexpectedErrorKey);

            if (guestResult.IsSuccess)
            {
                guestResult = StartSessionFromLogIn(guestResult);
            }

            return guestResult;
        }

        public OperationResult RegisterPlayerAccount(
            PlayerAccountRegistration playerAccountRegistration)
        {
            OperationResult operationRegisterPlayerAccountResult = SendUsersManagerRequest(
                usersClient => usersClient.RegisterPlayerAccount(playerAccountRegistration),
                "registrar la cuenta",
                RegistrationConnectionErrorKey,
                RegistrationUnexpectedErrorKey);

            NormalizeAccountResult(operationRegisterPlayerAccountResult, RegistrationUnexpectedErrorKey);
            return operationRegisterPlayerAccountResult;
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

            NormalizeAccountResult(operationResult, RegistrationUnexpectedErrorKey);
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

            NormalizeAccountResult(operationResult, RegistrationUnexpectedErrorKey);
            return operationResult;
        }

        public OperationResult ResendAccountVerificationCode(string email, string cultureCode)
        {
            OperationResult operationResult = SendUsersManagerRequest(
                usersClient => usersClient.ResendAccountVerificationCode(email, cultureCode),
                "reenviar el código de verificación",
                RegistrationConnectionErrorKey,
                RegistrationUnexpectedErrorKey);

            NormalizeAccountResult(operationResult, RegistrationUnexpectedErrorKey);
            return operationResult;
        }

        private static ResultType CreateFailedResult<ResultType>(string errorKey)
            where ResultType : OperationResult, new()
        {
            ResultType failedResult = new ResultType();
            failedResult.IsSuccess = false;
            failedResult.ErrorKey = errorKey;

            return failedResult;
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

        private OperationLogInResult SendGuestSessionRequest()
        {
            SessionManagerClient sessionClient = null;
            OperationLogInResult guestResult;

            try
            {
                sessionClient = new SessionManagerClient();
                guestResult = sessionClient.StartGuestSession();
            }
            catch (FaultException<SystemFault> systemFault)
            {
                _log.Error(
                    string.Format(
                        "El servidor reportó un error de infraestructura al iniciar sesión como invitado: {0}",
                        systemFault.Detail.SystemErrorMessage),
                    systemFault);
                guestResult = CreateFailedResult<OperationLogInResult>(LogInUnexpectedErrorKey);
            }
            catch (FaultException faultException)
            {
                _log.Error("El servidor devolvió una falla inesperada al iniciar sesión como invitado.",
                    faultException);
                guestResult = CreateFailedResult<OperationLogInResult>(LogInUnexpectedErrorKey);
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al iniciar sesión como invitado.", communicationException);
                guestResult = CreateFailedResult<OperationLogInResult>(LogInConnectionErrorKey);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("Se agotó el tiempo al iniciar sesión como invitado.", timeoutException);
                guestResult = CreateFailedResult<OperationLogInResult>(LogInConnectionErrorKey);
            }
            finally
            {
                CloseClientSafely(sessionClient);
            }

            if (guestResult == null)
            {
                _log.Error("El servidor respondió sin resultado al iniciar sesión como invitado.");
                guestResult = CreateFailedResult<OperationLogInResult>(LogInUnexpectedErrorKey);
            }

            return guestResult;
        }

        private ResultType SendUsersManagerRequest<ResultType>(
            Func<UsersManagerClient, ResultType> sendRequest,
            string requestDescription,
            string connectionErrorKey,
            string unexpectedErrorKey, bool recoveryRequest = false)
            where ResultType : OperationResult, new()
        {
            UsersManagerClient usersClient = null;
            ResultType sendUsersManagerResult;

            try
            {
                usersClient = new UsersManagerClient();
                if (recoveryRequest)
                {
                    OpenConnection(usersClient);
                    IClientChannel channel = usersClient.InnerChannel;
                    ResultType result = null;
                    SendConnectionRequest(channel, () => result = sendRequest(usersClient));
                    sendUsersManagerResult = result;
                }
                else
                {
                    sendUsersManagerResult = sendRequest(usersClient);
                }
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
                if (recoveryRequest)
                {
                    usersClient?.Abort();
                }
                else
                {
                    CloseClientSafely(usersClient);
                }
            }

            if (sendUsersManagerResult == null)
            {
                _log.Error(string.Format("El servidor respondió sin resultado al {0}.", requestDescription));
                sendUsersManagerResult = CreateFailedResult<ResultType>(unexpectedErrorKey);
            }

            return sendUsersManagerResult;
        }

        private static void NormalizeAccountResult(OperationResult result, string unexpectedErrorKey)
        {
            if (result.IsSuccess)
            {
                if (string.IsNullOrEmpty(result.ErrorKey))
                {
                    result.ErrorKey = string.Empty;
                }
                else
                {
                    _log.Warn("El servidor devolvió un resultado de cuenta inconsistente.");
                    result.IsSuccess = false;
                    result.ErrorKey = unexpectedErrorKey;
                }
            }
            else if (string.IsNullOrWhiteSpace(result.ErrorKey)
                || (Application.Current != null && !(Application.Current.TryFindResource(result.ErrorKey) is string)))
            {
                _log.Warn("El servidor devolvió una clave de error de cuenta inválida.");
                result.ErrorKey = unexpectedErrorKey;
            }
        }
    }
}
