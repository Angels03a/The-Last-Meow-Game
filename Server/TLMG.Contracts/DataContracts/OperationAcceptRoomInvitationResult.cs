using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Result of accepting an invitation. Returns the room ID for EnterLobby or an error key.
    /// </summary>
    [DataContract]
    public class OperationAcceptRoomInvitationResult : OperationResult
    {
        /// <summary>ID of the room the player may now enter, or 0 on failure.</summary>
        [DataMember]
        public int RoomId { get; set; }
    }
}