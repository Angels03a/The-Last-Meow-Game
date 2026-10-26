using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Callback con el que el servidor responde al registro; lo implementa el cliente.
    /// </summary>
    [ServiceContract]
    public interface IUsersManagerCallback
    {
        /// <summary>Entrega el resultado del registro al cliente.</summary>
        /// <param name="operationRegisterPlayerAccountResult">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void RegisterPlayerAccountResult(OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult);

        /// <summary>Entrega el resultado del inicio de sesión al cliente.</summary>
        /// <param name="operationLogInResult">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void LogInResult(OperationLogInResult operationLogInResult);
    }
}