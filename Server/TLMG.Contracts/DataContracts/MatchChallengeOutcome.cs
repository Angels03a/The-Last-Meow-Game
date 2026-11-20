using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Challenge result decided by the server and displayed by the client.</summary>
    [DataContract]
    public enum MatchChallengeOutcome
    {
        /// <summary>The bidder is still revealing cards.</summary>
        [EnumMember]
        InProgress,

        /// <summary>The bidder revealed the promised number of cards without revealing a Fatal Cat.</summary>
        [EnumMember]
        Succeeded,

        /// <summary>A Fatal Cat was revealed and the challenge failed.</summary>
        [EnumMember]
        Failed
    }
}
