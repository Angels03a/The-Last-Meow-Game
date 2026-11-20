using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Callback de amigos de prueba que guarda los avisos de solicitudes que recibió.</summary>
    internal sealed class FriendsCallbackStub : IFriendsManagerCallback
    {
        internal List<FriendRequest> ReceivedRequests { get; } = new List<FriendRequest>();

        internal List<Friend> AddedFriends { get; } = new List<Friend>();

        internal List<Friend> PresenceChanges { get; } = new List<Friend>();

        internal int OtherCallCount { get; private set; }

        public void SubscribeToFriendsResult(OperationSubscribeToFriendsResult operationSubscribeToFriendsResult)
        {
            OtherCallCount++;
        }

        public void InviteFriendToRoomResult(OperationResult operationInviteFriendToRoomResult)
        {
            OtherCallCount++;
        }

        public void AcceptRoomInvitationResult(OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            OtherCallCount++;
        }

        public void OnFriendPresenceChanged(Friend friend)
        {
            PresenceChanges.Add(friend);
            OtherCallCount++;
        }

        public void OnRoomInvitationReceived(RoomInvitation roomInvitation)
        {
            OtherCallCount++;
        }

        public void SendFriendRequestResult(OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            OtherCallCount++;
        }

        public void AcceptFriendRequestResult(OperationAcceptFriendRequestResult operationAcceptFriendRequestResult)
        {
            OtherCallCount++;
        }

        public void DeclineFriendRequestResult(OperationDeclineFriendRequestResult operationDeclineFriendRequestResult)
        {
            OtherCallCount++;
        }

        public void OnFriendRequestReceived(FriendRequest friendRequest)
        {
            ReceivedRequests.Add(friendRequest);
        }

        public void OnFriendAdded(Friend friend)
        {
            AddedFriends.Add(friend);
        }
        public void FriendsHeartbeatEcho(int sequence, string errorKey) { }
    }
}
