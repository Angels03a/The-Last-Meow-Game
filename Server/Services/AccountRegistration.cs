using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Managers;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IAccountRegistrationService
    {
        private const string UnexpectedErrorKey = "CreateAccount_DataModelError";

        private static readonly PlayerAccountRegistrationManager _accountRegistrationManager =
            new PlayerAccountRegistrationManager(new PlayerRepository());

        /// <summary>Delega el registro al manager y responde por callback.</summary>
        /// <param name="registrationPlayerAccount">Datos capturados por el jugador.</param>
        public void RegisterPlayerAccount(PlayerAccountRegistration registrationPlayerAccount)
        {
            IAccountRegistrationServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<IAccountRegistrationServiceCallback>();

            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult;

            try
            {
                operationRegisterPlayerAccountResult =
                    _accountRegistrationManager.RegisterPlayerAccount(registrationPlayerAccount);
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