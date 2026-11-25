namespace TLMG.Domain.Engine
{
    /// <summary>Current phase of a match round.</summary>
    public enum RoundPhase
    {
        /// <summary>Players take turns placing cards. The current player may open a bid.</summary>
        PlacingCards,

        /// <summary>During bidding, nobody places cards. Each player raises or folds on their turn.</summary>
        Betting,

        /// <summary>One bidder remains and resolves the challenge one card at a time.</summary>
        ChallengeReady,

        /// <summary>The challenge failed. Pending punishment blocks game actions.</summary>
        PunishmentPending,

        /// <summary>A player reached the mode's target for successful challenges and won the match.</summary>
        MatchFinished
    }
}
