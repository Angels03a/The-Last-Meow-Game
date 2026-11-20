using System;
using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Queries the leaderboard through a separate, short-lived channel. A query failure reaches the screen without
    /// disconnecting the player. Each query creates a new ServerCallback without a session and replaces the previous
    /// receiver, so earlier results are ignored.
    /// </summary>
    internal sealed partial class ServerConnectionManager : ILeaderboardConnection
    {
        private LeaderboardManagerClient _leaderboardClient;
        private ServerCallback _leaderboardCallback;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<OperationGetLeaderboardResult>> LeaderboardResultReceived;

        /// <inheritdoc />
        /// <remarks>
        /// Opening the channel blocks the UI thread until the server responds or the binding times out (BL-079).
        /// </remarks>
        public bool RequestLeaderboard()
        {
            bool wasLeaderboardRequestSent = false;
            CloseLeaderboardChannel();

            try
            {
                _leaderboardCallback = new ServerCallback(this);
                _leaderboardClient = new LeaderboardManagerClient(new InstanceContext(_leaderboardCallback));
                _leaderboardClient.GetLeaderboard();
                wasLeaderboardRequestSent = true;
            }
            catch (TimeoutException ex)
            {
                _log.Error("Se agotó el tiempo al consultar la tabla de posiciones.", ex);
                CloseLeaderboardChannel();
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al consultar la tabla de posiciones.", ex);
                CloseLeaderboardChannel();
            }

            return wasLeaderboardRequestSent;
        }

        /// <inheritdoc />
        public void CloseLeaderboardChannel()
        {
            CloseClientSafely(_leaderboardClient);
            _leaderboardClient = null;
            _leaderboardCallback = null;
        }

        /// <summary>
        /// Closes the query channel and raises LeaderboardResultReceived only for the current query. Ignores older
        /// results.
        /// </summary>
        /// <param name="callback">Receiver that delivered the result.</param>
        /// <param name="operationGetLeaderboardResult">Result received from the server.</param>
        internal void NotifyLeaderboardResultReceived(
            ServerCallback callback,
            OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            bool isCurrentCallback = ReferenceEquals(callback, _leaderboardCallback);

            if (isCurrentCallback && operationGetLeaderboardResult != null)
            {
                CloseLeaderboardChannel();
                LeaderboardResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationGetLeaderboardResult>(operationGetLeaderboardResult));
            }
        }
    }
}
