using System;
using System.Linq;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int MinimumBetValue = 1;
        private const int EmptyHandBetValue = 1;

        /// <summary>
        /// Abre una apuesta como acción del turno, alternativa a colocar. El valor va de 1 al menor entre las cartas
        /// de la mesa y las de la modalidad; con la mano vacía debe ser exactamente 1. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que abre la apuesta.</param>
        /// <param name="betValue">Cartas que declara poder voltear.</param>
        /// <returns>Accepted si se abrió; en otro caso, el motivo del rechazo.</returns>
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
        /// Sube la apuesta en el turno del jugador: el valor debe ser estrictamente mayor que la apuesta actual y no
        /// pasar del menor entre las cartas de la mesa y las de la modalidad. Si rechaza, el estado no cambia.
        /// </summary>
        /// <param name="playerId">Jugador que sube.</param>
        /// <param name="betValue">Nueva apuesta, en cartas a voltear.</param>
        /// <returns>Accepted si se subió; en otro caso, el motivo del rechazo.</returns>
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
        /// Retira al jugador de la apuesta en curso; no puede volver a participar en ella. Cuando queda un solo
        /// apostador, el reto queda listo y empieza su resolución (ChallengeResolution).
        /// </summary>
        /// <param name="playerId">Jugador que se retira.</param>
        /// <returns>Accepted si se retiró; en otro caso, el motivo del rechazo.</returns>
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
        /// Valida el valor con el que el jugador en turno quiere abrir: mesa, carta propia ya colocada en la ronda,
        /// mano vacía y rango.
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

        /// <summary>Inicia la ronda de apuestas: nadie se ha retirado y el jugador en turno la lidera.</summary>
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

        /// <summary>Indica si el jugador sigue en la apuesta en curso: no se retiró y sigue en juego.</summary>
        private static bool IsBetting(MatchPlayer player) => !player.HasWithdrawnFromBet && player.IsInPlay;

        /// <summary>Pasa el turno al siguiente del orden que sigue en la apuesta; siempre queda alguno.</summary>
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
