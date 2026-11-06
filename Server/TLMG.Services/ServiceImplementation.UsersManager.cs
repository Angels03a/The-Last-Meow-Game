using System.ServiceModel;

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

        /// <summary>Delega el registro al servicio de registro y responde por callback.</summary>
        /// <param name="registrationPlayerAccount">Datos capturados por el jugador.</param>
        public void RegisterPlayerAccount(PlayerAccountRegistration registrationPlayerAccount)
        {
            IUsersManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IUsersManagerCallback>();

            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult =
                _playerAccountRegistrationService.RegisterPlayerAccount(registrationPlayerAccount);

            SafeNotify(() => callback.RegisterPlayerAccountResult(operationRegisterPlayerAccountResult));
        }

        /// <summary>Delega el inicio de sesión al servicio de inicio de sesión y responde por callback.</summary>
        /// <param name="operationLogInRequest">Credenciales capturadas por el jugador.</param>
        public void LogIn(OperationLogInRequest operationLogInRequest)
        {
            IUsersManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IUsersManagerCallback>();

            ResultWithNotification<OperationLogInResult> sessionOutcome = _playerLogInService.LogIn(operationLogInRequest);

            SafeNotify(() => callback.LogInResult(sessionOutcome.OperationResult));
            SafeNotifyAll(sessionOutcome.Notifications);
        }
    }
}