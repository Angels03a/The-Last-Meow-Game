using System.Collections.Generic;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Estado de un jugador en una partida: mano, pila de cartas colocadas, retos exitosos y vida extra. Solo el
    /// motor lo modifica; Services lo consulta.
    /// </summary>
    public sealed class MatchPlayer
    {
        private readonly List<CardKind> _hand = new List<CardKind>();
        private readonly List<CardKind> _pile = new List<CardKind>();

        /// <summary>Crea al jugador con la mano completa de la modalidad y la pila vacía.</summary>
        /// <param name="playerId">Identificador del jugador en la partida.</param>
        /// <param name="modeRules">Reglas de la modalidad, que fijan la composición de la mano.</param>
        internal MatchPlayer(int playerId, MatchModeRules modeRules)
        {
            PlayerId = playerId;
            Hand = _hand.AsReadOnly();
            Pile = _pile.AsReadOnly();

            for (int fatalCatIndex = 0; fatalCatIndex < modeRules.FatalCatCount; fatalCatIndex++)
            {
                _hand.Add(CardKind.FatalCat);
            }

            for (int safeCardIndex = 0; safeCardIndex < modeRules.SafeCardCount; safeCardIndex++)
            {
                _hand.Add(CardKind.Safe);
            }
        }

        /// <summary>Obtiene el identificador del jugador en la partida.</summary>
        public int PlayerId { get; }

        /// <summary>Obtiene las cartas sin colocar; solo su dueño puede verlas.</summary>
        public IReadOnlyList<CardKind> Hand { get; }

        /// <summary>Obtiene las cartas colocadas, boca abajo, de la más antigua a la que queda arriba.</summary>
        public IReadOnlyList<CardKind> Pile { get; }

        /// <summary>Obtiene las vidas del jugador: las cartas de la mano más las de la pila.</summary>
        public int Lives => _hand.Count + _pile.Count;

        /// <summary>Obtiene los retos exitosos del jugador.</summary>
        public int SuccessfulChallenges { get; internal set; }

        /// <summary>Obtiene si el jugador conserva su vida extra; solo existe en la modalidad de 8 cartas.</summary>
        public bool HasExtraLife { get; internal set; }

        /// <summary>
        /// Obtiene el orden en que el jugador alcanzó los retos necesarios para ganar (1 es el primero), o null si
        /// aún no los alcanza. Sirve para el desempate; la etapa de colocación no lo usa.
        /// </summary>
        public int? ChallengeReachedOrder { get; internal set; }

        /// <summary>Quita la carta de la mano y la deja arriba de la pila, si la mano la contiene.</summary>
        /// <param name="card">Tipo de carta que se coloca.</param>
        /// <returns>true si la carta se colocó; false si la mano no la tiene.</returns>
        internal bool TryPlaceCard(CardKind card)
        {
            bool wasPlaced = _hand.Remove(card);

            if (wasPlaced)
            {
                _pile.Add(card);
            }

            return wasPlaced;
        }
    }
}
