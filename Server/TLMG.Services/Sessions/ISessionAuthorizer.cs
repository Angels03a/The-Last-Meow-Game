using TLMG.Domain.Authorization;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Decides whether a session may perform an action, so a modified client cannot skip a disabled button.
    /// </summary>
    public interface ISessionAuthorizer
    {
        /// <summary>
        /// Checks the token and the permission of its role in one step; the role is fixed when the session starts.
        /// </summary>
        /// <param name="sessionToken">Token received from the client; may be null or unknown.</param>
        /// <param name="requiredPermission">Permission the operation needs.</param>
        /// <returns>The decision with the user id and the error key to answer with; never null.</returns>
        SessionAuthorization Authorize(string sessionToken, SystemPermission requiredPermission);
    }
}