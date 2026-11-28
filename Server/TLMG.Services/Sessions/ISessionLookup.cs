namespace TLMG.Services.Sessions
{
    /// <summary>Looks up the player bound to a live session.</summary>
    public interface ISessionLookup
    {
        /// <summary>Finds a current session by its token.</summary>
        /// <param name="sessionToken">Client session token, which may be null or unknown.</param>
        /// <param name="playerSession">The session, or null if not found.</param>
        /// <returns>True if the token belongs to a current session.</returns>
        bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession);
    }
}
