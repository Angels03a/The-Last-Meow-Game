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

        /// <summary>Solicita iniciar sesión; la respuesta llega por LogInResult.</summary>
        /// <param name="operationLogInRequest">Credenciales capturadas por el jugador.</param>
        [OperationContract(IsOneWay = true)]
        void LogIn(OperationLogInRequest operationLogInRequest);
    }
}
