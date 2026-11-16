using System;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Reglas fijas de una modalidad de partida: cartas por jugador, Gatos con Maullido Fatal, retos exitosos para
    /// ganar y vida extra (Dominio v3 §1). Es inmutable.
    /// </summary>
    public sealed class MatchModeRules
    {
        private const int SingleFatalCatCount = 1;
        private const int DoubleFatalCatCount = 2;
        private const int ShortMatchChallengesToWin = 2;
        private const int LongMatchChallengesToWin = 4;

        private MatchModeRules(int cardsPerPlayerCount, int fatalCatCount, int challengesToWin)
        {
            CardsPerPlayerCount = cardsPerPlayerCount;
            FatalCatCount = fatalCatCount;
            ChallengesToWin = challengesToWin;
        }

        /// <summary>Obtiene las cartas que recibe cada jugador: 4, 6 u 8.</summary>
        public int CardsPerPlayerCount { get; }

        /// <summary>Obtiene los Gatos con Maullido Fatal que recibe cada jugador.</summary>
        public int FatalCatCount { get; }

        /// <summary>Obtiene las cartas seguras que recibe cada jugador.</summary>
        public int SafeCardCount => CardsPerPlayerCount - FatalCatCount;

        /// <summary>Obtiene los retos exitosos que se necesitan para ganar la partida.</summary>
        public int ChallengesToWin { get; }

        /// <summary>Obtiene si la modalidad admite vida extra; solo la de 8 cartas.</summary>
        public bool AllowsExtraLife => CardsPerPlayerCount == ValidationRules.EightCardsPerPlayerCount;

        /// <summary>Obtiene las reglas de la modalidad con esa cantidad de cartas por jugador.</summary>
        /// <param name="cardsPerPlayerCount">Cartas por jugador: 4, 6 u 8; Services la valida antes.</param>
        /// <returns>Las reglas de la modalidad.</returns>
        /// <exception cref="ArgumentOutOfRangeException">La cantidad no es una modalidad permitida.</exception>
        public static MatchModeRules FromCardsPerPlayerCount(int cardsPerPlayerCount)
        {
            MatchModeRules modeRules;

            if (cardsPerPlayerCount == ValidationRules.FourCardsPerPlayerCount)
            {
                modeRules = new MatchModeRules(cardsPerPlayerCount, SingleFatalCatCount, ShortMatchChallengesToWin);
            }
            else if (cardsPerPlayerCount == ValidationRules.SixCardsPerPlayerCount)
            {
                modeRules = new MatchModeRules(cardsPerPlayerCount, SingleFatalCatCount, ShortMatchChallengesToWin);
            }
            else if (cardsPerPlayerCount == ValidationRules.EightCardsPerPlayerCount)
            {
                modeRules = new MatchModeRules(cardsPerPlayerCount, DoubleFatalCatCount, LongMatchChallengesToWin);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(cardsPerPlayerCount));
            }

            return modeRules;
        }
    }
}
