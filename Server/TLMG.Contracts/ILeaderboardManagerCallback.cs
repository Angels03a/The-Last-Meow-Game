using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Callback con el que el servidor entrega la tabla de posiciones; lo implementa el cliente.
    /// </summary>
    [ServiceContract]
    public interface ILeaderboardManagerCallback
    {
        /// <summary>Entrega la tabla de posiciones al cliente.</summary>
        /// <param name="operationGetLeaderboardResult">Resultado con los jugadores o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult);
    }
}