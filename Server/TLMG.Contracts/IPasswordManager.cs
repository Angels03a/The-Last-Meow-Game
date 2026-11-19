using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Request-reply contract for password operations, apart from IUsersManager so neither interface passes the
    /// segregation limit.
    /// </summary>
    [ServiceContract]
    public interface IPasswordManager
    {
        /// <summary>Changes the password of the session owner after checking the current one.</summary>
        /// <param name="changePasswordRequest">Session token, current and new password.</param>
        /// <returns>Success or the error key so the client can translate it.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult ChangePassword(OperationChangePasswordRequest changePasswordRequest);

        /// <summary>Sends a recovery code; it answers the same whether or not the email has an account.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="cultureCode">Active client culture code.</param>
        /// <returns>Success or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult RequestPasswordResetCode(string email, string cultureCode);

        /// <summary>Verifies the recovery code and marks it as used.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="verificationCode">Six-digit code received by email.</param>
        /// <returns>Success or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult VerifyPasswordResetCode(string email, string verificationCode);

        /// <summary>Sets a new password using a code that was already verified.</summary>
        /// <param name="resetPasswordRequest">Email, verified code and new password.</param>
        /// <returns>Success or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult ResetPassword(OperationResetPasswordRequest resetPasswordRequest);
    }
}