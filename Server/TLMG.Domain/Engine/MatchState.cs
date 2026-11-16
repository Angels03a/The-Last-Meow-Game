using System;
using System.Collections.Generic;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Estado de una partida en curso: reglas de la modalidad, jugadores en su orden de turnos y jugador activo.
    /// Es una biblioteca pura (§4.2): valida y calcula; Services decide qué notificar y guardar. El orden de turnos
    /// es el de ingreso a la sala y se congela al crear la partida.
    /// </summary>
    public sealed class MatchState
    {
        private const int EmptyHandBetValue = 1;

        private readonly List<MatchPlayer> _players = new List<MatchPlayer>();
        private int _activePlayerIndex;

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
        public MatchPlayer ActivePlayer => _players[_activePlayerIndex];

        /// <summary>Obtiene la fase de la ronda: colocando cartas, apostando o con el reto listo.</summary>
        public MatchPhase Phase { get; private set; }

        /// <summary>Obtiene la apuesta actual, o 0 si no hay; con el reto listo, las cartas a voltear.</summary>
        public int CurrentBet { get; private set; }

        /// <summary>Obtiene a quien tiene la apuesta más alta (el apostador con el reto listo), o null.</summary>
        public MatchPlayer HighestBidder { get; private set; }

        /// <summary>Obtiene las cartas boca abajo en la mesa: la suma de las pilas de los jugadores.</summary>
        public int CardsOnTable
        {
            get
            {
                int cardCount = 0;

                foreach (MatchPlayer player in _players)
                {
                    cardCount += player.Pile.Count;
                }

                return cardCount;
            }
        }

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
            else if (Phase != MatchPhase.PlacingCards)
            {
                placementStatus = CardPlacementStatus.BetOpen;
            }
            else if (!ReferenceEquals(player, ActivePlayer))
            {
                placementStatus = CardPlacementStatus.NotPlayersTurn;
            }
            else if (player.Hand.Count == 0)
            {
                placementStatus = CardPlacementStatus.HandEmpty;
            }
            else if (!player.TryPlaceCard(card))
            {
                placementStatus = CardPlacementStatus.CardNotInHand;
            }
            else
            {
                _activePlayerIndex = (_activePlayerIndex + 1) % _players.Count;
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
            BetActionStatus betStatus = CheckBettingActor(
                playerId,
                MatchPhase.PlacingCards,
                BetActionStatus.BetAlreadyOpen);
            int maximumBetValue = GetMaximumBetValue();

            if (betStatus == BetActionStatus.Accepted)
            {
                if (maximumBetValue == 0)
                {
                    betStatus = BetActionStatus.TableEmpty;
                }
                else if (ActivePlayer.Hand.Count == 0 && betValue != EmptyHandBetValue)
                {
                    betStatus = BetActionStatus.EmptyHandRequiresBetOfOne;
                }
                else if (betValue < 1 || betValue > maximumBetValue)
                {
                    betStatus = BetActionStatus.BetValueNotValid;
                }
                else
                {
                    foreach (MatchPlayer player in _players)
                    {
                        player.HasWithdrawnFromBet = false;
                    }

                    Phase = MatchPhase.Betting;
                    CurrentBet = betValue;
                    HighestBidder = ActivePlayer;
                    MoveToNextBettor();
                }
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
            BetActionStatus betStatus = CheckBettingActor(playerId, MatchPhase.Betting, BetActionStatus.BetNotOpen);

            if (betStatus == BetActionStatus.Accepted)
            {
                if (betValue <= CurrentBet || betValue > GetMaximumBetValue())
                {
                    betStatus = BetActionStatus.BetValueNotValid;
                }
                else
                {
                    CurrentBet = betValue;
                    HighestBidder = ActivePlayer;
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
            BetActionStatus betStatus = CheckBettingActor(playerId, MatchPhase.Betting, BetActionStatus.BetNotOpen);

            if (betStatus == BetActionStatus.Accepted)
            {
                ActivePlayer.HasWithdrawnFromBet = true;

                if (CountRemainingBettors() == 1)
                {
                    Phase = MatchPhase.ChallengeReady;
                    _activePlayerIndex = _players.IndexOf(HighestBidder);
                }
                else
                {
                    MoveToNextBettor();
                }
            }

            return betStatus;
        }

        private BetActionStatus CheckBettingActor(
            int playerId,
            MatchPhase requiredPhase,
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
            else if (!ReferenceEquals(player, ActivePlayer))
            {
                betStatus = BetActionStatus.NotPlayersTurn;
            }

            return betStatus;
        }

        private int GetMaximumBetValue()
        {
            return Math.Min(CardsOnTable, ModeRules.CardsPerPlayerCount);
        }

        private int CountRemainingBettors()
        {
            int bettorCount = 0;

            foreach (MatchPlayer player in _players)
            {
                if (!player.HasWithdrawnFromBet)
                {
                    bettorCount++;
                }
            }

            return bettorCount;
        }

        /// <summary>Pasa el turno al siguiente del orden que no se haya retirado; siempre queda alguno.</summary>
        private void MoveToNextBettor()
        {
            do
            {
                _activePlayerIndex = (_activePlayerIndex + 1) % _players.Count;
            }
            while (ActivePlayer.HasWithdrawnFromBet);
        }

        private MatchPlayer FindPlayer(int playerId)
        {
            MatchPlayer foundPlayer = null;

            foreach (MatchPlayer player in _players)
            {
                if (player.PlayerId == playerId)
                {
                    foundPlayer = player;
                }
            }

            return foundPlayer;
        }
    }
}
