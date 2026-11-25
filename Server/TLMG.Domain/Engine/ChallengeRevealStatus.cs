namespace TLMG.Domain.Engine
{
    /// <summary>Challenge reveal result. Services maps it to an ErrorKey.</summary>
    public enum ChallengeRevealStatus
    {
        /// <summary>A card was revealed and the challenge continues.</summary>
        Revealed,

        /// <summary>The revealed card met the promised count and the challenge succeeded.</summary>
        ChallengeSucceeded,

        /// <summary>A Fatal Cat was revealed without an extra life to absorb it. The challenge failed.</summary>
        ChallengeFailed,

        /// <summary>The player requesting the reveal is outside the match.</summary>
        PlayerNotInMatch,

        /// <summary>There is no active challenge in which to reveal a card.</summary>
        NoChallengeInProgress,

        /// <summary>Only the highest bidder may reveal cards.</summary>
        NotTheBettor,

        /// <summary>The chosen pile's owner is outside the match.</summary>
        PileOwnerNotInMatch,

        /// <summary>The bidder must reveal their own pile first.</summary>
        OwnPileFirst,

        /// <summary>The chosen pile has no hidden cards left.</summary>
        PileFullyRevealed
    }
}
