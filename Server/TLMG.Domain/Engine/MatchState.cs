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
    public sealed partial class MatchState
    {
        private const int NoBetValue = 0;

        private readonly List<MatchPlayer> _players = new List<MatchPlayer>();
        private int _playerInTurnIndex;
        private readonly List<MatchPlayer> _eliminatedPlayers = new List<MatchPlayer>();
        private bool _hasExtraLifeBeenGranted;
        private bool _hasAnyCardBeenPlaced;

        /// <summary>Inicia una partida con la mano completa para cada jugador.</summary>
        /// <param name="modeRules">Reglas de la modalidad elegida en la sala.</param>
        /// <param name="playerIdsInJoinOrder">Identificadores de los jugadores, en orden de ingreso a la sala.</param>
        /// <exception cref="ArgumentNullException">Alguno de los argumentos es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Hay menos de 3 o más de 6 jugadores.</exception>
        /// <exception cref="ArgumentException">Un identificador de jugador se repite.</exception>
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

        /// <summary>
        /// Obtiene la última resolución disponible, o null si todavía no hubo ningún reto. Sigue disponible después de
        /// cerrarse para consultar la última carta volteada; las acciones permitidas las decide Phase.
        /// </summary>
        public ChallengeResolution Resolution { get; private set; }

        /// <summary>Obtiene el castigo pendiente de un reto fallido, o null si no hay.</summary>
        public PendingPunishment PendingPunishment { get; private set; }

        /// <summary>Obtiene al ganador por retos o por ser el último en pie, o null si la partida sigue.</summary>
        public MatchPlayer Winner { get; private set; }

        /// <summary>Obtiene las cartas boca abajo en la mesa: la suma de las pilas de los jugadores.</summary>
        public int CardsOnTable => _players.Sum(player => player.PileCards.Count);

        /// <summary>
        /// Coloca una carta de la mano del jugador arriba de su pila y pasa el turno al siguiente jugador activo del
        /// orden. No cambia las vidas. Si rechaza la acción, el estado no cambia.
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
        /// Otorga la vida extra a un jugador. Solo existe en la modalidad de 8 cartas y solo un jugador la obtiene por
        /// partida, una sola vez y antes de colocar la primera carta (2.0.4.1); no se acumula ni se transfiere. Quién
        /// la gana lo decide Services. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que recibe la vida extra.</param>
        /// <returns>Granted si se otorgó; en otro caso, el motivo del rechazo.</returns>
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
        /// Valida la creación de la partida antes de tocar ningún estado: argumentos no nulos, de 3 a 6 jugadores y
        /// sin identificadores repetidos.
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

        /// <summary>El motor solo recibe la hora UTC del servidor; nunca lee el reloj por su cuenta.</summary>
        private static void ValidateUtc(DateTime nowUtc)
        {
            if (nowUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La hora debe ser UTC.", nameof(nowUtc));
            }
        }

        /// <summary>Deja la mesa lista para colocar y da el turno al primer jugador activo del orden fijo.</summary>
        private void StartNextRound()
        {
            ClearWithdrawals();
            Phase = RoundPhase.PlacingCards;
            CurrentBet = NoBetValue;
            BetLeader = null;
            _playerInTurnIndex = _players.FindIndex(player => !player.IsEliminated);
        }

        private void ClearWithdrawals()
        {
            foreach (MatchPlayer player in _players)
            {
                player.HasWithdrawnFromBet = false;
            }
        }

        /// <summary>Pasa el turno al siguiente jugador activo del orden; quien coloca siempre lo está.</summary>
        private void MoveToNextActivePlayer()
        {
            do
            {
                _playerInTurnIndex = (_playerInTurnIndex + 1) % _players.Count;
            }
            while (PlayerInTurn.IsEliminated);
        }

        private MatchPlayer FindPlayer(int playerId) => _players.FirstOrDefault(player => player.PlayerId == playerId);
    }
}
