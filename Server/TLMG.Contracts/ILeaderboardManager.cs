using System.ServiceModel;

namespace TLMG.Contracts
{
    [ServiceContract(CallbackContract = typeof(ILeaderboardManagerCallback))]
    public interface ILeaderboardManager
    {
        [OperationContract(IsOneWay = true)]
        void GetLeaderboard(string sessionToken);
    }
}
