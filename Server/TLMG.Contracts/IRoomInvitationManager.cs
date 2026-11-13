using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Room invitations between online friends; answers and notices arrive through
    /// IRoomInvitationManagerCallback.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IRoomInvitationManagerCallback))]
    public interface IRoomInvitationManager
    {
        /// <summary>Invites an online friend to the room of the player; the answer is InviteFriendToRoomResult.</summary>
        /// <param name="sessionToken">Session token of the player inviting.</param>
        /// <param name="friendUserId">Account identifier of the invited friend.</param>
        [OperationContract(IsOneWay = true)]
        void InviteFriendToRoom(string sessionToken, int friendUserId);

        /// <summary>
        /// Accepts a received invitation and authorizes the player to enter the room without placing the player in
        /// it; the answer is AcceptRoomInvitationResult.
        /// </summary>
        /// <param name="sessionToken">Session token of the player accepting.</param>
        /// <param name="invitationId">Identifier of the received invitation.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptRoomInvitation(string sessionToken, int invitationId);
    }
}