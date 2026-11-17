using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Profile operations for the logged-in player; the session token stays inside the connection layer so
    /// ViewModels never handle credentials.
    /// </summary>
    public interface IProfileConnection
    {
        /// <summary>Requests the server-side profile of the current session.</summary>
        /// <returns>The profile data or the error key to translate.</returns>
        OperationPlayerProfileResult GetPlayerProfile();

        /// <summary>Sends the new nickname and avatar for the current session.</summary>
        /// <param name="nickname">Requested nickname.</param>
        /// <param name="avatarKey">Requested avatar key.</param>
        /// <returns>Success or the error key to translate.</returns>
        OperationResult UpdatePlayerProfile(string nickname, string avatarKey);
    }
}