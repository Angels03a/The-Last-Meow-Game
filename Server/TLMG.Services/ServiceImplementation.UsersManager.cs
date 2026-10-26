using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Accounts;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IUsersManager
    {
        private const string UnexpectedErrorKey = "CreateAccount_DataModelError";
        private const string UnexpectedLogInErrorKey = "Login_DataModelError";

        private static readonly IPlayerRepository _playerRepository = new PlayerRepository();

        private static readonly IPasswordHasher _passwordHasher = new PasswordHasher();

        private static readonly PlayerAccountRegistrationService _playerAccountRegistrationService =
            new PlayerAccountRegistrationService(_playerRepository, _passwordHasher);

        private static readonly PlayerLogInService _playerLogInService =
            new PlayerLogInService(_playerRepository, _passwordHasher);

        /// <summary>Delega el registro al servicio de registro y responde por callback.</summary>
        /// <param name="registrationPlayerAccount">Datos capturados por el jugador.</param>
        public void RegisterPlayerAccount(PlayerAccountRegistration registrationPlayerAccount)
        {
            IUsersManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IUsersManagerCallback>();

            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult;

            try
            {
                operationRegisterPlayerAccountResult =
                    _playerAccountRegistrationService.RegisterPlayerAccount(registrationPlayerAccount);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al registrar una cuenta de jugador.", ex);
                operationRegisterPlayerAccountResult = new OperationRegisterPlayerAccountResult
                {
                    ErrorKey = UnexpectedErrorKey
                };
            }

            SafeNotify(() => callback.RegisterPlayerAccountResult(operationRegisterPlayerAccountResult));
        }

        /// <summary>Delega el inicio de sesión al servicio de inicio de sesión y responde por callback.</summary>
        /// <param name="operationLogInRequest">Credenciales capturadas por el jugador.</param>
        public void LogIn(OperationLogInRequest operationLogInRequest)
        {
            IUsersManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IUsersManagerCallback>();

            OperationLogInResult operationLogInResult;

            try
            {
                operationLogInResult = _playerLogInService.LogIn(operationLogInRequest);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al iniciar sesión.", ex);
                operationLogInResult = new OperationLogInResult
                {
                    ErrorKey = UnexpectedLogInErrorKey
                };
            }

            SafeNotify(() => callback.LogInResult(operationLogInResult));
        }
    }
}