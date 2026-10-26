using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para consultar la tabla de posiciones.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ILeaderboardManagerCallback))]
    public interface ILeaderboardManager
    {
        /// <summary>Solicita la tabla de posiciones; la respuesta llega por GetLeaderboardResult.</summary>
        [OperationContract(IsOneWay = true)]
        void GetLeaderboard();
    }
}