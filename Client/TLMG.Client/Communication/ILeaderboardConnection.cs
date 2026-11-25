using System;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Provides leaderboard access for LeaderboardViewModel (D-70). ServerConnectionManager implements this interface
    /// (D-60). Guests can query the leaderboard without a session. The result event runs on the UI thread and carries
    /// the server's ErrorKey. The screen translates the key.
    /// </summary>
    public interface ILeaderboardConnection
    {
        /// <summary>Raised when the server returns the players or an error key.</summary>
        event EventHandler<ServerEventArgs<OperationGetLeaderboardResult>> LeaderboardResultReceived;

        /// <summary>
        /// Requests the leaderboard. A new request replaces the previous one. LeaderboardResultReceived delivers the
        /// response.
        /// </summary>
        /// <returns>
        /// True if sent. If the channel cannot open or the send fails, returns false and logs the details.
        /// </returns>
        bool RequestLeaderboard();

        /// <summary>Closes the query channel if open. Safe to call without an active query.</summary>
        void CloseLeaderboardChannel();
    }
}
