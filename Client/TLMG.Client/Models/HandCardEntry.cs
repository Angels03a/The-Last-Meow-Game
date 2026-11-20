namespace TLMG.Client.Models
{
    /// <summary>Local player's hand card as it appears on the match screen.</summary>
    public sealed class HandCardEntry
    {
        /// <summary>Position sent to the server during punishment.</summary>
        public int Index { get; set; }

        /// <summary>Whether the position hides the card's identity.</summary>
        public bool IsBlind { get; set; }

        /// <summary>Card type that selects its image, text and colors.</summary>
        public HandCardKind Kind { get; set; }
    }
}