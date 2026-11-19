using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Password operations of the connection with the server. The session token stays inside the connection layer
    /// so ViewModels never handle credentials; results carry the ErrorKey sent by the server.
    /// </summary>
    public interface IPasswordConnection
    {
        /// <summary>Changes the password of the current session.</summary>
        /// <param name="currentPassword">Current password.</param>
        /// <param name="newPassword">New password.</param>
        /// <returns>Success or the error key to translate; never null.</returns>
        OperationResult ChangePassword(string currentPassword, string newPassword);

        /// <summary>Asks the server to send a recovery code.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="cultureCode">Active culture code.</param>
        /// <returns>Success or the error key to translate; never null.</returns>
        OperationResult RequestPasswordResetCode(string email, string cultureCode);

        /// <summary>Verifies the recovery code.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="verificationCode">Code typed by the player.</param>
        /// <returns>Success or the error key to translate; never null.</returns>
        OperationResult VerifyPasswordResetCode(string email, string verificationCode);

        /// <summary>Sets the new password with an already verified code.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="verificationCode">Verified code.</param>
        /// <param name="newPassword">New password.</param>
        /// <returns>Success or the error key to translate; never null.</returns>
        OperationResult ResetPassword(string email, string verificationCode, string newPassword);
    }
}