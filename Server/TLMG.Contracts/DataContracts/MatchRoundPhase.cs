using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Round phase sent over the network. Services maps it from the engine phase.</summary>
    [DataContract]
    public enum MatchRoundPhase
    {
        /// <summary>Players take turns placing cards.</summary>
        [EnumMember]
        PlacingCards,

        /// <summary>Bidding is in progress.</summary>
        [EnumMember]
        Betting,

        /// <summary>One bidder remains and the challenge is ready.</summary>
        [EnumMember]
        ChallengeReady,

        /// <summary>A failed challenge has a pending punishment.</summary>
        [EnumMember]
        PunishmentPending,

        /// <summary>The match has ended.</summary>
        [EnumMember]
        MatchFinished
    }
}
