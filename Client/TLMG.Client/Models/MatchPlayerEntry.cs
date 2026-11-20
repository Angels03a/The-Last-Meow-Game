using System.Collections.Generic;

namespace TLMG.Client.Models
{
    /// <summary>
    /// Player as shown on the match screen, with nickname, counts and status. Only the server knows the types of an
    /// opponent's hidden cards.
    /// </summary>
    public sealed class MatchPlayerEntry
    {
        /// <summary>Player account.</summary>
        public int UserId { get; set; }

        /// <summary>Nickname at match start.</summary>
        public string Nickname { get; set; }

        /// <summary>Cards remaining in hand.</summary>
        public int HandCardCount { get; set; }

        /// <summary>Face-down cards in the player's pile.</summary>
        public int PileCardCount { get; set; }

        /// <summary>Cards still hidden in the pile.</summary>
        public int HiddenPileCardCount { get; set; }

        /// <summary>Player's lives, counted as cards in hand plus cards in the pile.</summary>
        public int LivesCount { get; set; }

        /// <summary>Starting lives, equal to cards per player in this mode.</summary>
        public int MaximumLivesCount { get; set; }

        /// <summary>Whether the player has been eliminated.</summary>
        public bool IsEliminated { get; set; }

        /// <summary>Whether it is this player's turn.</summary>
        public bool IsInTurn { get; set; }

        /// <summary>Miniature pile cards, one per card placed.</summary>
        public IReadOnlyList<PileCardEntry> PileCards { get; set; }
    }
}
