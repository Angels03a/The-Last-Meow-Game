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
