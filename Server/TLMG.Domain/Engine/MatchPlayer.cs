using System.Collections.Generic;
using System.Linq;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Estado de un jugador en una partida: mano, pila de cartas colocadas, retos exitosos y vida extra. Solo el
    /// motor lo modifica; Services lo consulta.
    /// </summary>
    public sealed class MatchPlayer
    {
        private const int LastIndexOffset = 1;
        private const int NoLives = 0;

        private readonly List<CardKind> _handCards = new List<CardKind>();
        private readonly List<CardKind> _pileCards = new List<CardKind>();

        /// <summary>Crea al jugador con la mano completa de la modalidad y la pila vacía.</summary>
        /// <param name="playerId">Identificador del jugador en la partida.</param>
        /// <param name="modeRules">Reglas de la modalidad, que fijan la composición de la mano.</param>
        internal MatchPlayer(int playerId, MatchModeRules modeRules)
        {
            PlayerId = playerId;
            HandCards = _handCards.AsReadOnly();
            PileCards = _pileCards.AsReadOnly();

            _handCards.AddRange(Enumerable.Repeat(CardKind.FatalCat, modeRules.FatalCatCount));
            _handCards.AddRange(Enumerable.Repeat(CardKind.Safe, modeRules.SafeCardCount));
        }

        /// <summary>Obtiene el identificador del jugador en la partida.</summary>
        public int PlayerId { get; }

        /// <summary>Obtiene las cartas sin colocar; solo su dueño puede verlas.</summary>
        public IReadOnlyList<CardKind> HandCards { get; }

        /// <summary>Obtiene las cartas colocadas, boca abajo, de la más antigua a la que queda arriba.</summary>
        public IReadOnlyList<CardKind> PileCards { get; }

        /// <summary>Obtiene las vidas del jugador: las cartas de la mano más las de la pila.</summary>
        public int Lives => _handCards.Count + _pileCards.Count;

        /// <summary>
        /// Obtiene si el jugador quedó eliminado: sus vidas llegaron a cero. Tener la mano vacía con cartas en la pila
        /// no es eliminación.
        /// </summary>
        public bool IsEliminated => Lives == NoLives;

        /// <summary>
        /// Obtiene la salida del jugador de la partida (abandono, desconexión no recuperada o votación), o null
        /// mientras sigue en ella.
        /// </summary>
        public PlayerExit Exit { get; internal set; }

        /// <summary>Obtiene si el jugador salió de la partida; sigue en Players, pero ya no participa.</summary>
        public bool HasLeftMatch => Exit != null;

        /// <summary>Obtiene si el jugador sigue en juego: ni eliminado ni salido de la partida.</summary>
        public bool IsInPlay => !IsEliminated && !HasLeftMatch;

        /// <summary>Obtiene si el jugador se retiró de la apuesta en curso y ya no participa en ella.</summary>
        public bool HasWithdrawnFromBet { get; internal set; }

        /// <summary>
        /// Obtiene cuántas cartas de la pila, contadas desde arriba, ya se voltearon en la resolución en curso; es 0
        /// fuera de una resolución.
        /// </summary>
        public int RevealedCardCount { get; private set; }

        /// <summary>Obtiene los retos exitosos del jugador.</summary>
        public int SuccessfulChallengeCount { get; internal set; }

        /// <summary>
        /// Obtiene la secuencia del último reto exitoso del jugador en la partida, o 0 si no tiene ninguno. Una
        /// secuencia menor indica que alcanzó antes su número actual de retos (Reglas 5.0.4).
        /// </summary>
        public int LastSuccessfulChallengeSequence { get; internal set; }

        /// <summary>Obtiene si el jugador conserva su vida extra; solo existe en la modalidad de 8 cartas.</summary>
        public bool HasExtraLife { get; internal set; }

        /// <summary>Obtiene las cartas de la pila que todavía están boca abajo.</summary>
        internal int UnrevealedCardCount => _pileCards.Count - RevealedCardCount;

        /// <summary>Quita la carta de la mano y la deja arriba de la pila, si la mano la contiene.</summary>
        /// <param name="card">Tipo de carta que se coloca.</param>
        /// <returns>true si la carta se colocó; false si la mano no la tiene.</returns>
        internal bool TryPlaceCard(CardKind card)
        {
            bool wasPlaced = _handCards.Remove(card);

            if (wasPlaced)
            {
                _pileCards.Add(card);
            }

            return wasPlaced;
        }

        /// <summary>Voltea la carta de arriba que sigue boca abajo; la pila no debe estar toda volteada.</summary>
        /// <returns>El tipo de la carta volteada.</returns>
        internal CardKind RevealTopCard()
        {
            CardKind revealedCard = _pileCards[UnrevealedCardCount - LastIndexOffset];
            RevealedCardCount++;

            return revealedCard;
        }

        /// <summary>Quita para siempre una carta de la mano; el índice debe ser válido.</summary>
        /// <param name="handIndex">Índice de la carta en la mano.</param>
        /// <returns>El tipo de la carta quitada.</returns>
        internal CardKind RemoveHandCardAt(int handIndex)
        {
            CardKind removedCard = _handCards[handIndex];
            _handCards.RemoveAt(handIndex);

            return removedCard;
        }

        /// <summary>Devuelve a la mano las cartas de la pila y la deja vacía y sin cartas volteadas.</summary>
        internal void ReturnPileToHand()
        {
            _handCards.AddRange(_pileCards);
            _pileCards.Clear();
            RevealedCardCount = 0;
        }
    }
}
