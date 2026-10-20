using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Managers;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IAccountRegistrationService
    {
        private static readonly PlayerAccountRegistrationManager _accountRegistrationManager =
            new PlayerAccountRegistrationManager(new PlayerRepository());

        /// <summary>Delega el registro al manager y responde por callback.</summary>
        /// <param name="registrationPlayerAccount">Datos capturados por el jugador.</param>
        public void RegisterPlayerAccount(PlayerAccountRegistration registrationPlayerAccount)
        {
            IAccountRegistrationServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<IAccountRegistrationServiceCallback>();

            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult = 
                _accountRegistrationManager.RegisterPlayerAccount(registrationPlayerAccount);

            SafeNotify(() => callback.RegisterPlayerAccountResult(operationRegisterPlayerAccountResult));
        }
    }
}