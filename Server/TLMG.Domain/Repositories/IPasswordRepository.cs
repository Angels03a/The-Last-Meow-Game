namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Persistence access for password changes; the database applies each change atomically so a stale read or a
    /// reused code can never overwrite a newer password.
    /// </summary>
    public interface IPasswordRepository
    {
        /// <summary>Reads the stored hash of an account so the service can verify the current password.</summary>
        /// <param name="userId">Account identifier resolved from the session.</param>
        /// <returns>The hash and its lookup status; never null.</returns>
        PasswordHashLookup FindPasswordHashByUserId(int userId);

        /// <summary>Replaces the hash only if the stored one still equals the hash that was verified.</summary>
        /// <param name="userId">Account identifier resolved from the session.</param>
        /// <param name="currentHash">Stored hash the service verified.</param>
        /// <param name="newHash">Hash of the new password.</param>
        /// <returns>Changed, StaleCredentials, UserNotFound or DataFailure.</returns>
        PasswordChangeStatus ChangePassword(int userId, string currentHash, string newHash);

        /// <summary>Replaces the hash if the account has a recently verified, unused recovery code.</summary>
        /// <param name="email">Normalized email of the account.</param>
        /// <param name="codeHash">Hash of the already verified code.</param>
        /// <param name="newPasswordHash">Hash of the new password.</param>
        /// <returns>Reset, NoVerifiedCode, UserNotFound or DataFailure.</returns>
        PasswordResetStatus ResetPasswordWithVerifiedCode(string email, string codeHash, string newPasswordHash);
    }
}