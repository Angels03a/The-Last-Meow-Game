using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Request-reply contract to start sessions that do not need credentials.
    /// </summary>
    [ServiceContract]
    public interface ISessionManager
    {
        /// <summary>Starts a temporary guest session, because guests have no account to log in with.</summary>
        /// <returns>The session data with IsGuest set, or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationLogInResult StartGuestSession();
    }
}