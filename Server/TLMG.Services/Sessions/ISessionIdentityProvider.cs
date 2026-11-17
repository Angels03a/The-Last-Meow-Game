namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resolves and updates the identity bound to a session token so services never trust a user id sent by the
    /// client.
    /// </summary>
    public interface ISessionIdentityProvider
    {
        /// <summary>Finds the account that owns a live session.</summary>
        /// <param name="sessionToken">Token issued at log in.</param>
        /// <param name="userId">The owner account; zero when the session is not valid.</param>
        /// <returns>True when the session exists.</returns>
        bool TryGetUserId(string sessionToken, out int userId);

        /// <summary>Keeps the in-memory nickname equal to the stored one so rooms and invitations show the new name.</summary>
        /// <param name="sessionToken">Token issued at log in.</param>
        /// <param name="nickname">Nickname already saved in the database.</param>
        /// <returns>True when the session exists and was updated.</returns>
        bool TryUpdateNickname(string sessionToken, string nickname);
    }
}