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

        /// <summary>Obtiene si el jugador se retiró de la apuesta en curso y ya no participa en ella.</summary>
        public bool HasWithdrawnFromBet { get; internal set; }

        /// <summary>Obtiene los retos exitosos del jugador.</summary>
        public int SuccessfulChallengeCount { get; internal set; }

        /// <summary>Obtiene si el jugador conserva su vida extra; solo existe en la modalidad de 8 cartas.</summary>
        public bool HasExtraLife { get; internal set; }

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
    }
}
