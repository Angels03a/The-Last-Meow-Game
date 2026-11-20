using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contract for leaderboard queries.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ILeaderboardManagerCallback))]
    public interface ILeaderboardManager
    {
        /// <summary>Requests the leaderboard. GetLeaderboardResult delivers the response.</summary>
        [OperationContract(IsOneWay = true)]
        void GetLeaderboard();
    }
}