using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.Dtos;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Managers;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IAccountRegistrationService
    {
        private static readonly AccountRegistrationManager _accountRegistrationManager =
            new AccountRegistrationManager(new PlayerRepository());

        /// <summary>Delega el registro al manager y responde por callback.</summary>
        /// <param name="registration">Datos capturados por el jugador.</param>
        public void Register(RegistrationData registration)
        {
            IAccountRegistrationServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<IAccountRegistrationServiceCallback>();

            OperationResultData result = _accountRegistrationManager.Register(registration);

            SafeNotify(() => callback.OnRegisterResult(result));
        }
    }
}