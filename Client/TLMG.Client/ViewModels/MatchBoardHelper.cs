using System.Collections.Generic;
using System.Linq;

using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Convierte la vista privada del servidor en el tablero que dibuja la pantalla. Solo copia y ordena lo recibido:
    /// no decide turnos, resultados ni permisos, y no inventa datos que la vista no trae.
    /// </summary>
    public static class MatchBoardHelper
    {
        private const string SafeCardSymbol = "🎴";
        private const string FatalCatSymbol = "☠️";
        private const string FaceDownCardSymbol = "🎴";
        private const string PlacingCardsLabelKey = "Gameplay_PhasePlacingCardsLabel";
        private const string BettingLabelKey = "Gameplay_PhaseBiddingLabel";
        private const string ChallengeReadyLabelKey = "Gameplay_PhaseChallengeReadyLabel";
        private const string PunishmentPendingLabelKey = "Gameplay_PhasePunishmentPendingLabel";
        private const string MatchFinishedLabelKey = "Gameplay_PhaseMatchFinishedLabel";
        private const string UnknownPhaseLabelKey = "Common_UnexpectedError";

        private static readonly Dictionary<MatchRoundPhase, string> _labelKeysByPhase =
            new Dictionary<MatchRoundPhase, string>
            {
                { MatchRoundPhase.PlacingCards, PlacingCardsLabelKey },
                { MatchRoundPhase.Betting, BettingLabelKey },
                { MatchRoundPhase.ChallengeReady, ChallengeReadyLabelKey },
                { MatchRoundPhase.PunishmentPending, PunishmentPendingLabelKey },
                { MatchRoundPhase.MatchFinished, MatchFinishedLabelKey }
            };

        /// <summary>
        /// Lugares de la mesa según cuántos rivales hay, en el orden de turnos que sigue al jugador local; el
        /// siguiente en jugar queda a la izquierda.
        /// </summary>
        private static readonly OpponentSlot[][] _layouts =
        {
            new[] { OpponentSlot.Top },
            new[] { OpponentSlot.Left, OpponentSlot.Right },
            new[] { OpponentSlot.Left, OpponentSlot.Top, OpponentSlot.Right },
            new[] { OpponentSlot.Left, OpponentSlot.TopLeft, OpponentSlot.TopRight, OpponentSlot.Right },
            new[]
            {
                OpponentSlot.Left, OpponentSlot.TopLeft, OpponentSlot.Top, OpponentSlot.TopRight, OpponentSlot.Right
            }
        };

        /// <summary>Crea el tablero vacío que se muestra antes de que llegue la primera vista.</summary>
        /// <returns>Un tablero sin jugadores ni cartas.</returns>
        public static MatchBoard CreateEmptyBoard()
        {
            MatchBoard board = new MatchBoard
            {
                PhaseLabelKey = string.Empty,
                Players = new List<MatchPlayerEntry>().AsReadOnly(),
                HandCards = new List<HandCardEntry>().AsReadOnly(),
                OwnPileCards = new List<PileCardEntry>().AsReadOnly()
            };

            return board;
        }

        /// <summary>Arma el tablero que corresponde a una vista del servidor.</summary>
        /// <param name="matchView">Vista privada recibida; si es null o sin jugadores, el tablero queda vacío.</param>
        /// <param name="localUserId">Cuenta del jugador local, que decide su lugar y si es su turno.</param>
        /// <param name="cardsPerPlayer">Cartas por jugador de la modalidad; son las vidas con que se empieza.</param>
        /// <returns>El tablero nuevo; nunca es null.</returns>
        public static MatchBoard CreateBoard(MatchView matchView, int localUserId, int cardsPerPlayer)
        {
            MatchBoard board = CreateEmptyBoard();

            if (matchView != null && matchView.Players != null)
            {
                List<MatchPlayerEntry> players = CreatePlayerEntries(matchView, cardsPerPlayer);
                bool isLocalPlayerTurn = matchView.PlayerInTurnUserId == localUserId;

                board.HasMatchView = true;
                board.RoundNumber = matchView.RoundNumber;
                board.PhaseLabelKey = FindPhaseLabelKey(matchView.Phase);
                board.IsLocalPlayerTurn = isLocalPlayerTurn;
                board.CanPlaceCards = isLocalPlayerTurn && matchView.Phase == MatchRoundPhase.PlacingCards;
                board.Players = players.AsReadOnly();
                board.LocalPlayer = players.FirstOrDefault(player => player.UserId == localUserId);
                board.HandCards = CreateHandCards(matchView.OwnHandCards);
                board.OwnPileCards = CreateOwnPileCards(matchView.OwnPileCards);
                AssignOpponents(board, players, localUserId);
            }

            return board;
        }

        /// <summary>Convierte el tipo de carta que dibuja la pantalla al que entiende el servidor.</summary>
        /// <param name="handCardKind">Aspecto de la carta que eligió el jugador.</param>
        /// <returns>FatalCat para la calavera; Safe para cualquier otra.</returns>
        public static MatchCardKind ConvertToServerCard(HandCardKind handCardKind)
        {
            MatchCardKind serverCard = MatchCardKind.Safe;

            if (handCardKind == HandCardKind.Skull)
            {
                serverCard = MatchCardKind.FatalCat;
            }

            return serverCard;
        }

        /// <summary>Busca la clave de recurso del nombre de una fase.</summary>
        /// <param name="roundPhase">Fase recibida del servidor.</param>
        /// <returns>La clave de la fase; una clave genérica si la fase no se conoce.</returns>
        public static string FindPhaseLabelKey(MatchRoundPhase roundPhase)
        {
            string labelKey;

            if (!_labelKeysByPhase.TryGetValue(roundPhase, out labelKey))
            {
                labelKey = UnknownPhaseLabelKey;
            }

            return labelKey;
        }

        /// <summary>
        /// Crea la mano a partir de los tipos recibidos, en el mismo orden. Las cartas seguras alternan su aspecto
        /// (egipcio y zombi) solo para variar el dibujo; el servidor solo conoce Safe y FatalCat.
        /// </summary>
        /// <param name="ownHandCards">Mano del jugador local; puede ser null.</param>
        /// <returns>Las cartas de la mano; vacía si no hay.</returns>
        public static IReadOnlyList<HandCardEntry> CreateHandCards(IEnumerable<MatchCardKind> ownHandCards)
        {
            List<HandCardEntry> handCards = new List<HandCardEntry>();
            bool isNextSafeCardEgyptian = true;

            foreach (MatchCardKind cardKind in ownHandCards ?? Enumerable.Empty<MatchCardKind>())
            {
                HandCardKind handCardKind = HandCardKind.Skull;

                if (cardKind == MatchCardKind.Safe)
                {
                    handCardKind = isNextSafeCardEgyptian ? HandCardKind.Egyptian : HandCardKind.Zombie;
                    isNextSafeCardEgyptian = !isNextSafeCardEgyptian;
                }

                handCards.Add(new HandCardEntry { Kind = handCardKind });
            }

            return handCards.AsReadOnly();
        }

        /// <summary>
        /// Reparte a los rivales por la mesa: se recorre el orden de turnos empezando por quien juega después del
        /// jugador local, y cada uno ocupa el siguiente lugar del diseño que corresponde al número de rivales.
        /// </summary>
        /// <param name="players">Jugadores en su orden fijo de turnos.</param>
        /// <param name="localUserId">Cuenta del jugador local.</param>
        /// <returns>Cada rival con su lugar, en el orden en que se dibujan.</returns>
        public static IReadOnlyList<KeyValuePair<OpponentSlot, MatchPlayerEntry>> ArrangeOpponents(
            IReadOnlyList<MatchPlayerEntry> players,
            int localUserId)
        {
            List<MatchPlayerEntry> opponents = ListOpponentsInTurnOrder(players, localUserId);
            OpponentSlot[] layout = _layouts.FirstOrDefault(slots => slots.Length == opponents.Count)
                ?? new OpponentSlot[0];
            List<KeyValuePair<OpponentSlot, MatchPlayerEntry>> arrangement =
                new List<KeyValuePair<OpponentSlot, MatchPlayerEntry>>();

            for (int opponentIndex = 0; opponentIndex < layout.Length; opponentIndex++)
            {
                arrangement.Add(
                    new KeyValuePair<OpponentSlot, MatchPlayerEntry>(layout[opponentIndex], opponents[opponentIndex]));
            }

            return arrangement.AsReadOnly();
        }

        private static List<MatchPlayerEntry> CreatePlayerEntries(MatchView matchView, int cardsPerPlayer)
        {
            List<MatchPlayerEntry> players = new List<MatchPlayerEntry>();

            foreach (MatchPlayerView playerView in matchView.Players)
            {
                players.Add(new MatchPlayerEntry
                {
                    UserId = playerView.UserId,
                    Nickname = playerView.Nickname,
                    HandCardCount = playerView.HandCardCount,
                    PileCardCount = playerView.PileCardCount,
                    LivesCount = playerView.HandCardCount + playerView.PileCardCount,
                    MaximumLivesCount = cardsPerPlayer,
                    IsEliminated = playerView.IsEliminated,
                    IsInTurn = playerView.UserId == matchView.PlayerInTurnUserId,
                    PileCards = CreateFaceDownPile(playerView.PileCardCount)
                });
            }

            return players;
        }

        private static IReadOnlyList<PileCardEntry> CreateFaceDownPile(int pileCardCount)
        {
            IReadOnlyList<PileCardEntry> faceDownPile = Enumerable.Range(0, pileCardCount)
                .Select(cardNumber => new PileCardEntry { Symbol = FaceDownCardSymbol })
                .ToList()
                .AsReadOnly();

            return faceDownPile;
        }

        private static IReadOnlyList<PileCardEntry> CreateOwnPileCards(IEnumerable<MatchCardKind> ownPileCards)
        {
            IReadOnlyList<PileCardEntry> pile = (ownPileCards ?? Enumerable.Empty<MatchCardKind>())
                .Select(cardKind => new PileCardEntry { Symbol = FindSymbol(cardKind) })
                .ToList()
                .AsReadOnly();

            return pile;
        }

        private static string FindSymbol(MatchCardKind cardKind)
        {
            string symbol = SafeCardSymbol;

            if (cardKind == MatchCardKind.FatalCat)
            {
                symbol = FatalCatSymbol;
            }

            return symbol;
        }

        private static List<MatchPlayerEntry> ListOpponentsInTurnOrder(
            IReadOnlyList<MatchPlayerEntry> players,
            int localUserId)
        {
            int localIndex = players.ToList().FindIndex(player => player.UserId == localUserId);
            List<MatchPlayerEntry> opponents = new List<MatchPlayerEntry>();

            if (localIndex >= 0)
            {
                opponents.AddRange(players.Skip(localIndex + 1));
                opponents.AddRange(players.Take(localIndex));
            }

            return opponents;
        }

        private static void AssignOpponents(MatchBoard board, IReadOnlyList<MatchPlayerEntry> players, int localUserId)
        {
            foreach (KeyValuePair<OpponentSlot, MatchPlayerEntry> placement in ArrangeOpponents(players, localUserId))
            {
                switch (placement.Key)
                {
                    case OpponentSlot.Left:
                        board.OpponentLeft = placement.Value;
                        break;
                    case OpponentSlot.TopLeft:
                        board.OpponentTopLeft = placement.Value;
                        break;
                    case OpponentSlot.Top:
                        board.OpponentTop = placement.Value;
                        break;
                    case OpponentSlot.TopRight:
                        board.OpponentTopRight = placement.Value;
                        break;
                    default:
                        board.OpponentRight = placement.Value;
                        break;
                }
            }
        }
    }
}
