using System;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Fixed mode rules for cards, Fatal Cats, challenges needed to win and the extra life (Domain v3 §1).
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

        /// <summary>Cards dealt to each player (4, 6 or 8).</summary>
        public int CardsPerPlayerCount { get; }

        /// <summary>Fatal Cats dealt to each player.</summary>
        public int FatalCatCount { get; }

        /// <summary>Safe cards dealt to each player.</summary>
        public int SafeCardCount => CardsPerPlayerCount - FatalCatCount;

        /// <summary>Successful challenges needed to win.</summary>
        public int SuccessfulChallengesToWin { get; }

        /// <summary>Winner's bonus in this mode (+1, +2 or +3, Rule 5.0.1).</summary>
        public int WinnerBonusPoints => SelectWinnerBonusPoints(CardsPerPlayerCount);

        /// <summary>Non-winner's bonus in this mode (0, +1 or +2, Rule 5.0.1).</summary>
        public int NonWinnerBonusPoints => SelectNonWinnerBonusPoints(CardsPerPlayerCount);

        /// <summary>Whether this mode allows an extra life. Only eight-card mode does.</summary>
        public bool AllowsExtraLife => CardsPerPlayerCount == ValidationRules.EightCardsPerPlayerCount;

        /// <summary>Returns the mode rules for the given number of cards per player.</summary>
        /// <param name="cardsPerPlayerCount">Cards per player (4, 6 or 8), already checked by Services.</param>
        /// <returns>Rules for the selected mode.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The card count does not match an allowed mode.</exception>
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

        /// <summary>Winner's bonus by cards per player. The factory has already checked the mode.</summary>
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

        /// <summary>Non-winner's bonus by cards per player. The factory has already checked the mode.</summary>
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
