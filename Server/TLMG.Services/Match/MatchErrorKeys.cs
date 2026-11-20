namespace TLMG.Services.Match
{
    /// <summary>
    /// Match error keys translated by the client. These keys live here to avoid changing Domain in this increment.
    /// Moving them to ErrorKeys.Match would keep their values.
    /// </summary>
    public static class MatchErrorKeys
    {
        /// <summary>The match is missing, belongs to someone else or no longer includes the player.</summary>
        public const string NotInMatch = "Match_NotInMatchError";

        /// <summary>The channel has no match subscription.</summary>
        public const string NotSubscribed = "Match_NotSubscribedError";

        /// <summary>A player is waiting to reconnect.</summary>
        public const string ReconnectionPending = "Match_ReconnectionPending";

        /// <summary>The pre-match minigame is still running.</summary>
        public const string FugitiveKittenInProgress = "Match_FugitiveKittenInProgressError";

        /// <summary>It is another player's turn.</summary>
        public const string NotYourTurn = "Match_NotYourTurnError";

        /// <summary>An open bid or ready challenge blocks card placement.</summary>
        public const string BetInProgress = "Match_BetInProgressError";

        /// <summary>The player's hand is empty.</summary>
        public const string HandEmpty = "Match_HandEmptyError";

        /// <summary>The hand has no card of that type.</summary>
        public const string CardNotInHand = "Match_CardNotInHandError";

        /// <summary>Pending punishment blocks actions.</summary>
        public const string PunishmentPending = "Match_PunishmentPendingError";

        /// <summary>The match has ended.</summary>
        public const string MatchFinished = "Match_FinishedError";

        /// <summary>The received card type does not exist.</summary>
        public const string CardNotValid = "Match_CardNotValidError";

        /// <summary>A bid must be open before anyone can raise or fold.</summary>
        public const string NoBetInProgress = "Match_NoBetInProgressError";

        /// <summary>There are no face-down cards to bid on.</summary>
        public const string TableEmpty = "Match_TableEmptyError";

        /// <summary>With an empty hand, the bid must be exactly 1.</summary>
        public const string EmptyHandBet = "Match_EmptyHandBetError";

        /// <summary>The bid is outside the allowed range or does not exceed the current bid.</summary>
        public const string BetValueNotValid = "Match_BetValueNotValidError";

        /// <summary>The player must place a card this round before opening a bid.</summary>
        public const string NoCardPlaced = "Match_NoCardPlacedError";

        /// <summary>There is no challenge ready for a reveal.</summary>
        public const string NoChallenge = "Match_NoChallengeError";

        /// <summary>Only the highest bidder can reveal cards.</summary>
        public const string NotTheBettor = "Match_NotTheBettorError";

        /// <summary>The chosen pile's owner is no longer in the match.</summary>
        public const string PileOwnerNotInMatch = "Match_PileOwnerNotInMatchError";

        /// <summary>The bidder must reveal their own pile first.</summary>
        public const string OwnPileFirst = "Match_OwnPileFirstError";

        /// <summary>The chosen pile has no hidden cards left.</summary>
        public const string PileEmpty = "Match_PileEmptyError";

        /// <summary>There is no pending punishment.</summary>
        public const string NoPunishmentPending = "Match_NoPunishmentPendingError";

        /// <summary>Only the designated player can choose the punishment card.</summary>
        public const string NotTheChooser = "Match_NotTheChooserError";

        /// <summary>The chosen slot or card position does not exist.</summary>
        public const string CardIndexNotValid = "Match_CardIndexNotValidError";
    }
}
