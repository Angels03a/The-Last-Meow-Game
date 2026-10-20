using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para crear cuentas de jugador.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IAccountRegistrationServiceCallback))]
    public interface IAccountRegistrationService
    {
        /// <summary>Solicita crear una cuenta; la respuesta llega por RegisterPlayerAccountResult.</summary>
        /// <param name="playerAccountRegistration">Datos capturados por el jugador.</param>
        [OperationContract(IsOneWay = true)]
        void RegisterPlayerAccount(PlayerAccountRegistration playerAccountRegistration);
    }

    /// <summary>
    /// Callback con el que el servidor responde al registro; lo implementa el cliente.
    /// </summary>
    [ServiceContract]
    public interface IAccountRegistrationServiceCallback
    {
        /// <summary>Entrega el resultado del registro al cliente.</summary>
        /// <param name="operationRegisterPlayerAccountResult">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void RegisterPlayerAccountResult(OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult);
    }
}