using System;
using System.Collections.Generic;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Exposes friends state and notifications independently of the active screen.</summary>
    public interface IFriendsConnection
    {
        /// <summary>Raised when the server returns the friends list and pending requests.</summary>
        event EventHandler<ServerEventArgs<OperationSubscribeToFriendsResult>> FriendListReceived;

        /// <summary>Raised when a friend changes online presence.</summary>
        event EventHandler<ServerEventArgs<Friend>> FriendPresenceChanged;

        /// <summary>Raised when another player sends a friend request.</summary>
        event EventHandler<ServerEventArgs<FriendRequest>> FriendRequestReceived;

        /// <summary>Raised when an accepted request adds a friend.</summary>
        event EventHandler<ServerEventArgs<Friend>> FriendAdded;

        /// <summary>Raised when the server responds to a room invitation request.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> InviteFriendResultReceived;

        /// <summary>Raised when the server responds to a sent friend request.</summary>
        event EventHandler<ServerEventArgs<OperationSendFriendRequestResult>> SendFriendRequestResultReceived;

        /// <summary>Raised when the server responds to an accepted friend request.</summary>
        event EventHandler<ServerEventArgs<OperationAcceptFriendRequestResult>>
            AcceptFriendRequestResultReceived;

        /// <summary>Raised when the server responds to a declined friend request.</summary>
        event EventHandler<ServerEventArgs<OperationDeclineFriendRequestResult>>
            DeclineFriendRequestResultReceived;

        /// <summary>Gets whether a registered or guest session exists.</summary>
        bool HasSession { get; }

        /// <summary>Gets whether a registered player session exists.</summary>
        bool HasRegisteredSession { get; }

        /// <summary>Gets whether the player is currently in a room.</summary>
        bool HasCurrentRoom { get; }

        /// <summary>Gets the latest friends list, or null before it is received.</summary>
        IReadOnlyList<Friend> FriendList { get; }

        /// <summary>Gets the current sent and received pending requests, or null before they are received.</summary>
        IReadOnlyList<FriendRequest> PendingFriendRequests { get; }

        /// <summary>Requests the friends list for the current registered or guest session; the result arrives through FriendListReceived.</summary>
        bool RequestFriendList();

        /// <summary>Invites an online friend to the room using the current registered or guest session; the result arrives through InviteFriendResultReceived.</summary>
        bool InviteFriendToRoom(int friendUserId);

        /// <summary>Sends a friend request by nickname using a registered session; the result arrives through SendFriendRequestResultReceived.</summary>
        bool SendFriendRequest(string friendNickname);

        /// <summary>Accepts a received request using a registered or guest session; the result arrives through AcceptFriendRequestResultReceived.</summary>
        bool AcceptFriendRequest(int friendRequestId);

        /// <summary>Declines a received request using a registered or guest session; the result arrives through DeclineFriendRequestResultReceived.</summary>
        bool DeclineFriendRequest(int friendRequestId);
    }
}
