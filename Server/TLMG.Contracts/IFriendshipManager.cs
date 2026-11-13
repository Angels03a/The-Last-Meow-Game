using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Friend list with presence and friend requests; answers and notices arrive through
    /// IFriendshipManagerCallback.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IFriendshipManagerCallback))]
    public interface IFriendshipManager
    {
        /// <summary>
        /// Registers this channel as the friends channel of the session and sets the player online; the answer is
        /// SubscribeToFriendsResult.
        /// </summary>
        /// <param name="sessionToken">Session token of the player.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToFriends(string sessionToken);

        /// <summary>
        /// Requests friendship with the registered player owning the nickname; a crossed request is accepted by
        /// itself and a rejected one can be reactivated without waiting. Guests are rejected.
        /// </summary>
        /// <param name="sessionToken">Session token of the player sending the request.</param>
        /// <param name="friendNickname">Searched nickname, 3 to 12 characters and not an email.</param>
        [OperationContract(IsOneWay = true)]
        void SendFriendRequest(string sessionToken, string friendNickname);

        /// <summary>Accepts a received friend request; only the receiver of a pending request can accept it.</summary>
        /// <param name="sessionToken">Session token of the player accepting.</param>
        /// <param name="friendRequestId">Identifier of the received request.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequest(string sessionToken, int friendRequestId);

        /// <summary>Declines a received friend request without notifying the sender.</summary>
        /// <param name="sessionToken">Session token of the player declining.</param>
        /// <param name="friendRequestId">Identifier of the received request.</param>
        [OperationContract(IsOneWay = true)]
        void DeclineFriendRequest(string sessionToken, int friendRequestId);
    }
}