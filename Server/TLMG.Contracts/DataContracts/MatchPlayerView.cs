using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Public player state exposes card counts and keeps hidden card types private.
    /// </summary>
    [DataContract]
    public class MatchPlayerView
    {
        /// <summary>Player account.</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nickname at match start.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Cards remaining in hand.</summary>
        [DataMember]
        public int HandCardCount { get; set; }

        /// <summary>Face-down cards in the player's pile.</summary>
        [DataMember]
        public int PileCardCount { get; set; }

        /// <summary>Whether the player has been eliminated.</summary>
        [DataMember]
        public bool IsEliminated { get; set; }

        /// <summary>Player's successful challenges. The count never resets during the match.</summary>
        [DataMember]
        public int SuccessfulChallengeCount { get; set; }

        /// <summary>Whether the player folded in the current bidding round.</summary>
        [DataMember]
        public bool HasWithdrawnFromBet { get; set; }

        /// <summary>Whether the player left the match entirely.</summary>
        [DataMember]
        public bool HasLeftMatch { get; set; }

        /// <summary>
        /// Pile cards revealed in the current challenge, from top to bottom. Visible to everyone and empty outside a
        /// challenge.
        /// </summary>
        [DataMember]
        public List<MatchCardKind> RevealedPileCards { get; set; }
    }
}
