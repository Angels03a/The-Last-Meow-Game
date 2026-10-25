using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para crear cuentas de jugador.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IUsersManagerCallback))]
    public interface IUsersManager
    {
        /// <summary>Solicita crear una cuenta; la respuesta llega por RegisterPlayerAccountResult.</summary>
        /// <param name="playerAccountRegistration">Datos capturados por el jugador.</param>
        [OperationContract(IsOneWay = true)]
        void RegisterPlayerAccount(PlayerAccountRegistration playerAccountRegistration);
    }
}
