using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    [DataContract]
    public class RoomInvitation
    {
        [DataMember]
        public int InvitationId { get; set; }

        [DataMember]
        public string InviterNickname { get; set; }

        [DataMember]
        public int InviterUserId { get; set; }
    }
}