using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Current match rules, players in turn order and current player. This pure library validates and calculates
    /// (§4.2). Services decides what to publish and store. Turn order follows room entry and is fixed at creation.
    /// Callers must serialize each complete operation on a match, including reads.
    /// </summary>
    public sealed partial class MatchState
    {
        private const int NoBetValue = 0;
        private const int FirstRoundNumber = 1;
        private const int LastSurvivorCount = 1;

        private readonly List<MatchPlayer> _players = new List<MatchPlayer>();
        private int _playerInTurnIndex;
        private readonly List<MatchPlayer> _eliminatedPlayers = new List<MatchPlayer>();
        private bool _hasExtraLifeBeenGranted;
        private bool _hasAnyCardBeenPlaced;
        private int _successfulChallengeSequence;

        /// <summary>Starts a match with a full hand for each player.</summary>
        /// <param name="modeRules">Mode rules selected in the room.</param>
        /// <param name="playerIdsInJoinOrder">Player IDs in room entry order.</param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">There are fewer than 3 or more than 6 players.</exception>
        /// <exception cref="ArgumentException">A player ID appears more than once.</exception>
        public MatchState(MatchModeRules modeRules, IReadOnlyList<int> playerIdsInJoinOrder)
        {
            ValidateMatchSetup(modeRules, playerIdsInJoinOrder);

            foreach (int playerId in playerIdsInJoinOrder)
            {
                _players.Add(new MatchPlayer(playerId, modeRules));
            }

            ModeRules = modeRules;
            Players = _players.AsReadOnly();
            EliminatedPlayers = _eliminatedPlayers.AsReadOnly();
        }

        /// <summary>Rules for this match mode.</summary>
        public MatchModeRules ModeRules { get; }

        /// <summary>Players in fixed turn order.</summary>
        public IReadOnlyList<MatchPlayer> Players { get; }

        /// <summary>
        /// Player whose turn it is, starting with the first player. After the match ends, Phase is MatchFinished, every
        /// action is rejected and the turn has no meaning.
        /// </summary>
        public MatchPlayer PlayerInTurn => _players[_playerInTurnIndex];

        /// <summary>
        /// Current round number, starting at 1. Increases only for a new round, including a restart after an exit.
        /// </summary>
        public int RoundNumber { get; private set; } = FirstRoundNumber;

        /// <summary>Current round phase for placement, bidding or a ready challenge.</summary>
        public RoundPhase Phase { get; private set; }

        /// <summary>
        /// Current bid, or 0 without a bid. For a ready challenge, this is the number of cards to reveal.
        /// </summary>
        public int CurrentBet { get; private set; }

        /// <summary>Highest bidder, who takes the challenge when ready, or null.</summary>
        public MatchPlayer BetLeader { get; private set; }

        /// <summary>
        /// Latest challenge resolution, or null before the first challenge or after an exit restarts the round. A
        /// closed resolution keeps the last revealed card available. Phase determines permitted actions.
        /// </summary>
        public ChallengeResolution Resolution { get; private set; }

        /// <summary>Pending punishment from a failed challenge, or null if none.</summary>
        public PendingPunishment PendingPunishment { get; private set; }

        /// <summary>
        /// Winner by challenge count or by being the last player standing, or null while play continues.
        /// </summary>
        public MatchPlayer Winner { get; private set; }

        /// <summary>Face-down cards on the table, summed across player piles.</summary>
        public int CardsOnTable => _players.Sum(player => player.PileCards.Count);

        /// <summary>
        /// Moves a hand card to the top of the player's pile and passes the turn to the next active player. Lives stay
        /// the same. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player requesting placement.</param>
        /// <param name="card">Type of card to place.</param>
        /// <returns>Placed on success, otherwise the rejection reason.</returns>
        public CardPlacementStatus PlaceCard(int playerId, CardKind card)
        {
            CardPlacementStatus placementStatus;
            MatchPlayer player = FindPlayer(playerId);

            if (player == null)
            {
                placementStatus = CardPlacementStatus.PlayerNotInMatch;
            }
            else if (Phase == RoundPhase.PunishmentPending)
            {
                placementStatus = CardPlacementStatus.PunishmentPending;
            }
            else if (Phase == RoundPhase.MatchFinished)
            {
                placementStatus = CardPlacementStatus.MatchFinished;
            }
            else if (Phase != RoundPhase.PlacingCards)
            {
                placementStatus = CardPlacementStatus.BetInProgress;
            }
            else if (!ReferenceEquals(player, PlayerInTurn))
            {
                placementStatus = CardPlacementStatus.NotPlayersTurn;
            }
            else if (player.HandCards.Count == 0)
            {
                placementStatus = CardPlacementStatus.HandEmpty;
            }
            else if (!player.TryPlaceCard(card))
            {
                placementStatus = CardPlacementStatus.CardNotInHand;
            }
            else
            {
                _hasAnyCardBeenPlaced = true;
                MoveToNextActivePlayer();
                placementStatus = CardPlacementStatus.Placed;
            }

            return placementStatus;
        }

        /// <summary>
        /// Awards an extra life only in eight-card mode, once per match to one player before the first card is placed
        /// (2.0.4.1). It cannot stack or transfer. Services determines the recipient. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player receiving the extra life.</param>
        /// <returns>Granted on success, otherwise the rejection reason.</returns>
        public ExtraLifeStatus GrantExtraLife(int playerId)
        {
            ExtraLifeStatus extraLifeStatus = ExtraLifeStatus.Granted;
            MatchPlayer player = FindPlayer(playerId);

            if (player == null)
            {
                extraLifeStatus = ExtraLifeStatus.PlayerNotInMatch;
            }
            else if (!ModeRules.AllowsExtraLife)
            {
                extraLifeStatus = ExtraLifeStatus.ModeDoesNotAllowExtraLife;
            }
            else if (_hasExtraLifeBeenGranted)
            {
                extraLifeStatus = ExtraLifeStatus.ExtraLifeAlreadyGranted;
            }
            else if (_hasAnyCardBeenPlaced)
            {
                extraLifeStatus = ExtraLifeStatus.CardAlreadyPlaced;
            }
            else
            {
                player.HasExtraLife = true;
                _hasExtraLifeBeenGranted = true;
            }

            return extraLifeStatus;
        }

        /// <summary>
        /// Checks for non-null arguments, 3 to 6 players and unique IDs before changing state.
        /// </summary>
        private static void ValidateMatchSetup(MatchModeRules modeRules, IReadOnlyList<int> playerIdsInJoinOrder)
        {
            if (modeRules == null)
            {
                throw new ArgumentNullException(nameof(modeRules));
            }

            if (playerIdsInJoinOrder == null)
            {
                throw new ArgumentNullException(nameof(playerIdsInJoinOrder));
            }

            bool hasValidPlayerCount = playerIdsInJoinOrder.Count >= ValidationRules.MinimumPlayersPerMatchCount
                && playerIdsInJoinOrder.Count <= ValidationRules.MaximumPlayersPerMatchCount;

            if (!hasValidPlayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIdsInJoinOrder));
            }

            if (new HashSet<int>(playerIdsInJoinOrder).Count != playerIdsInJoinOrder.Count)
            {
                throw new ArgumentException("Un jugador se repite.", nameof(playerIdsInJoinOrder));
            }
        }

        /// <summary>The engine uses only UTC time supplied by the server and never reads the clock itself.</summary>
        private static void ValidateUtc(DateTime nowUtc)
        {
            PlayerExit.ValidateUtc(nowUtc, nameof(nowUtc));
        }

        /// <summary>
        /// Starts a new round, increments RoundNumber, prepares the table and gives the turn to the first active player
        /// in fixed order. Only this method counts rounds.
        /// </summary>
        private void StartNextRound()
        {
            RoundNumber++;
            ResetBetting();
            Phase = RoundPhase.PlacingCards;
            _playerInTurnIndex = _players.FindIndex(player => player.IsInPlay);
        }

        /// <summary>Resets bidding to no bid, no leader and no folded players.</summary>
        private void ResetBetting()
        {
            ClearWithdrawals();
            CurrentBet = NoBetValue;
            BetLeader = null;
        }

        private bool HasSinglePlayerInPlay() => _players.Count(player => player.IsInPlay) == LastSurvivorCount;

        /// <summary>Ends the match with the sole active player as winner (No. 9b).</summary>
        private void FinishWithLastPlayerInPlay()
        {
            Winner = _players.First(player => player.IsInPlay);
            Phase = RoundPhase.MatchFinished;
        }

        private void ClearWithdrawals()
        {
            foreach (MatchPlayer player in _players)
            {
                player.HasWithdrawnFromBet = false;
            }
        }

        /// <summary>Passes the turn to the next active player. The player placing a card is always active.</summary>
        private void MoveToNextActivePlayer()
        {
            do
            {
                _playerInTurnIndex = (_playerInTurnIndex + 1) % _players.Count;
            }
            while (!PlayerInTurn.IsInPlay);
        }

        /// <summary>Finds a match player. A player who left cannot take any game action.</summary>
        private MatchPlayer FindPlayer(int playerId)
        {
            MatchPlayer participant = FindParticipant(playerId);
            MatchPlayer player = null;

            if (participant != null && !participant.HasLeftMatch)
            {
                player = participant;
            }

            return player;
        }
    }
}
