using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Maintains friends channels and online presence.</summary>
    public interface IFriendsChannelRegistry : ISessionLookup
    {
        /// <summary>Registers the current friends channel and identifies recipients for online presence notifications.</summary>
        /// <param name="sessionToken">Client session token, which may be null or unknown.</param>
        /// <param name="channelId">Caller's friends channel identifier.</param>
        /// <param name="friendsCallback">Caller's friends channel callback.</param>
        /// <param name="friendUserIds">Accepted friend identifiers, including temporary friendships.</param>
        /// <returns>Applied with online friends, or InvalidSession. Always present.</returns>
        FriendsSubscription SubscribeFriendsChannel(string sessionToken, string channelId,
            IFriendsManagerCallback friendsCallback, IReadOnlyList<int> friendUserIds);

        /// <summary>Updates both online friends channels under the session lock without invoking callbacks.</summary>
        /// <param name="firstUserId">First player identifier.</param>
        /// <param name="secondUserId">Second player identifier.</param>
        /// <returns>Presence and callbacks for both players; offline callbacks are null.</returns>
        FriendsAddition AddFriendshipToOnlineChannels(int firstUserId, int secondUserId);

        /// <summary>Finds a friends channel callback for notification outside the lock.</summary>
        /// <param name="userId">Player identifier.</param>
        /// <returns>The callback, or null if the player is offline.</returns>
        IFriendsManagerCallback FindFriendsCallbackOfUser(int userId);

        /// <summary>Ends the session only if the closed or failed friends channel is still current.</summary>
        /// <param name="channelId">Closed or failed friends channel.</param>
        /// <param name="sessionEnd">Notifications for the ended session, or null if none was ended.</param>
        /// <returns>True if a session was ended.</returns>
        bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd);
    }
}
