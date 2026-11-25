namespace TLMG.Domain.Engine
{
    /// <summary>Card placement result. Services maps it to an ErrorKey.</summary>
    public enum CardPlacementStatus
    {
        /// <summary>The card is on top of the player's pile and the next player takes the turn.</summary>
        Placed,

        /// <summary>The player is outside this match.</summary>
        PlayerNotInMatch,

        /// <summary>Bidding or a pending challenge blocks card placement.</summary>
        BetInProgress,

        /// <summary>It is another player's turn. Only the current player can act.</summary>
        NotPlayersTurn,

        /// <summary>The player has no cards in hand to place.</summary>
        HandEmpty,

        /// <summary>The hand has no card of that type.</summary>
        CardNotInHand,

        /// <summary>A pending punishment blocks game actions.</summary>
        PunishmentPending,

        /// <summary>The finished match accepts no game actions.</summary>
        MatchFinished
    }
}
