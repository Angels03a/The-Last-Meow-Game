using System.ServiceModel;
using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para gestionar la cuenta de jugador (Transaccional / Request-Reply).
    /// </summary>
    [ServiceContract]
    public interface IUsersManager
    {
        /// <summary>Solicita crear una cuenta devolviendo el resultado directamente.</summary>
        /// <param name="playerAccountRegistration">Datos capturados por el jugador.</param>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationRegisterPlayerAccountResult RegisterPlayerAccount(PlayerAccountRegistration playerAccountRegistration);

        /// <summary>Solicita iniciar sesión devolviendo el resultado directamente.</summary>
        /// <param name="operationLogInRequest">Credenciales capturadas por el jugador.</param>
        [OperationContract]
        [FaultContract(typeof(SystemFault))]
        OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest); 
    }
}