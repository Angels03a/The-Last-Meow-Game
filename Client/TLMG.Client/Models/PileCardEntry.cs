namespace TLMG.Client.Models
{
    /// <summary>Miniature pile card with only its visible symbol.</summary>
    public sealed class PileCardEntry
    {
        /// <summary>Card symbol. Another player's pile shows only card backs.</summary>
        public string Symbol { get; set; }
    }
}
