using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Estado de una partida en curso: reglas de la modalidad, jugadores en su orden de turnos y jugador en turno.
    /// Es una biblioteca pura (§4.2): valida y calcula; Services decide qué notificar y guardar. El orden de turnos
    /// es el de ingreso a la sala y se congela al crear la partida.
    /// </summary>
    public sealed class MatchState
    {
        private const int MinimumBetValue = 1;
        private const int EmptyHandBetValue = 1;

        private readonly List<MatchPlayer> _players = new List<MatchPlayer>();
        private int _playerInTurnIndex;

        /// <summary>Inicia una partida con la mano completa para cada jugador.</summary>
        /// <param name="modeRules">Reglas de la modalidad elegida en la sala.</param>
        /// <param name="playerIdsInJoinOrder">Identificadores de los jugadores, en orden de ingreso a la sala.</param>
        /// <exception cref="ArgumentNullException">Alguno de los argumentos es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Hay menos de 3 o más de 6 jugadores.</exception>
        /// <exception cref="ArgumentException">Un identificador de jugador se repite.</exception>
        public MatchState(MatchModeRules modeRules, IReadOnlyList<int> playerIdsInJoinOrder)
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

            HashSet<int> seenPlayerIds = new HashSet<int>();

            foreach (int playerId in playerIdsInJoinOrder)
            {
                if (!seenPlayerIds.Add(playerId))
                {
                    throw new ArgumentException("Un jugador se repite.", nameof(playerIdsInJoinOrder));
                }

                _players.Add(new MatchPlayer(playerId, modeRules));
            }

            ModeRules = modeRules;
            Players = _players.AsReadOnly();
        }

        /// <summary>Obtiene las reglas de la modalidad de la partida.</summary>
        public MatchModeRules ModeRules { get; }

        /// <summary>Obtiene a los jugadores en su orden fijo de turnos.</summary>
        public IReadOnlyList<MatchPlayer> Players { get; }

        /// <summary>Obtiene al jugador que tiene el turno; la partida empieza con el primero del orden.</summary>
        public MatchPlayer PlayerInTurn => _players[_playerInTurnIndex];

        /// <summary>Obtiene la fase de la ronda: colocando cartas, apostando o con el reto listo.</summary>
        public RoundPhase Phase { get; private set; }

        /// <summary>Obtiene la apuesta actual, o 0 si no hay; con el reto listo, las cartas a voltear.</summary>
        public int CurrentBet { get; private set; }

        /// <summary>Obtiene a quien tiene la apuesta más alta (el apostador con el reto listo), o null.</summary>
        public MatchPlayer BetLeader { get; private set; }

        /// <summary>Obtiene las cartas boca abajo en la mesa: la suma de las pilas de los jugadores.</summary>
        public int CardsOnTable => _players.Sum(player => player.PileCards.Count);

        /// <summary>
        /// Coloca una carta de la mano del jugador arriba de su pila y pasa el turno al siguiente del orden. No
        /// cambia las vidas. Si rechaza la acción, el estado no cambia.
        /// </summary>
        /// <param name="playerId">Jugador que pide colocar.</param>
        /// <param name="card">Tipo de carta que coloca.</param>
        /// <returns>Placed si se colocó; en otro caso, el motivo del rechazo.</returns>
        public CardPlacementStatus PlaceCard(int playerId, CardKind card)
        {
            CardPlacementStatus placementStatus;
            MatchPlayer player = FindPlayer(playerId);

            if (player == null)
            {
                placementStatus = CardPlacementStatus.PlayerNotInMatch;
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
                _playerInTurnIndex = (_playerInTurnIndex + 1) % _players.Count;
                placementStatus = CardPlacementStatus.Placed;
            }

            return placementStatus;
        }

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
        /// apostador, el reto queda listo para resolverse (la resolución es de la etapa E3).
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

        /// <summary>Valida el valor con el que el jugador en turno quiere abrir: mesa, mano vacía y rango.</summary>
        private BetActionStatus ValidateOpeningBetValue(int betValue)
        {
            BetActionStatus betStatus = BetActionStatus.Accepted;
            int maximumBetValue = GetMaximumBetValue();

            if (maximumBetValue == 0)
            {
                betStatus = BetActionStatus.TableEmpty;
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
            foreach (MatchPlayer player in _players)
            {
                player.HasWithdrawnFromBet = false;
            }

            Phase = RoundPhase.Betting;
            CurrentBet = betValue;
            BetLeader = PlayerInTurn;
            MoveToNextBettor();
        }

        private int GetMaximumBetValue() => Math.Min(CardsOnTable, ModeRules.CardsPerPlayerCount);

        private int CountRemainingBettors() => _players.Count(player => !player.HasWithdrawnFromBet);

        /// <summary>Pasa el turno al siguiente del orden que no se haya retirado; siempre queda alguno.</summary>
        private void MoveToNextBettor()
        {
            do
            {
                _playerInTurnIndex = (_playerInTurnIndex + 1) % _players.Count;
            }
            while (PlayerInTurn.HasWithdrawnFromBet);
        }

        private MatchPlayer FindPlayer(int playerId) => _players.FirstOrDefault(player => player.PlayerId == playerId);
    }
}
