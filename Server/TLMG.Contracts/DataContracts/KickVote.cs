using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Identifies the room and target of a vote; the server determines the voter.</summary>
    [DataContract]
    public sealed class KickVote
    {
        [DataMember]
        public int RoomId { get; set; }

        [DataMember]
        public int TargetUserId { get; set; }
    }
}
