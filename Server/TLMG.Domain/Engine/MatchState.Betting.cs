using System;
using System.Linq;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int MinimumBetValue = 1;
        private const int EmptyHandBetValue = 1;

        /// <summary>
        /// Opens a bid on the player's turn instead of placing a card. Bid from 1 to the smaller of the table count and
        /// the mode's card count. With an empty hand, bid exactly 1. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player opening the bid.</param>
        /// <param name="betValue">Number of cards the player claims they can reveal.</param>
        /// <returns>Accepted if bidding opened, otherwise the rejection reason.</returns>
        public BetActionStatus OpenBet(int playerId, int betValue)
        {
            BetActionStatus betStatus = ValidateBetActor(
                playerId,
                RoundPhase.PlacingCards,
                BetActionStatus.BetInProgress);

            if (betStatus == BetActionStatus.Accepted)
            {
                betStatus = ValidateOpeningBetValue(betValue);
            }

            if (betStatus == BetActionStatus.Accepted)
            {
                StartBetting(betValue);
            }

            return betStatus;
        }

        /// <summary>
        /// Raises on the player's turn. The bid must exceed the current bid and cannot exceed the smaller of the table
        /// count and the mode's card count. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player raising the bid.</param>
        /// <param name="betValue">New bid in cards to reveal.</param>
        /// <returns>Accepted if the bid was raised, otherwise the rejection reason.</returns>
        public BetActionStatus RaiseBet(int playerId, int betValue)
        {
            BetActionStatus betStatus = ValidateBetActor(playerId, RoundPhase.Betting, BetActionStatus.NoBetInProgress);

            if (betStatus == BetActionStatus.Accepted)
            {
                if (betValue <= CurrentBet || betValue > GetMaximumBetValue())
                {
                    betStatus = BetActionStatus.BetValueNotValid;
                }
                else
                {
                    CurrentBet = betValue;
                    BetLeader = PlayerInTurn;
                    MoveToNextBettor();
                }
            }

            return betStatus;
        }

        /// <summary>
        /// Folds for the rest of this bidding round. When only one bidder remains, the challenge is ready and
        /// ChallengeResolution starts.
        /// </summary>
        /// <param name="playerId">Player folding.</param>
        /// <returns>Accepted if the player folded, otherwise the rejection reason.</returns>
        public BetActionStatus WithdrawFromBet(int playerId)
        {
            BetActionStatus betStatus = ValidateBetActor(playerId, RoundPhase.Betting, BetActionStatus.NoBetInProgress);

            if (betStatus == BetActionStatus.Accepted)
            {
                PlayerInTurn.HasWithdrawnFromBet = true;

                if (CountRemainingBettors() == 1)
                {
                    Phase = RoundPhase.ChallengeReady;
                    _playerInTurnIndex = _players.IndexOf(BetLeader);
                    Resolution = new ChallengeResolution(BetLeader, CurrentBet, Players);
                }
                else
                {
                    MoveToNextBettor();
                }
            }

            return betStatus;
        }

        private BetActionStatus ValidateBetActor(
            int playerId,
            RoundPhase requiredPhase,
            BetActionStatus wrongPhaseStatus)
        {
            BetActionStatus betStatus = BetActionStatus.Accepted;
            MatchPlayer player = FindPlayer(playerId);

            if (player == null)
            {
                betStatus = BetActionStatus.PlayerNotInMatch;
            }
            else if (Phase == RoundPhase.PunishmentPending)
            {
                betStatus = BetActionStatus.PunishmentPending;
            }
            else if (Phase == RoundPhase.MatchFinished)
            {
                betStatus = BetActionStatus.MatchFinished;
            }
            else if (Phase != requiredPhase)
            {
                betStatus = wrongPhaseStatus;
            }
            else if (!ReferenceEquals(player, PlayerInTurn))
            {
                betStatus = BetActionStatus.NotPlayersTurn;
            }

            return betStatus;
        }

        /// <summary>
        /// Checks the opening bid against the table, the player's card already placed this round, the empty-hand rule
        /// and the bid range.
        /// </summary>
        private BetActionStatus ValidateOpeningBetValue(int betValue)
        {
            BetActionStatus betStatus = BetActionStatus.Accepted;
            int maximumBetValue = GetMaximumBetValue();

            if (maximumBetValue == 0)
            {
                betStatus = BetActionStatus.TableEmpty;
            }
            else if (PlayerInTurn.PileCards.Count == 0)
            {
                betStatus = BetActionStatus.NoCardPlacedInRound;
            }
            else if (PlayerInTurn.HandCards.Count == 0 && betValue != EmptyHandBetValue)
            {
                betStatus = BetActionStatus.EmptyHandRequiresBetOfOne;
            }
            else if (betValue < MinimumBetValue || betValue > maximumBetValue)
            {
                betStatus = BetActionStatus.BetValueNotValid;
            }

            return betStatus;
        }

        /// <summary>Starts bidding with the current player in the lead and nobody folded.</summary>
        private void StartBetting(int betValue)
        {
            ClearWithdrawals();
            Phase = RoundPhase.Betting;
            CurrentBet = betValue;
            BetLeader = PlayerInTurn;
            MoveToNextBettor();
        }

        private int GetMaximumBetValue() => Math.Min(CardsOnTable, ModeRules.CardsPerPlayerCount);

        private int CountRemainingBettors() => _players.Count(player => IsBetting(player));

        /// <summary>Whether the player is active and has not folded in this bidding round.</summary>
        private static bool IsBetting(MatchPlayer player) => !player.HasWithdrawnFromBet && player.IsInPlay;

        /// <summary>Passes the turn to the next remaining bidder. At least one always remains.</summary>
        private void MoveToNextBettor()
        {
            do
            {
                _playerInTurnIndex = (_playerInTurnIndex + 1) % _players.Count;
            }
            while (!IsBetting(PlayerInTurn));
        }
    }
}
