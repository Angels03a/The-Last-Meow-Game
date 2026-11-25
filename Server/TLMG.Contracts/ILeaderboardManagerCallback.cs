using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Client callback used by the server to deliver the leaderboard.
    /// </summary>
    [ServiceContract]
    public interface ILeaderboardManagerCallback
    {
        /// <summary>Delivers the leaderboard to the client.</summary>
        /// <param name="operationGetLeaderboardResult">Players returned by the server, or an error key.</param>
        [OperationContract(IsOneWay = true)]
        void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult);
    }
}