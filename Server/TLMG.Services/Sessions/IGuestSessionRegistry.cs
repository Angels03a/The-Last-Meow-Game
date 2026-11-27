namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Starts temporary guest sessions that live only in memory, because guests have no credentials or saved data.
    /// </summary>
    public interface IGuestSessionRegistry
    {
        /// <summary>Creates a guest session with a generated nickname and number.</summary>
        /// <returns>The started session, or a rejected result when every guest number is in use; never null.</returns>
        GuestSessionStart StartGuestSession();
    }
}