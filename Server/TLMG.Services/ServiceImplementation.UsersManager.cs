using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IUsersManager
    {
        private static readonly IPlayerRepository _playerRepository = new PlayerRepository();

        private static readonly IPasswordHasher _passwordHasher = new PasswordHasher();

        private static readonly PlayerAccountRegistrationService _playerAccountRegistrationService =
            new PlayerAccountRegistrationService(_playerRepository, _passwordHasher);

        private static readonly PlayerLogInService _playerLogInService;

        /// <summary>
        /// Delegates the registration to the registration service and returns the result directly.
        /// </summary>
        /// <param name="registrationPlayerAccount">Data captured by the player.</param>
        /// <returns>The result of the registration operation.</returns>
        public OperationRegisterPlayerAccountResult RegisterPlayerAccount(PlayerAccountRegistration registrationPlayerAccount)
        {
            return _playerAccountRegistrationService.RegisterPlayerAccount(registrationPlayerAccount);
        }

        /// <summary>
        /// Delegates the login to the login service, notifies other players, and returns the result directly.
        /// </summary>
        /// <param name="operationLogInRequest">Credentials captured by the player.</param>
        /// <returns>The result of the login operation.</returns>
        public OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest)
        {
            ResultWithNotification<OperationLogInResult> loginResult = _playerLogInService.LogIn(operationLogInRequest);

            SafeNotifyAll(loginResult.Notifications);

            return loginResult.OperationResult;
        }
    }
}