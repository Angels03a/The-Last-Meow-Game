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
        private const int FourCardsWinnerBonusPoints = 1;
        private const int SixCardsWinnerBonusPoints = 2;
        private const int EightCardsWinnerBonusPoints = 3;
        private const int FourCardsNonWinnerBonusPoints = 0;
        private const int SixCardsNonWinnerBonusPoints = 1;
        private const int EightCardsNonWinnerBonusPoints = 2;

        private MatchModeRules(int cardsPerPlayerCount, int fatalCatCount, int successfulChallengesToWin)
        {
            CardsPerPlayerCount = cardsPerPlayerCount;
            FatalCatCount = fatalCatCount;
            SuccessfulChallengesToWin = successfulChallengesToWin;
        }

        /// <summary>Obtiene las cartas que recibe cada jugador: 4, 6 u 8.</summary>
        public int CardsPerPlayerCount { get; }

        /// <summary>Obtiene los Gatos con Maullido Fatal que recibe cada jugador.</summary>
        public int FatalCatCount { get; }

        /// <summary>Obtiene las cartas seguras que recibe cada jugador.</summary>
        public int SafeCardCount => CardsPerPlayerCount - FatalCatCount;

        /// <summary>Obtiene los retos exitosos que se necesitan para ganar la partida.</summary>
        public int SuccessfulChallengesToWin { get; }

        /// <summary>Obtiene el bono de puntos del ganador en esta modalidad: +1, +2 o +3 (Reglas 5.0.1).</summary>
        public int WinnerBonusPoints => SelectWinnerBonusPoints(CardsPerPlayerCount);

        /// <summary>Obtiene el bono de puntos de quien no gana en esta modalidad: 0, +1 o +2 (Reglas 5.0.1).</summary>
        public int NonWinnerBonusPoints => SelectNonWinnerBonusPoints(CardsPerPlayerCount);

        /// <summary>Obtiene si la modalidad admite vida extra; solo la de 8 cartas.</summary>
        public bool AllowsExtraLife => CardsPerPlayerCount == ValidationRules.EightCardsPerPlayerCount;

        /// <summary>Obtiene las reglas de la modalidad con esa cantidad de cartas por jugador.</summary>
        /// <param name="cardsPerPlayerCount">Cartas por jugador: 4, 6 u 8; Services la valida antes.</param>
        /// <returns>Las reglas de la modalidad.</returns>
        /// <exception cref="ArgumentOutOfRangeException">La cantidad no es una modalidad permitida.</exception>
        public static MatchModeRules CreateFromCardsPerPlayerCount(int cardsPerPlayerCount)
        {
            MatchModeRules modeRules;

            bool isShortMode = cardsPerPlayerCount == ValidationRules.FourCardsPerPlayerCount
                || cardsPerPlayerCount == ValidationRules.SixCardsPerPlayerCount;

            if (isShortMode)
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

        /// <summary>Bono del ganador según las cartas por jugador; la fábrica ya validó la modalidad.</summary>
        private static int SelectWinnerBonusPoints(int cardsPerPlayerCount)
        {
            int winnerBonusPoints;

            switch (cardsPerPlayerCount)
            {
                case ValidationRules.FourCardsPerPlayerCount:
                    winnerBonusPoints = FourCardsWinnerBonusPoints;
                    break;
                case ValidationRules.SixCardsPerPlayerCount:
                    winnerBonusPoints = SixCardsWinnerBonusPoints;
                    break;
                case ValidationRules.EightCardsPerPlayerCount:
                    winnerBonusPoints = EightCardsWinnerBonusPoints;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cardsPerPlayerCount));
            }

            return winnerBonusPoints;
        }

        /// <summary>Bono de quien no gana según las cartas por jugador; la fábrica ya validó la modalidad.</summary>
        private static int SelectNonWinnerBonusPoints(int cardsPerPlayerCount)
        {
            int nonWinnerBonusPoints;

            switch (cardsPerPlayerCount)
            {
                case ValidationRules.FourCardsPerPlayerCount:
                    nonWinnerBonusPoints = FourCardsNonWinnerBonusPoints;
                    break;
                case ValidationRules.SixCardsPerPlayerCount:
                    nonWinnerBonusPoints = SixCardsNonWinnerBonusPoints;
                    break;
                case ValidationRules.EightCardsPerPlayerCount:
                    nonWinnerBonusPoints = EightCardsNonWinnerBonusPoints;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cardsPerPlayerCount));
            }

            return nonWinnerBonusPoints;
        }
    }
}
