using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Callback with which the server answers invitation operations and announces received invitations.</summary>
    [ServiceContract]
    public interface IRoomInvitationManagerCallback
    {
        /// <summary>Delivers the result of inviting a friend to the room.</summary>
        /// <param name="operationInviteFriendToRoomResult">Success or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void InviteFriendToRoomResult(OperationResult operationInviteFriendToRoomResult);

        /// <summary>Delivers the result of accepting an invitation.</summary>
        /// <param name="operationAcceptRoomInvitationResult">The room identifier or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptRoomInvitationResult(OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult);

        /// <summary>Announces that a friend invited the player to a room.</summary>
        /// <param name="roomInvitation">The received invitation.</param>
        [OperationContract(IsOneWay = true)]
        void OnRoomInvitationReceived(RoomInvitation roomInvitation);
    }
}