using System.Collections.Generic;
using System.Linq;

using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Builds the board from the server's private view. It only copies and orders the data received. The server
    /// supplies turns, results and permissions.
    /// </summary>
    public static class MatchBoardHelper
    {
        private const string SafeCardSymbol = "✅";
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
        /// Seats for each number of opponents, starting with the next player in turn order on the local player's left.
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

        /// <summary>Creates the empty board shown before the first view arrives.</summary>
        /// <returns>Board without players or cards.</returns>
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

        /// <summary>Builds the board for a server view.</summary>
        /// <param name="matchView">
        /// Private view received. A null view or a view without players gives an empty board.
        /// </param>
        /// <param name="localUserId">
        /// Local player's account, used to find their seat and check whose turn it is.
        /// </param>
        /// <param name="cardsPerPlayer">Cards per player in this mode, equal to starting lives.</param>
        /// <returns>New board, always present.</returns>
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
                board.CanPlaceCards = matchView.CanPlaceCard;
                board.CanPlaceBet = matchView.CanPlaceBet;
                board.CanRevealCard = matchView.CanRevealCard;
                board.HasChallenge = matchView.Challenge != null;
                if (matchView.Challenge != null)
                {
                    board.RevealedCount = matchView.Challenge.RevealedCards?.Length ?? 0;
                    board.PromisedCount = matchView.Challenge.PromisedCount;
                    board.ChallengeOutcome = matchView.Challenge.Outcome;
                }
                board.CanWithdrawFromBet = matchView.CanWithdrawFromBet;
                board.CurrentBet = matchView.CurrentBet;
                board.MinimumBetValue = matchView.MinimumBetValue;
                board.MaximumBetValue = matchView.MaximumBetValue;
                board.BetLeaderName = players.FirstOrDefault(player => player.UserId == matchView.BetLeaderUserId)
                    ?.Nickname ?? string.Empty;
                board.Players = players.AsReadOnly();
                board.LocalPlayer = players.FirstOrDefault(player => player.UserId == localUserId);
                board.HandCards = CreateHandCards(matchView.OwnHandCards);
                if (matchView.Punishment != null)
                {
                    board.HasPunishment = true;
                    board.IsBlindPunishment = matchView.Punishment.IsBlindChoice;
                    board.CanChoosePunishment = matchView.CanChoosePunishmentCard;
                    board.PunishmentChooserName = players.FirstOrDefault(
                        player => player.UserId == matchView.Punishment.ChooserUserId)?.Nickname ?? string.Empty;
                    board.PunishedPlayerName = players.FirstOrDefault(
                        player => player.UserId == matchView.Punishment.PunishedUserId)?.Nickname ?? string.Empty;
                    if (board.CanChoosePunishment)
                    {
                        if (matchView.Punishment.IsBlindChoice)
                        {
                            board.PunishmentCards = Enumerable.Range(0, matchView.Punishment.CardSlotCount)
                                .Select(index => new HandCardEntry { Index = index, IsBlind = true }).ToList().AsReadOnly();
                        }
                        else
                        {
                            board.PunishmentCards = board.HandCards.Select((card, index) =>
                                new HandCardEntry { Index = index, Kind = card.Kind }).ToList().AsReadOnly();
                        }
                    }
                }
                board.OwnPileCards = board.LocalPlayer?.PileCards ?? new List<PileCardEntry>().AsReadOnly();
                AssignOpponents(board, players, localUserId);
            }

            return board;
        }

        /// <summary>Converts the displayed card type to the type used by the server.</summary>
        /// <param name="handCardKind">Appearance of the selected card.</param>
        /// <returns>FatalCat for the skull, Safe for all other cards.</returns>
        public static MatchCardKind ConvertToServerCard(HandCardKind handCardKind)
        {
            MatchCardKind serverCard = MatchCardKind.Safe;

            if (handCardKind == HandCardKind.Skull)
            {
                serverCard = MatchCardKind.FatalCat;
            }

            return serverCard;
        }

        /// <summary>Finds the resource key for a phase name.</summary>
        /// <param name="roundPhase">Phase received from the server.</param>
        /// <returns>Phase key, or a generic key for an unknown phase.</returns>
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
        /// Builds the hand in the order received. Safe cards alternate between Egyptian and zombie designs for display.
        /// The server uses only Safe and FatalCat.
        /// </summary>
        /// <param name="ownHandCards">Local player's hand, which may be null.</param>
        /// <returns>Hand cards, empty if there are none.</returns>
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
        /// Seats opponents in turn order, starting with the player after the local player. Each takes the next seat in
        /// the layout for that number of opponents.
        /// </summary>
        /// <param name="players">Players in fixed turn order.</param>
        /// <param name="localUserId">Local player's account.</param>
        /// <returns>Opponents and their seats in display order.</returns>
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
                    HiddenPileCardCount = System.Math.Max(0,
                        playerView.PileCardCount - (playerView.RevealedPileCards?.Length ?? 0)),
                    LivesCount = playerView.HandCardCount + playerView.PileCardCount,
                    MaximumLivesCount = cardsPerPlayer,
                    IsEliminated = playerView.IsEliminated,
                    IsInTurn = playerView.UserId == matchView.PlayerInTurnUserId,
                    PileCards = CreatePublicPile(playerView)
                });
            }

            return players;
        }

        private static IReadOnlyList<PileCardEntry> CreatePublicPile(MatchPlayerView player)
        {
            MatchCardKind[] revealedCards = player.RevealedPileCards ?? new MatchCardKind[0];
            int hiddenCount = System.Math.Max(0, player.PileCardCount - revealedCards.Length);
            List<PileCardEntry> pile = Enumerable.Range(0, hiddenCount)
                .Select(cardNumber => new PileCardEntry { Symbol = FaceDownCardSymbol }).ToList();
            pile.AddRange(revealedCards.Select(card => new PileCardEntry { Symbol = FindSymbol(card) }));
            return pile.AsReadOnly();
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
