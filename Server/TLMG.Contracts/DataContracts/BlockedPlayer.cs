using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Identifies a player blocked by the requesting account, without exposing incoming blocks.
    /// </summary>
    [DataContract]
    public sealed class BlockedPlayer
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Nickname { get; set; }
    }
}
