using System;
using System.ServiceModel;
using System.Windows;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager : IPasswordConnection
    {
        private const string PasswordConnectionErrorKey = "Password_NetworkError";
        private const string PasswordUnexpectedErrorKey = "Password_DataModelError";
        private const string PasswordInvalidSessionErrorKey = "Common_InvalidSessionError";

        public OperationResult ChangePassword(string currentPassword, string newPassword)
        {
            OperationResult changeResult;

            if (HasRegisteredSession)
            {
                OperationChangePasswordRequest changeRequest = new OperationChangePasswordRequest
                {
                    SessionToken = _sessionToken,
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                };

                changeResult = SendPasswordManagerRequest(
                    passwordClient => passwordClient.ChangePassword(changeRequest),
                    "cambiar la contraseña");
            }
            else
            {
                changeResult = CreateFailedResult<OperationResult>(PasswordInvalidSessionErrorKey);
            }

            return changeResult;
        }

        public OperationResult RequestPasswordResetCode(string email, string cultureCode)
        {
            OperationResult requestResult = SendPasswordManagerRequest(
                passwordClient => passwordClient.RequestPasswordResetCode(email, cultureCode),
                "solicitar el código de recuperación");

            if (!requestResult.IsSuccess && (string.IsNullOrWhiteSpace(requestResult.ErrorKey)
                || (Application.Current != null
                    && !(Application.Current.TryFindResource(requestResult.ErrorKey) is string))))
            {
                _log.Warn("El servidor devolvió una clave de error inválida al solicitar el código de recuperación.");
                requestResult.ErrorKey = PasswordUnexpectedErrorKey;
            }

            return requestResult;
        }

        public OperationResult VerifyPasswordResetCode(string email, string verificationCode)
        {
            OperationResult verifyResult = SendPasswordManagerRequest(
                passwordClient => passwordClient.VerifyPasswordResetCode(email, verificationCode),
                "verificar el código de recuperación");

            return verifyResult;
        }

        public OperationResult ResetPassword(string email, string verificationCode, string newPassword)
        {
            OperationResetPasswordRequest resetRequest = new OperationResetPasswordRequest
            {
                Email = email,
                VerificationCode = verificationCode,
                NewPassword = newPassword
            };

            OperationResult resetResult = SendPasswordManagerRequest(
                passwordClient => passwordClient.ResetPassword(resetRequest),
                "restablecer la contraseña");

            return resetResult;
        }

        private OperationResult SendPasswordManagerRequest(
            Func<PasswordManagerClient, OperationResult> sendRequest,
            string requestDescription)
        {
            PasswordManagerClient passwordClient = null;
            OperationResult requestResult;

            try
            {
                passwordClient = new PasswordManagerClient();
                requestResult = sendRequest(passwordClient);
            }
            catch (FaultException<SystemFault> systemFault)
            {
                _log.Error(
                    string.Format(
                        "El servidor reportó un error de infraestructura al {0}: {1}",
                        requestDescription,
                        systemFault.Detail.SystemErrorMessage),
                    systemFault);
                requestResult = CreateFailedResult<OperationResult>(PasswordUnexpectedErrorKey);
            }
            catch (FaultException faultException)
            {
                _log.Error(
                    string.Format("El servidor devolvió una falla inesperada al {0}.", requestDescription),
                    faultException);
                requestResult = CreateFailedResult<OperationResult>(PasswordUnexpectedErrorKey);
            }
            catch (CommunicationException communicationException)
            {
                _log.Error(string.Format("Error de comunicación al {0}.", requestDescription), communicationException);
                requestResult = CreateFailedResult<OperationResult>(PasswordConnectionErrorKey);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0}.", requestDescription), timeoutException);
                requestResult = CreateFailedResult<OperationResult>(PasswordConnectionErrorKey);
            }
            finally
            {
                CloseClientSafely(passwordClient);
            }

            if (requestResult == null)
            {
                _log.Error(string.Format("El servidor respondió sin resultado al {0}.", requestDescription));
                requestResult = CreateFailedResult<OperationResult>(PasswordUnexpectedErrorKey);
            }

            return requestResult;
        }
    }
}
