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
        private const int NoBetValue = 0;
        private const int SingleLife = 1;
        private const int OnlyCardIndex = 0;
        private const int LastSurvivorCount = 1;

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

        /// <summary>Obtiene el último castigo aplicado, con la carta que se perdió, o null si no hubo.</summary>
        public PendingPunishment LastPunishment { get; private set; }

        /// <summary>Obtiene a los jugadores eliminados, en el orden en que quedaron sin vidas.</summary>
        public IReadOnlyList<MatchPlayer> EliminatedPlayers { get; }

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

        /// <summary>
        /// Voltea la carta de arriba sin voltear de la pila de un jugador durante la resolución del reto. Primero se
        /// voltea la pila propia del apostador y después elige el dueño antes de cada volteo. El reto termina al
        /// alcanzar el número prometido (éxito) o al revelar un Gato sin vida extra (fallo). Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que pide voltear; debe ser el apostador.</param>
        /// <param name="pileOwnerId">Dueño de la pila de la que se voltea.</param>
        /// <param name="nowUtc">Hora UTC del servidor; si el reto falla, de ella sale el plazo del castigo.</param>
        /// <returns>Revealed, ChallengeSucceeded o ChallengeFailed; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        public ChallengeRevealStatus RevealCard(int playerId, int pileOwnerId, DateTime nowUtc)
        {
            ValidateUtc(nowUtc);
            ChallengeRevealStatus revealStatus;
            MatchPlayer pileOwner = FindPlayer(pileOwnerId);

            if (FindPlayer(playerId) == null)
            {
                revealStatus = ChallengeRevealStatus.PlayerNotInMatch;
            }
            else if (Phase != RoundPhase.ChallengeReady)
            {
                revealStatus = ChallengeRevealStatus.NoChallengeInProgress;
            }
            else if (playerId != BetLeader.PlayerId)
            {
                revealStatus = ChallengeRevealStatus.NotTheBettor;
            }
            else if (pileOwner == null)
            {
                revealStatus = ChallengeRevealStatus.PileOwnerNotInMatch;
            }
            else
            {
                revealStatus = Resolution.RevealCard(pileOwner);
                ApplyChallengeOutcome(revealStatus, nowUtc);
            }

            return revealStatus;
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
        /// Elige, una sola vez y antes de que venza el plazo, la carta que pierde el castigado. Solo puede elegir quien
        /// indica PendingPunishment. Después se aplica la eliminación si quedó sin vidas, se comprueba el fin de la
        /// partida y, si no termina, empieza una ronda con el primer jugador activo. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que elige.</param>
        /// <param name="cardIndex">Índice en la mano real del castigado; llega del cliente y no es confiable.</param>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>CardRemoved si se eliminó la carta; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        public PunishmentStatus ChoosePunishmentCard(int playerId, int cardIndex, DateTime nowUtc)
        {
            ValidateUtc(nowUtc);
            PunishmentStatus punishmentStatus;

            if (FindPlayer(playerId) == null)
            {
                punishmentStatus = PunishmentStatus.PlayerNotInMatch;
            }
            else if (Phase != RoundPhase.PunishmentPending)
            {
                punishmentStatus = PunishmentStatus.NoPunishmentPending;
            }
            else
            {
                punishmentStatus = PendingPunishment.ValidateChoice(playerId, cardIndex, nowUtc);
            }

            if (punishmentStatus == PunishmentStatus.CardRemoved)
            {
                ApplyPunishment(cardIndex);
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Resuelve el castigo cuando venció el plazo (nowUtc igual o posterior a DeadlineUtc): el sistema elige al
        /// azar, con la misma probabilidad para cada carta del castigado, incluido el Gato. Antes del plazo se
        /// rechaza. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <param name="pickRandomIndex">
        /// Azar que provee Services: recibe la cantidad n de cartas y devuelve un índice uniforme en [0, n).
        /// Nunca debe venir del cliente.
        /// </param>
        /// <returns>CardRemoved si se eliminó la carta; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        /// <exception cref="ArgumentNullException">pickRandomIndex es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">El azar devolvió un índice fuera de [0, n).</exception>
        public PunishmentStatus ExpirePunishment(DateTime nowUtc, Func<int, int> pickRandomIndex)
        {
            ValidateUtc(nowUtc);

            if (pickRandomIndex == null)
            {
                throw new ArgumentNullException(nameof(pickRandomIndex));
            }

            PunishmentStatus punishmentStatus = PunishmentStatus.CardRemoved;

            if (Phase != RoundPhase.PunishmentPending)
            {
                punishmentStatus = PunishmentStatus.NoPunishmentPending;
            }
            else if (!PendingPunishment.HasExpired(nowUtc))
            {
                punishmentStatus = PunishmentStatus.DeadlineNotReached;
            }
            else
            {
                ResolvePunishmentAtRandom(pickRandomIndex);
            }

            return punishmentStatus;
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

        /// <summary>Cierra la resolución según su desenlace; mientras el reto continúa no cambia nada.</summary>
        private void ApplyChallengeOutcome(ChallengeRevealStatus revealStatus, DateTime nowUtc)
        {
            if (revealStatus == ChallengeRevealStatus.ChallengeSucceeded)
            {
                CompleteChallengeSuccess();
            }
            else if (revealStatus == ChallengeRevealStatus.ChallengeFailed)
            {
                CompleteChallengeFailure(nowUtc);
            }
        }

        /// <summary>
        /// Reto exitoso: suma un reto al apostador y todas las cartas jugadas vuelven a las manos. Si llegó a los retos
        /// de la modalidad, la partida termina con su victoria; si no, empieza la siguiente ronda.
        /// </summary>
        private void CompleteChallengeSuccess()
        {
            MatchPlayer bettor = BetLeader;
            Resolution.ReturnPilesToHands();
            bettor.SuccessfulChallengeCount++;

            if (bettor.SuccessfulChallengeCount >= ModeRules.SuccessfulChallengesToWin)
            {
                Winner = bettor;
                Phase = RoundPhase.MatchFinished;
            }
            else
            {
                StartNextRound();
            }
        }

        /// <summary>
        /// Reto fallido: todas las cartas jugadas vuelven a las manos y empieza el castigo pendiente, con plazo de
        /// PunishmentChoiceSeconds. Elige el dueño de la pila del Gato, a ciegas si no es el propio apostador. Si el
        /// apostador tiene una sola vida no hay nada que elegir: la carta se elimina en la misma transición.
        /// </summary>
        private void CompleteChallengeFailure(DateTime nowUtc)
        {
            MatchPlayer bettor = BetLeader;
            MatchPlayer chooser = Resolution.FatalCatOwner;
            bool isBlindChoice = !ReferenceEquals(chooser, bettor);
            DateTime deadlineUtc = nowUtc.AddSeconds(ValidationRules.PunishmentChoiceSeconds);
            Resolution.ReturnPilesToHands();
            PendingPunishment = new PendingPunishment(chooser.PlayerId, bettor, isBlindChoice, deadlineUtc);
            Phase = RoundPhase.PunishmentPending;

            if (bettor.Lives == SingleLife)
            {
                ApplyPunishment(OnlyCardIndex);
            }
        }

        /// <summary>
        /// Elige al azar la carta del castigado y aplica el castigo. Lo usa el vencimiento del plazo y lo reutilizará
        /// la expulsión de quien elige (E6). Valida el índice antes de cambiar cualquier estado.
        /// </summary>
        private void ResolvePunishmentAtRandom(Func<int, int> pickRandomIndex)
        {
            int cardIndex = pickRandomIndex(PendingPunishment.PunishedCardCount);

            if (!PendingPunishment.IsValidCardIndex(cardIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(pickRandomIndex), "El azar devolvió un índice inválido.");
            }

            ApplyPunishment(cardIndex);
        }

        /// <summary>
        /// Quita la carta, registra la eliminación si el castigado quedó sin vidas y comprueba el fin: con un solo
        /// jugador activo gana él; si no, empieza la siguiente ronda con el primer jugador activo del orden.
        /// </summary>
        private void ApplyPunishment(int cardIndex)
        {
            PendingPunishment punishment = PendingPunishment;
            punishment.RemoveCard(cardIndex);
            LastPunishment = punishment;
            PendingPunishment = null;

            if (punishment.PunishedPlayer.IsEliminated)
            {
                _eliminatedPlayers.Add(punishment.PunishedPlayer);
            }

            if (_players.Count(player => !player.IsEliminated) == LastSurvivorCount)
            {
                Winner = _players.First(player => !player.IsEliminated);
                Phase = RoundPhase.MatchFinished;
            }
            else
            {
                StartNextRound();
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

        private int GetMaximumBetValue() => Math.Min(CardsOnTable, ModeRules.CardsPerPlayerCount);

        private int CountRemainingBettors() => _players.Count(player => IsBetting(player));

        /// <summary>Indica si el jugador sigue en la apuesta en curso: no se retiró ni está eliminado.</summary>
        private static bool IsBetting(MatchPlayer player) => !player.HasWithdrawnFromBet && !player.IsEliminated;

        /// <summary>Pasa el turno al siguiente del orden que sigue en la apuesta; siempre queda alguno.</summary>
        private void MoveToNextBettor()
        {
            do
            {
                _playerInTurnIndex = (_playerInTurnIndex + 1) % _players.Count;
            }
            while (!IsBetting(PlayerInTurn));
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
