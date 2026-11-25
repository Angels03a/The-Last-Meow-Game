using System;
using System.Collections.Generic;

using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Presentation
{
    public static class MatchBoardMapper
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
        private const int NoLocalPlayerIndex = -1;
        private const int FirstIndex = 0;
        private const int IndexOffset = 1;
        private const int NoCards = 0;

        private static readonly Dictionary<MatchRoundPhase, string> _labelKeysByPhase =
            new Dictionary<MatchRoundPhase, string>
            {
                { MatchRoundPhase.PlacingCards, PlacingCardsLabelKey },
                { MatchRoundPhase.Betting, BettingLabelKey },
                { MatchRoundPhase.ChallengeReady, ChallengeReadyLabelKey },
                { MatchRoundPhase.PunishmentPending, PunishmentPendingLabelKey },
                { MatchRoundPhase.MatchFinished, MatchFinishedLabelKey }
            };

        private static readonly OpponentSlot[][] _opponentLayouts =
        {
            new OpponentSlot[] { OpponentSlot.Top },
            new OpponentSlot[] { OpponentSlot.Left, OpponentSlot.Right },
            new OpponentSlot[] { OpponentSlot.Left, OpponentSlot.Top, OpponentSlot.Right },
            new OpponentSlot[] { OpponentSlot.Left, OpponentSlot.TopLeft, OpponentSlot.TopRight, OpponentSlot.Right },
            new OpponentSlot[]
            {
                OpponentSlot.Left, OpponentSlot.TopLeft, OpponentSlot.Top, OpponentSlot.TopRight, OpponentSlot.Right
            }
        };

        /// <summary>Provides an empty board until the server supplies a match view.</summary>
        public static MatchBoard CreateEmptyBoard()
        {
            MatchBoard matchBoard = new MatchBoard
            {
                PhaseLabelKey = string.Empty,
                Players = new List<MatchPlayerEntry>().AsReadOnly(),
                HandCards = new List<HandCardEntry>().AsReadOnly(),
                OwnPileCards = new List<PileCardEntry>().AsReadOnly()
            };

            return matchBoard;
        }

        public static MatchBoard CreateBoard(MatchView matchView, int localUserId, int cardsPerPlayer)
        {
            MatchBoard matchBoard = CreateEmptyBoard();

            if (matchView != null && matchView.Players != null)
            {
                List<MatchPlayerEntry> matchPlayers = CreatePlayerEntries(matchView, cardsPerPlayer);

                matchBoard.HasMatchView = true;
                matchBoard.RoundNumber = matchView.RoundNumber;
                matchBoard.PhaseLabelKey = FindPhaseLabelKey(matchView.Phase);
                matchBoard.IsLocalPlayerTurn = matchView.PlayerInTurnUserId == localUserId;
                matchBoard.CanPlaceCards = matchView.CanPlaceCard;
                matchBoard.CanPlaceBet = matchView.CanPlaceBet;
                matchBoard.CanRevealCard = matchView.CanRevealCard;
                matchBoard.CanWithdrawFromBet = matchView.CanWithdrawFromBet;
                matchBoard.CurrentBet = matchView.CurrentBet;
                matchBoard.MinimumBetValue = matchView.MinimumBetValue;
                matchBoard.MaximumBetValue = matchView.MaximumBetValue;
                matchBoard.BetLeaderName = string.Empty;
                matchBoard.Players = matchPlayers.AsReadOnly();

                foreach (MatchPlayerEntry matchPlayer in matchPlayers)
                {
                    if (matchPlayer.UserId == matchView.BetLeaderUserId)
                    {
                        matchBoard.BetLeaderName = matchPlayer.Nickname ?? string.Empty;
                    }

                    if (matchPlayer.UserId == localUserId)
                    {
                        matchBoard.LocalPlayer = matchPlayer;
                        matchBoard.OwnPileCards = matchPlayer.PileCards;
                    }
                }

                matchBoard.HandCards = CreateHandCards(matchView.OwnHandCards);

                matchBoard.HasChallenge = matchView.Challenge != null;

                if (matchBoard.HasChallenge)
                {
                    matchBoard.RevealedCount = matchView.Challenge.RevealedCards?.Length ?? NoCards;
                    matchBoard.PromisedCount = matchView.Challenge.PromisedCount;
                    matchBoard.ChallengeOutcome = matchView.Challenge.Outcome;
                }

                ApplyPunishment(matchBoard, matchView);
                AssignOpponents(matchBoard, matchPlayers, localUserId);
            }

            return matchBoard;
        }

        public static MatchCardKind ConvertToServerCard(HandCardKind handCardKind)
        {
            MatchCardKind serverCardKind = MatchCardKind.Safe;

            if (handCardKind == HandCardKind.Skull)
            {
                serverCardKind = MatchCardKind.FatalCat;
            }

            return serverCardKind;
        }

        public static string FindPhaseLabelKey(MatchRoundPhase roundPhase)
        {
            string phaseLabelKey;

            if (!_labelKeysByPhase.TryGetValue(roundPhase, out phaseLabelKey))
            {
                phaseLabelKey = UnknownPhaseLabelKey;
            }

            return phaseLabelKey;
        }

        public static IReadOnlyList<HandCardEntry> CreateHandCards(IEnumerable<MatchCardKind> ownHandCards)
        {
            List<HandCardEntry> handCards = new List<HandCardEntry>();
            bool isNextSafeCardEgyptian = true;

            foreach (MatchCardKind cardKind in ownHandCards ?? Array.Empty<MatchCardKind>())
            {
                HandCardKind handCardKind = HandCardKind.Skull;

                if (cardKind == MatchCardKind.Safe)
                {
                    handCardKind = isNextSafeCardEgyptian ? HandCardKind.Egyptian : HandCardKind.Zombie;
                    isNextSafeCardEgyptian = !isNextSafeCardEgyptian;
                }

                handCards.Add(new HandCardEntry { Kind = handCardKind });
            }

            IReadOnlyList<HandCardEntry> displayedHandCards = handCards.AsReadOnly();

            return displayedHandCards;
        }

        public static IReadOnlyList<KeyValuePair<OpponentSlot, MatchPlayerEntry>> ArrangeOpponents(
            IReadOnlyList<MatchPlayerEntry> players, int localUserId)
        {
            int localPlayerIndex = NoLocalPlayerIndex;
            List<KeyValuePair<OpponentSlot, MatchPlayerEntry>> opponentPlacements =
                new List<KeyValuePair<OpponentSlot, MatchPlayerEntry>>();

            for (int playerIndex = FirstIndex;
                playerIndex < players.Count && localPlayerIndex == NoLocalPlayerIndex;
                playerIndex++)
            {
                if (players[playerIndex].UserId == localUserId)
                {
                    localPlayerIndex = playerIndex;
                }
            }

            int opponentCount = players.Count - IndexOffset;

            if (localPlayerIndex != NoLocalPlayerIndex && opponentCount > FirstIndex
                && opponentCount <= _opponentLayouts.Length)
            {
                OpponentSlot[] opponentLayout = _opponentLayouts[opponentCount - IndexOffset];

                for (int opponentIndex = FirstIndex; opponentIndex < opponentCount; opponentIndex++)
                {
                    int playerIndex = (localPlayerIndex + opponentIndex + IndexOffset) % players.Count;
                    opponentPlacements.Add(new KeyValuePair<OpponentSlot, MatchPlayerEntry>(
                        opponentLayout[opponentIndex], players[playerIndex]));
                }
            }

            IReadOnlyList<KeyValuePair<OpponentSlot, MatchPlayerEntry>> displayedOpponents =
                opponentPlacements.AsReadOnly();

            return displayedOpponents;
        }

        private static List<MatchPlayerEntry> CreatePlayerEntries(MatchView matchView, int cardsPerPlayer)
        {
            List<MatchPlayerEntry> matchPlayers = new List<MatchPlayerEntry>();

            foreach (MatchPlayerView playerView in matchView.Players)
            {
                MatchCardKind[] revealedCards = playerView.RevealedPileCards ?? Array.Empty<MatchCardKind>();
                int hiddenCardCount = Math.Max(NoCards, playerView.PileCardCount - revealedCards.Length);
                List<PileCardEntry> pileCards = new List<PileCardEntry>();

                for (int cardIndex = FirstIndex; cardIndex < hiddenCardCount; cardIndex++)
                {
                    pileCards.Add(new PileCardEntry { Symbol = FaceDownCardSymbol });
                }

                foreach (MatchCardKind revealedCard in revealedCards)
                {
                    string cardSymbol = revealedCard == MatchCardKind.FatalCat ? FatalCatSymbol : SafeCardSymbol;
                    pileCards.Add(new PileCardEntry { Symbol = cardSymbol });
                }

                matchPlayers.Add(new MatchPlayerEntry
                {
                    UserId = playerView.UserId,
                    Nickname = DisplayNameHelper.Resolve(playerView.UserId, playerView.Nickname),
                    HandCardCount = playerView.HandCardCount,
                    PileCardCount = playerView.PileCardCount,
                    HiddenPileCardCount = hiddenCardCount,
                    LivesCount = playerView.HandCardCount + playerView.PileCardCount,
                    MaximumLivesCount = cardsPerPlayer,
                    IsEliminated = playerView.IsEliminated,
                    IsInTurn = playerView.UserId == matchView.PlayerInTurnUserId,
                    PileCards = pileCards.AsReadOnly()
                });
            }

            return matchPlayers;
        }

        private static void ApplyPunishment(MatchBoard matchBoard, MatchView matchView)
        {
            if (matchView.Punishment != null)
            {
                matchBoard.HasPunishment = true;
                matchBoard.IsBlindPunishment = matchView.Punishment.IsBlindChoice;
                matchBoard.CanChoosePunishment = matchView.CanChoosePunishmentCard;
                matchBoard.PunishmentChooserName = string.Empty;
                matchBoard.PunishedPlayerName = string.Empty;

                foreach (MatchPlayerEntry matchPlayer in matchBoard.Players)
                {
                    string playerNickname = matchPlayer.Nickname ?? string.Empty;

                    if (matchPlayer.UserId == matchView.Punishment.ChooserUserId)
                    {
                        matchBoard.PunishmentChooserName = playerNickname;
                    }

                    if (matchPlayer.UserId == matchView.Punishment.PunishedUserId)
                    {
                        matchBoard.PunishedPlayerName = playerNickname;
                    }
                }

                if (matchBoard.CanChoosePunishment)
                {
                    List<HandCardEntry> punishmentCards = new List<HandCardEntry>();
                    int punishmentCardCount = matchBoard.IsBlindPunishment
                        ? matchView.Punishment.CardSlotCount : matchBoard.HandCards.Count;

                    for (int cardIndex = FirstIndex; cardIndex < punishmentCardCount; cardIndex++)
                    {
                        HandCardEntry punishmentCard = new HandCardEntry { Index = cardIndex };

                        if (matchBoard.IsBlindPunishment)
                        {
                            punishmentCard.IsBlind = true;
                        }
                        else
                        {
                            punishmentCard.Kind = matchBoard.HandCards[cardIndex].Kind;
                        }

                        punishmentCards.Add(punishmentCard);
                    }

                    matchBoard.PunishmentCards = punishmentCards.AsReadOnly();
                }
            }
        }

        private static void AssignOpponents(MatchBoard matchBoard, IReadOnlyList<MatchPlayerEntry> players,
            int localUserId)
        {
            foreach (KeyValuePair<OpponentSlot, MatchPlayerEntry> opponentPlacement in ArrangeOpponents(players, localUserId))
            {
                switch (opponentPlacement.Key)
                {
                    case OpponentSlot.Left:
                        matchBoard.OpponentLeft = opponentPlacement.Value;
                        break;
                    case OpponentSlot.TopLeft:
                        matchBoard.OpponentTopLeft = opponentPlacement.Value;
                        break;
                    case OpponentSlot.Top:
                        matchBoard.OpponentTop = opponentPlacement.Value;
                        break;
                    case OpponentSlot.TopRight:
                        matchBoard.OpponentTopRight = opponentPlacement.Value;
                        break;
                    default:
                        matchBoard.OpponentRight = opponentPlacement.Value;
                        break;
                }
            }
        }
    }
}
