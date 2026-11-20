namespace TLMG.Client.Models
{
    /// <summary>
    /// Match settings used by the UI include cards per player, Fatal Cats, challenges needed to win and hand layout.
    /// These settings control the display and available controls. The server decides the rules (Reglas de The Last
    /// Meow, §2.0.4 and §2.0.5).
    /// </summary>
    public sealed class MatchModeSettings
    {
        /// <summary>Cards per player in the smallest mode, which is also the default.</summary>
        public const int FourCardsPerPlayer = 4;

        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int SingleFatalCatCount = 1;
        private const int DoubleFatalCatCount = 2;
        private const int ShortMatchChallengesToWin = 2;
        private const int LongMatchChallengesToWin = 4;
        private const int TwoRowHandColumnCount = 4;

        private MatchModeSettings()
        {
        }

        /// <summary>Cards dealt to each player (4, 6 or 8).</summary>
        public int CardsPerPlayer { get; private set; }

        /// <summary>Fatal Cats per player.</summary>
        public int FatalCatCount { get; private set; }

        /// <summary>Successful challenges needed to win.</summary>
        public int ChallengesToWin { get; private set; }

        /// <summary>Whether this mode includes Fugitive Kitten. Only the eight-card mode does.</summary>
        public bool HasFugitiveKitten { get; private set; }

        /// <summary>Whether to draw smaller cards so the hand fits.</summary>
        public bool IsCompactHand { get; private set; }

        /// <summary>Hand columns. Eight cards use two rows.</summary>
        public int HandColumnCount { get; private set; }

        /// <summary>Safe cards per player.</summary>
        public int SafeCardCount
        {
            get { return CardsPerPlayer - FatalCatCount; }
        }

        /// <summary>Creates settings for the given number of cards per player.</summary>
        /// <param name="cardsPerPlayer">Cards per player. An unknown value selects four-card mode.</param>
        /// <returns>Mode settings, always present.</returns>
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