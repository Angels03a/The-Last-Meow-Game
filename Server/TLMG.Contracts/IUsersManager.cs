using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Request-reply contract for account operations that need an immediate answer because the player cannot
    /// continue without it.
    /// </summary>
    [ServiceContract]
    public interface IUsersManager
    {
        /// <summary>Creates a player account and returns the result directly.</summary>
        /// <param name="playerAccountRegistration">Data captured by the player.</param>
        /// <returns>Success or the error key so the client can translate it.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult RegisterPlayerAccount(PlayerAccountRegistration playerAccountRegistration);

        /// <summary>Logs a player in and returns the result directly, including the session token.</summary>
        /// <param name="operationLogInRequest">Credentials captured by the player.</param>
        /// <returns>The session data or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest);
    }
}