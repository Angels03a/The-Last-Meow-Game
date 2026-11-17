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

        /// <summary>
        /// Validates registration data, checks availability and sends a 6-digit verification code by email.
        /// </summary>
        /// <param name="playerAccountRegistration">Data captured by the player.</param>
        /// <param name="cultureCode">Active client culture code (for example, "es-MX" or "en-US").</param>
        /// <returns>Success or the error key so the client can translate it.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult RequestAccountVerificationCode(
            PlayerAccountRegistration playerAccountRegistration,
            string cultureCode);

        /// <summary>
        /// Verifies the 6-digit code and persists the player account in the database upon confirmation.
        /// </summary>
        /// <param name="playerAccountRegistration">Data captured by the player.</param>
        /// <param name="verificationCode">Six-digit code entered by the player.</param>
        /// <returns>Success or the error key so the client can translate it.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult CompletePlayerAccountRegistration(
            PlayerAccountRegistration playerAccountRegistration,
            string verificationCode);

        /// <summary>
        /// Generates and sends a new 6-digit verification code to the specified email address.
        /// </summary>
        /// <param name="email">Destination email address.</param>
        /// <param name="cultureCode">Active client culture code.</param>
        /// <returns>Success or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult ResendAccountVerificationCode(string email, string cultureCode);

        /// <summary>Logs a player in and returns the result directly, including the session token.</summary>
        /// <param name="operationLogInRequest">Credentials captured by the player.</param>
        /// <returns>The session data or the error key.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest);

        /// <summary>Returns the profile of the session owner so the screen shows server data, not client state.</summary>
        /// <param name="sessionToken">Token issued at log in that identifies the player.</param>
        /// <returns>The profile data or the error key so the client can translate it.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationPlayerProfileResult GetPlayerProfile(string sessionToken);

        /// <summary>Updates nickname and avatar of the session owner in a single request-reply operation.</summary>
        /// <param name="updateProfileRequest">Session token and the new profile values.</param>
        /// <returns>Success or the error key so the client can translate it.</returns>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationResult UpdatePlayerProfile(OperationUpdatePlayerProfileRequest updateProfileRequest);
    }
}