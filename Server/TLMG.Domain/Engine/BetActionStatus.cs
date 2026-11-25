namespace TLMG.Domain.Engine
{
    /// <summary>Result of opening a bid, raising or folding. Services maps it to an ErrorKey.</summary>
    public enum BetActionStatus
    {
        /// <summary>The action was applied.</summary>
        Accepted,

        /// <summary>The player is outside this match.</summary>
        PlayerNotInMatch,

        /// <summary>It is another player's turn. Only the current player can act.</summary>
        NotPlayersTurn,

        /// <summary>A bid is already open or a challenge is ready, so bidding cannot open.</summary>
        BetInProgress,

        /// <summary>There is no bidding round in which to raise or fold.</summary>
        NoBetInProgress,

        /// <summary>There are no face-down cards on the table to bid on.</summary>
        TableEmpty,

        /// <summary>With an empty hand, the opening bid must be exactly 1.</summary>
        EmptyHandRequiresBetOfOne,

        /// <summary>The bid is outside the allowed range or does not exceed the current bid.</summary>
        BetValueNotValid,

        /// <summary>The player must place a card this round before opening a bid.</summary>
        NoCardPlacedInRound,

        /// <summary>A pending punishment blocks game actions.</summary>
        PunishmentPending,

        /// <summary>The finished match accepts no game actions.</summary>
        MatchFinished
    }
}
