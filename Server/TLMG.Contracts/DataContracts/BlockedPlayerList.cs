using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Delivers only blocks owned by the requester so other players' moderation choices remain private.
    /// </summary>
    [DataContract]
    public sealed class BlockedPlayerList : OperationResult
    {
        [DataMember]
        public List<BlockedPlayer> Players { get; set; } = new List<BlockedPlayer>();
    }
}
