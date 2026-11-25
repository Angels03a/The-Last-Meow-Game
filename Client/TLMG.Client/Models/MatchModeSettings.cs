namespace TLMG.Client.Models
{
    public sealed class MatchModeSettings
    {
        public const int FourCardsPerPlayer = 4;
        public const int SixCardsPerPlayer = 6;
        public const int EightCardsPerPlayer = 8;
        private const int SingleFatalCatCount = 1;
        private const int DoubleFatalCatCount = 2;
        private const int ShortMatchChallengesToWin = 2;
        private const int LongMatchChallengesToWin = 4;
        private const int TwoRowHandColumnCount = 4;

        private MatchModeSettings()
        {
        }

        public int CardsPerPlayer { get; private set; }

        public int FatalCatCount { get; private set; }

        public int ChallengesToWin { get; private set; }

        public bool HasFugitiveKitten { get; private set; }

        public bool IsCompactHand { get; private set; }

        public int HandColumnCount { get; private set; }

        public int SafeCardCount
        {
            get { return CardsPerPlayer - FatalCatCount; }
        }

        public static MatchModeSettings FromCardsPerPlayer(int cardsPerPlayer)
        {
            MatchModeSettings matchModeSettings;

            switch (cardsPerPlayer)
            {
                case SixCardsPerPlayer:
                    matchModeSettings = Create(SixCardsPerPlayer, SingleFatalCatCount, ShortMatchChallengesToWin);
                    break;
                case EightCardsPerPlayer:
                    matchModeSettings = Create(EightCardsPerPlayer, DoubleFatalCatCount, LongMatchChallengesToWin);
                    break;
                default:
                    matchModeSettings = Create(FourCardsPerPlayer, SingleFatalCatCount, ShortMatchChallengesToWin);
                    break;
            }

            return matchModeSettings;
        }

        private static MatchModeSettings Create(int cardsPerPlayer, int fatalCatCount, int challengesToWin)
        {
            bool isEightCardMode = cardsPerPlayer == EightCardsPerPlayer;
            int handColumnCount = cardsPerPlayer;

            if (isEightCardMode)
            {
                handColumnCount = TwoRowHandColumnCount;
            }

            MatchModeSettings matchModeSettings = new MatchModeSettings
            {
                CardsPerPlayer = cardsPerPlayer,
                FatalCatCount = fatalCatCount,
                ChallengesToWin = challengesToWin,
                HasFugitiveKitten = isEightCardMode,
                IsCompactHand = cardsPerPlayer > FourCardsPerPlayer,
                HandColumnCount = handColumnCount
            };

            return matchModeSettings;
        }
    }
}