using System.ServiceModel;

using TLMG.Contracts.Dtos;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para crear cuentas de jugador.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IAccountRegistrationServiceCallback))]
    public interface IAccountRegistrationService
    {
        /// <summary>Solicita crear una cuenta; la respuesta llega por OnRegisterResult.</summary>
        /// <param name="registration">Datos capturados por el jugador.</param>
        [OperationContract(IsOneWay = true)]
        void Register(RegistrationData registration);
    }

    /// <summary>
    /// Callback con el que el servidor responde al registro; lo implementa el cliente.
    /// </summary>
    [ServiceContract]
    public interface IAccountRegistrationServiceCallback
    {
        /// <summary>Entrega el resultado del registro al cliente.</summary>
        /// <param name="result">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void OnRegisterResult(OperationResultData result);
    }
}