using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Callback with which the server answers friendship operations and announces presence changes.</summary>
    [ServiceContract]
    public interface IFriendshipManagerCallback
    {
        /// <summary>Delivers the friend list and pending requests, or the error key.</summary>
        /// <param name="operationSubscribeToFriendsResult">Friends, pending requests or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToFriendsResult(OperationSubscribeToFriendsResult operationSubscribeToFriendsResult);

        /// <summary>Delivers the result of requesting friendship to the sender.</summary>
        /// <param name="operationSendFriendRequestResult">The request, the new friend or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void SendFriendRequestResult(OperationSendFriendRequestResult operationSendFriendRequestResult);

        /// <summary>Delivers the result of accepting a request to the player who accepted.</summary>
        /// <param name="operationAcceptFriendRequestResult">The new friend or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequestResult(OperationAcceptFriendRequestResult operationAcceptFriendRequestResult);

        /// <summary>Delivers the result of declining a request to the player who declined.</summary>
        /// <param name="operationDeclineFriendRequestResult">Success or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void DeclineFriendRequestResult(OperationDeclineFriendRequestResult operationDeclineFriendRequestResult);

        /// <summary>Announces that an accepted friend went online or offline.</summary>
        /// <param name="friend">The friend with the current presence.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendPresenceChanged(Friend friend);

        /// <summary>Announces to an online receiver that another player sent a friend request.</summary>
        /// <param name="friendRequest">The received request, with IsIncoming true.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendRequestReceived(FriendRequest friendRequest);

        /// <summary>Announces to the sender, if online, that the request was accepted.</summary>
        /// <param name="friend">The new friend with the current presence.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendAdded(Friend friend);
    }
}