using System;
using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager : ILeaderboardConnection
    {
        private LeaderboardManagerClient _leaderboardClient;
        private ServerCallback _leaderboardCallback;

        public event EventHandler<ServerEventArgs<OperationGetLeaderboardResult>> LeaderboardResultReceived;

        public bool RequestLeaderboard()
        {
            bool wasLeaderboardRequestSent = false;
            CloseLeaderboardChannel();

            try
            {
                _leaderboardCallback = new ServerCallback(this);
                _leaderboardClient = new LeaderboardManagerClient(new InstanceContext(_leaderboardCallback));
                _leaderboardClient.GetLeaderboard(_sessionToken);
                wasLeaderboardRequestSent = true;
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("Leaderboard query timed out.", timeoutException);
                CloseLeaderboardChannel();
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Leaderboard communication failed.", communicationException);
                CloseLeaderboardChannel();
            }

            return wasLeaderboardRequestSent;
        }

        public void CloseLeaderboardChannel()
        {
            CloseClientSafely(_leaderboardClient);
            _leaderboardClient = null;
            _leaderboardCallback = null;
        }

        internal void NotifyLeaderboardResultReceived(
            ServerCallback callback,
            OperationGetLeaderboardResult leaderboardResponse)
        {
            bool isCurrentCallback = ReferenceEquals(callback, _leaderboardCallback);

            if (isCurrentCallback && leaderboardResponse != null)
            {
                CloseLeaderboardChannel();
                LeaderboardResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationGetLeaderboardResult>(leaderboardResponse));
            }
        }
    }
}
