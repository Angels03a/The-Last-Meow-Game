using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILeaderboardManager
    {
        public void GetLeaderboard(string sessionToken)
        {
            ILeaderboardManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<ILeaderboardManagerCallback>();
            OperationGetLeaderboardResult leaderboardResponse = _leaderboardService.GetLeaderboard(sessionToken);

            void NotifyLeaderboard()
            {
                callback.GetLeaderboardResult(leaderboardResponse);
            }

            SafeNotify(NotifyLeaderboard);
        }
    }
}
