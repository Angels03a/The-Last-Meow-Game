using System;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IPasswordManager
    {
        public OperationResult ChangePassword(OperationChangePasswordRequest changePasswordRequest)
        {
            OperationResult changePasswordResult;

            try
            {
                changePasswordResult = _passwordChangeService.ChangePassword(changePasswordRequest);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al cambiar una contraseña.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return changePasswordResult;
        }

        public OperationResult RequestPasswordResetCode(string email, string cultureCode)
        {
            OperationResult passwordResetCodeRequest;

            try
            {
                passwordResetCodeRequest = _passwordRecoveryService.RequestResetCode(email, cultureCode);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al solicitar el código de recuperación.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return passwordResetCodeRequest;
        }

        public OperationResult VerifyPasswordResetCode(string email, string verificationCode)
        {
            OperationResult verificationPasswordResetCode;

            try
            {
                verificationPasswordResetCode = _passwordRecoveryService.VerifyResetCode(email, verificationCode);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al verificar el código de recuperación.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return verificationPasswordResetCode;
        }

        public OperationResult ResetPassword(OperationResetPasswordRequest resetPasswordRequest)
        {
            OperationResult resetPassword;

            try
            {
                resetPassword = _passwordRecoveryService.ResetPassword(resetPasswordRequest);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente al restablecer una contraseña.", outOfMemoryException);
                throw CreateOutOfMemoryFault();
            }

            return resetPassword;
        }
    }
}