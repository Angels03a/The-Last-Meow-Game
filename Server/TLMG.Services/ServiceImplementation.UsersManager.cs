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

        private static readonly PlayerAccountRegistrationService _playerAccountRegistrationService =
            new PlayerAccountRegistrationService(new PlayerRepository());

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
                _log.Fatal("Fallo inesperado al registrar una cuenta de jugador.", ex);
                operationRegisterPlayerAccountResult = new OperationRegisterPlayerAccountResult
                {
                    ErrorKey = UnexpectedErrorKey
                };
            }

            SafeNotify(() => callback.RegisterPlayerAccountResult(operationRegisterPlayerAccountResult));
        }
    }
}