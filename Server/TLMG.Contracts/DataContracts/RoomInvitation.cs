using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Room invitation from a friend. The server reveals the room only after acceptance.
    /// </summary>
    [DataContract]
    public class RoomInvitation
    {
        /// <summary>Invitation ID used by the player to accept it.</summary>
        [DataMember]
        public int InvitationId { get; set; }

        /// <summary>Inviting friend's nickname.</summary>
        [DataMember]
        public string InviterNickname { get; set; }

        /// <summary>Inviting friend's user identifier.</summary>
        [DataMember]
        public int InviterUserId { get; set; }
    }
}
