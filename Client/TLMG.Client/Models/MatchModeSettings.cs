namespace TLMG.Client.Models
{
    /// <summary>
    /// Datos de una modalidad de partida que la interfaz necesita: cartas por jugador, Gatos con Maullido Fatal,
    /// retos exitosos para ganar y disposición de la mano. Solo sirve para mostrar y limitar controles; las reglas
    /// las decide el servidor (Reglas de The Last Meow, §2.0.4 y §2.0.5).
    /// </summary>
    public sealed class MatchModeSettings
    {
        /// <summary>Cartas por jugador de la modalidad pequeña, que también es la predeterminada.</summary>
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

        /// <summary>Cartas que recibe cada jugador: 4, 6 u 8.</summary>
        public int CardsPerPlayer { get; private set; }

        /// <summary>Gatos con Maullido Fatal por jugador.</summary>
        public int FatalCatCount { get; private set; }

        /// <summary>Retos exitosos que se necesitan para ganar la partida.</summary>
        public int ChallengesToWin { get; private set; }

        /// <summary>Indica si la modalidad incluye el minijuego del Gatito Fugitivo; solo la de 8 cartas.</summary>
        public bool HasFugitiveKitten { get; private set; }

        /// <summary>Indica si las cartas de la mano se dibujan en tamaño reducido para que quepan.</summary>
        public bool IsCompactHand { get; private set; }

        /// <summary>Columnas de la mano; con 8 cartas se usan dos filas.</summary>
        public int HandColumnCount { get; private set; }

        /// <summary>Cartas seguras por jugador.</summary>
        public int SafeCardCount
        {
            get { return CardsPerPlayer - FatalCatCount; }
        }

        /// <summary>Crea la configuración de las cartas por jugador indicadas.</summary>
        /// <param name="cardsPerPlayer">Cartas por jugador; un valor desconocido usa la modalidad de 4 cartas.</param>
        /// <returns>La configuración de la modalidad; nunca es null.</returns>
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