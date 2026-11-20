using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Private match view containing public state and only the recipient's hand and pile. The client discards views
    /// with the same or an older version than the displayed view.
    /// </summary>
    [DataContract]
    public class MatchView
    {
        /// <summary>Match this view belongs to.</summary>
        [DataMember]
        public Guid MatchId { get; set; }

        /// <summary>Match version, increased after each accepted action.</summary>
        [DataMember]
        public int Version { get; set; }

        /// <summary>Server time when this view was built.</summary>
        [DataMember]
        public DateTime ServerTimeUtc { get; set; }

        /// <summary>UTC deadlines for disconnected players who are holding up the match.</summary>
        [DataMember]
        public Dictionary<int, DateTime> ReconnectDeadlinesUtc { get; set; }

        /// <summary>The minigame waits for the first connections before starting its clock.</summary>
        [DataMember]
        public bool IsWaitingForInitialLinks { get; set; }

        /// <summary>Public minigame, or null in four- and six-card modes.</summary>
        [DataMember]
        public FugitiveKittenView FugitiveKitten { get; set; }

        /// <summary>Current round.</summary>
        [DataMember]
        public int RoundNumber { get; set; }

        /// <summary>Current round phase.</summary>
        [DataMember]
        public MatchRoundPhase Phase { get; set; }

        /// <summary>Account of the player whose turn it is.</summary>
        [DataMember]
        public int PlayerInTurnUserId { get; set; }

        /// <summary>Players in fixed turn order, with card counts only.</summary>
        [DataMember]
        public List<MatchPlayerView> Players { get; set; }

        /// <summary>Recipient's hand, sent only to that player.</summary>
        [DataMember]
        public List<MatchCardKind> OwnHandCards { get; set; }

        /// <summary>
        /// Recipient's pile from oldest to top card, sent only to that player.
        /// </summary>
        [DataMember]
        public List<MatchCardKind> OwnPileCards { get; set; }

        /// <summary>Current bid in cards to reveal, or 0 without a bid.</summary>
        [DataMember]
        public int CurrentBet { get; set; }

        /// <summary>Highest bidder's account, or 0 without a bid.</summary>
        [DataMember]
        public int BetLeaderUserId { get; set; }

        /// <summary>Successful challenges needed to win.</summary>
        [DataMember]
        public int ChallengesToWin { get; set; }

        /// <summary>Winner's account, or 0 while the match continues.</summary>
        [DataMember]
        public int WinnerUserId { get; set; }

        /// <summary>Public final standings, or null if unavailable.</summary>
        [DataMember]
        public List<MatchStandingView> FinalStandings { get; set; }

        /// <summary>Total face-down cards across all piles.</summary>
        [DataMember]
        public int CardsOnTable { get; set; }

        /// <summary>Latest public challenge outcome, or null if none is active.</summary>
        [DataMember]
        public MatchChallengeView Challenge { get; set; }

        /// <summary>Public pending punishment, or null if none.</summary>
        [DataMember]
        public MatchPunishmentView Punishment { get; set; }

        /// <summary>Whether the recipient can place a card now.</summary>
        [DataMember]
        public bool CanPlaceCard { get; set; }

        /// <summary>Whether the recipient can open or raise a bid now.</summary>
        [DataMember]
        public bool CanPlaceBet { get; set; }

        /// <summary>Lowest bid the recipient can make. Applies only when CanPlaceBet is true.</summary>
        [DataMember]
        public int MinimumBetValue { get; set; }

        /// <summary>Highest bid the recipient can make. Applies only when CanPlaceBet is true.</summary>
        [DataMember]
        public int MaximumBetValue { get; set; }

        /// <summary>Whether the recipient can fold now.</summary>
        [DataMember]
        public bool CanWithdrawFromBet { get; set; }

        /// <summary>Whether the recipient can choose a pile to reveal now.</summary>
        [DataMember]
        public bool CanRevealCard { get; set; }

        /// <summary>Whether the recipient must choose the lost card now.</summary>
        [DataMember]
        public bool CanChoosePunishmentCard { get; set; }
    }
}
