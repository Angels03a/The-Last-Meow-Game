using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>References to the match the token owner can still access.</summary>
    [DataContract]
    public class OperationRecoveryContextResult : OperationResult
    {
        /// <summary>Authorized room, or null if there is no recoverable match.</summary>
        [DataMember]
        public int? RoomId { get; set; }

        /// <summary>Authorized match, or null if there is no recoverable match.</summary>
        [DataMember]
        public Guid? MatchId { get; set; }
    }
}
