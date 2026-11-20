using System.Collections.Generic;

namespace TLMG.Client.Models
{
    /// <summary>
    /// Board built from a private server view. It includes the round, phase, players by seat, and the local hand and
    /// pile. Each view creates a complete board that stays unchanged.
    /// </summary>
    public sealed class MatchBoard
    {
        /// <summary>Whether a server view has arrived. Until then, the board is empty.</summary>
        public bool HasMatchView { get; set; }

        /// <summary>Current round.</summary>
        public int RoundNumber { get; set; }

        /// <summary>Resource key for the current phase name.</summary>
        public string PhaseLabelKey { get; set; }

        /// <summary>Whether it is the local player's turn.</summary>
        public bool IsLocalPlayerTurn { get; set; }

        /// <summary>
        /// Whether the local player can request to place a card. The server decides whether to accept it.
        /// </summary>
        public bool CanPlaceCards { get; set; }

        /// <summary>Bid confirmed by the server.</summary>
        public int CurrentBet { get; set; }

        /// <summary>Lowest next bid published by the server.</summary>
        public int MinimumBetValue { get; set; }

        /// <summary>Highest next bid published by the server.</summary>
        public int MaximumBetValue { get; set; }

        /// <summary>Whether the server allows a bid.</summary>
        public bool CanPlaceBet { get; set; }

        /// <summary>Whether the server allows the player to fold.</summary>
        public bool CanWithdrawFromBet { get; set; }

        /// <summary>Name of the leading bidder, if any.</summary>
        public string BetLeaderName { get; set; }

        /// <summary>Whether the server allows a reveal request.</summary>
        public bool CanRevealCard { get; set; }

        /// <summary>Whether public challenge details are available.</summary>
        public bool HasChallenge { get; set; }

        /// <summary>Cards revealed during the challenge.</summary>
        public int RevealedCount { get; set; }

        /// <summary>Number of cards the bidder promised to reveal.</summary>
        public int PromisedCount { get; set; }

        /// <summary>Challenge result received from the server.</summary>
        public TLMG.Client.UsersManagerReference.MatchChallengeOutcome ChallengeOutcome { get; set; }

        /// <summary>Whether a punishment is pending.</summary>
        public bool HasPunishment { get; set; }

        /// <summary>Permission from the server to choose a card.</summary>
        public bool CanChoosePunishment { get; set; }

        /// <summary>Whether the player must choose blindly.</summary>
        public bool IsBlindPunishment { get; set; }

        /// <summary>Name of the player making the choice.</summary>
        public string PunishmentChooserName { get; set; }

        /// <summary>Name of the player receiving the punishment.</summary>
        public string PunishedPlayerName { get; set; }

        /// <summary>Positions available to the authorized player.</summary>
        public IReadOnlyList<HandCardEntry> PunishmentCards { get; set; }

        /// <summary>All players in their fixed turn order.</summary>
        public IReadOnlyList<MatchPlayerEntry> Players { get; set; }

        /// <summary>Local player, or null before the view arrives.</summary>
        public MatchPlayerEntry LocalPlayer { get; set; }

        /// <summary>Opponent on the left, or null if the seat is empty.</summary>
        public MatchPlayerEntry OpponentLeft { get; set; }

        /// <summary>Opponent at the top left, or null if the seat is empty.</summary>
        public MatchPlayerEntry OpponentTopLeft { get; set; }

        /// <summary>Opponent at the top, or null if the seat is empty.</summary>
        public MatchPlayerEntry OpponentTop { get; set; }

        /// <summary>Opponent at the top right, or null if the seat is empty.</summary>
        public MatchPlayerEntry OpponentTopRight { get; set; }

        /// <summary>Opponent on the right, or null if the seat is empty.</summary>
        public MatchPlayerEntry OpponentRight { get; set; }

        /// <summary>Local player's hand.</summary>
        public IReadOnlyList<HandCardEntry> HandCards { get; set; }

        /// <summary>Local player's pile, from the oldest card to the top card.</summary>
        public IReadOnlyList<PileCardEntry> OwnPileCards { get; set; }
    }
}
