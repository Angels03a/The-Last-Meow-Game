using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILeaderboardManager
    {
        public void GetLeaderboard()
        {
                ILeaderboardManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<ILeaderboardManagerCallback>();

                OperationGetLeaderboardResult operationGetLeaderboardResult = _leaderboardService.GetLeaderboard();

                SafeNotify(() => callback.GetLeaderboardResult(operationGetLeaderboardResult));
        }
    }
}