using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Builds private player views and maps engine cards, phases and rejections to the contract. Copies values without
    /// keeping references to MatchState or its lists.
    /// </summary>
    internal static class MatchViewHelper
    {
        private const int NoUserId = 0;

        private static readonly Dictionary<CardKind, MatchCardKind> _contractCardsByEngineCard =
            new Dictionary<CardKind, MatchCardKind>
            {
                { CardKind.Safe, MatchCardKind.Safe },
                { CardKind.FatalCat, MatchCardKind.FatalCat }
            };

        private static readonly Dictionary<MatchCardKind, CardKind> _engineCardsByContractCard =
            new Dictionary<MatchCardKind, CardKind>
            {
                { MatchCardKind.Safe, CardKind.Safe },
                { MatchCardKind.FatalCat, CardKind.FatalCat }
            };

        private static readonly Dictionary<RoundPhase, MatchRoundPhase> _contractPhasesByEnginePhase =
            new Dictionary<RoundPhase, MatchRoundPhase>
            {
                { RoundPhase.PlacingCards, MatchRoundPhase.PlacingCards },
                { RoundPhase.Betting, MatchRoundPhase.Betting },
                { RoundPhase.ChallengeReady, MatchRoundPhase.ChallengeReady },
                { RoundPhase.PunishmentPending, MatchRoundPhase.PunishmentPending },
                { RoundPhase.MatchFinished, MatchRoundPhase.MatchFinished }
            };

        private static readonly Dictionary<BetActionStatus, string> _errorKeysByBetStatus =
            new Dictionary<BetActionStatus, string>
            {
                { BetActionStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { BetActionStatus.NotPlayersTurn, MatchErrorKeys.NotYourTurn },
                { BetActionStatus.BetInProgress, MatchErrorKeys.BetInProgress },
                { BetActionStatus.NoBetInProgress, MatchErrorKeys.NoBetInProgress },
                { BetActionStatus.TableEmpty, MatchErrorKeys.TableEmpty },
                { BetActionStatus.EmptyHandRequiresBetOfOne, MatchErrorKeys.EmptyHandBet },
                { BetActionStatus.BetValueNotValid, MatchErrorKeys.BetValueNotValid },
                { BetActionStatus.NoCardPlacedInRound, MatchErrorKeys.NoCardPlaced },
                { BetActionStatus.PunishmentPending, MatchErrorKeys.PunishmentPending },
                { BetActionStatus.MatchFinished, MatchErrorKeys.MatchFinished }
            };

        private static readonly HashSet<ChallengeRevealStatus> _acceptedRevealStatuses =
            new HashSet<ChallengeRevealStatus>
            {
                ChallengeRevealStatus.Revealed,
                ChallengeRevealStatus.ChallengeSucceeded,
                ChallengeRevealStatus.ChallengeFailed
            };

        private static readonly Dictionary<ChallengeRevealStatus, string> _errorKeysByRevealStatus =
            new Dictionary<ChallengeRevealStatus, string>
            {
                { ChallengeRevealStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { ChallengeRevealStatus.NoChallengeInProgress, MatchErrorKeys.NoChallenge },
                { ChallengeRevealStatus.NotTheBettor, MatchErrorKeys.NotTheBettor },
                { ChallengeRevealStatus.PileOwnerNotInMatch, MatchErrorKeys.PileOwnerNotInMatch },
                { ChallengeRevealStatus.OwnPileFirst, MatchErrorKeys.OwnPileFirst },
                { ChallengeRevealStatus.PileFullyRevealed, MatchErrorKeys.PileEmpty }
            };

        private static readonly Dictionary<PunishmentStatus, string> _errorKeysByPunishmentStatus =
            new Dictionary<PunishmentStatus, string>
            {
                { PunishmentStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { PunishmentStatus.NoPunishmentPending, MatchErrorKeys.NoPunishmentPending },
                { PunishmentStatus.NotTheChooser, MatchErrorKeys.NotTheChooser },
                { PunishmentStatus.CardIndexNotValid, MatchErrorKeys.CardIndexNotValid }
            };

        private static readonly Dictionary<CardPlacementStatus, string> _errorKeysByPlacementStatus =
            new Dictionary<CardPlacementStatus, string>
            {
                { CardPlacementStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { CardPlacementStatus.BetInProgress, MatchErrorKeys.BetInProgress },
                { CardPlacementStatus.NotPlayersTurn, MatchErrorKeys.NotYourTurn },
                { CardPlacementStatus.HandEmpty, MatchErrorKeys.HandEmpty },
                { CardPlacementStatus.CardNotInHand, MatchErrorKeys.CardNotInHand },
                { CardPlacementStatus.PunishmentPending, MatchErrorKeys.PunishmentPending },
                { CardPlacementStatus.MatchFinished, MatchErrorKeys.MatchFinished }
            };

        /// <summary>
        /// Builds public state plus only the recipient's hand, pile and available actions. The caller must hold
        /// _sessionsLock while reading MatchState. Other players' hidden card types stay private (Rule No. 2).
        /// </summary>
        /// <param name="context">Engine state and match details stored separately by the registry.</param>
        /// <param name="viewerUserId">Player receiving the view.</param>
        /// <returns>New view without engine references.</returns>
        internal static MatchView CreateMatchView(MatchViewContext context, int viewerUserId)
        {
            MatchState matchState = context.MatchState;
            MatchPlayer viewer = matchState.Players.FirstOrDefault(player => player.PlayerId == viewerUserId);
            MatchView matchView = new MatchView
            {
                MatchId = context.MatchId,
                Version = context.Version,
                RoundNumber = matchState.RoundNumber,
                Phase = ConvertToContractPhase(matchState.Phase),
                PlayerInTurnUserId = FindPlayerInTurnUserId(matchState),
                Players = CreatePlayerViews(matchState, context.NicknamesByUserId),
                OwnHandCards = ConvertToContractCards(viewer?.HandCards),
                OwnPileCards = ConvertToContractCards(viewer?.PileCards),
                CurrentBet = matchState.CurrentBet,
                BetLeaderUserId = matchState.BetLeader?.PlayerId ?? NoUserId,
                ChallengesToWin = matchState.ModeRules.SuccessfulChallengesToWin,
                WinnerUserId = matchState.Winner?.PlayerId ?? NoUserId,
                FinalStandings = CreateFinalStandings(context),
                CardsOnTable = matchState.CardsOnTable,
                Challenge = CreateChallengeView(context.LastChallenge),
                Punishment = CreatePunishmentView(matchState)
            };
            MatchActionAvailabilityHelper.ApplyAvailability(matchView, matchState, viewer);

            return matchView;
        }

        /// <summary>Maps an engine phase to the contract phase.</summary>
        /// <param name="enginePhase">Engine phase.</param>
        /// <returns>Equivalent contract phase.</returns>
        internal static MatchRoundPhase ConvertToContractPhase(RoundPhase enginePhase)
        {
            MatchRoundPhase contractPhase = _contractPhasesByEnginePhase[enginePhase];

            return contractPhase;
        }

        /// <summary>Maps an engine card to the contract card type.</summary>
        /// <param name="engineCard">Engine card.</param>
        /// <returns>Equivalent contract card.</returns>
        internal static MatchCardKind ConvertToContractCard(CardKind engineCard)
        {
            MatchCardKind contractCard = _contractCardsByEngineCard[engineCard];

            return contractCard;
        }

        /// <summary>Maps a client card type to its engine type.</summary>
        /// <param name="contractCard">Received value, possibly outside the enum.</param>
        /// <param name="engineCard">Engine type, valid only on success.</param>
        /// <returns>True if the type exists.</returns>
        internal static bool TryConvertToEngineCard(MatchCardKind contractCard, out CardKind engineCard)
        {
            bool isValid = _engineCardsByContractCard.TryGetValue(contractCard, out engineCard);

            return isValid;
        }

        /// <summary>Maps the engine result for opening a bid, raising or folding.</summary>
        /// <param name="betStatus">Engine result.</param>
        /// <returns>Null if applied, otherwise the error key.</returns>
        internal static string ConvertToErrorKey(BetActionStatus betStatus)
        {
            string errorKey;

            if (betStatus == BetActionStatus.Accepted)
            {
                errorKey = null;
            }
            else if (!_errorKeysByBetStatus.TryGetValue(betStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        /// <summary>Maps the engine result for revealing a challenge card.</summary>
        /// <param name="revealStatus">Engine result.</param>
        /// <returns>Null if a card was revealed, even if the challenge ended. Otherwise the error key.</returns>
        internal static string ConvertToErrorKey(ChallengeRevealStatus revealStatus)
        {
            string errorKey;

            if (_acceptedRevealStatuses.Contains(revealStatus))
            {
                errorKey = null;
            }
            else if (!_errorKeysByRevealStatus.TryGetValue(revealStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        /// <summary>Maps the engine result for a punishment choice.</summary>
        /// <param name="punishmentStatus">Engine result.</param>
        /// <returns>Null if the card was removed, otherwise the error key.</returns>
        internal static string ConvertToErrorKey(PunishmentStatus punishmentStatus)
        {
            string errorKey;

            if (punishmentStatus == PunishmentStatus.CardRemoved)
            {
                errorKey = null;
            }
            else if (!_errorKeysByPunishmentStatus.TryGetValue(punishmentStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        /// <summary>Maps the engine result for placing a card.</summary>
        /// <param name="placementStatus">Engine result.</param>
        /// <returns>Null if placed, otherwise the error key.</returns>
        internal static string ConvertToErrorKey(CardPlacementStatus placementStatus)
        {
            string errorKey;

            if (placementStatus == CardPlacementStatus.Placed)
            {
                errorKey = null;
            }
            else if (!_errorKeysByPlacementStatus.TryGetValue(placementStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        /// <summary>
        /// Builds final standings after the match ends. Guests retain their position and receive zero points.
        /// Guest identities are supplied by the server, never by the client.
        /// </summary>
        private static List<MatchStandingView> CreateFinalStandings(MatchViewContext context)
        {
            List<MatchStandingView> standingViews = null;

            if (context.MatchState.Phase == RoundPhase.MatchFinished && context.GuestUserIds != null)
            {
                FinalStandings finalStandings =
                    FinalStandings.CalculateFromMatch(context.MatchState, context.GuestUserIds);

                if (finalStandings.Status == FinalStandingsStatus.Calculated)
                {
                    standingViews = ConvertToStandingViews(finalStandings, context.NicknamesByUserId);
                }
            }

            return standingViews;
        }

        private static List<MatchStandingView> ConvertToStandingViews(
            FinalStandings finalStandings,
            IReadOnlyDictionary<int, string> nicknamesByUserId)
        {
            List<MatchStandingView> standingViews = new List<MatchStandingView>();

            foreach (PlayerStanding standing in finalStandings.Standings)
            {
                string nickname;

                if (!nicknamesByUserId.TryGetValue(standing.PlayerId, out nickname))
                {
                    nickname = string.Empty;
                }

                standingViews.Add(new MatchStandingView
                {
                    UserId = standing.PlayerId,
                    Nickname = nickname,
                    Position = standing.Position,
                    Points = standing.Points
                });
            }

            return standingViews;
        }

        private static List<MatchPlayerView> CreatePlayerViews(
            MatchState matchState,
            IReadOnlyDictionary<int, string> nicknamesByUserId)
        {
            List<MatchPlayerView> playerViews = new List<MatchPlayerView>();

            foreach (MatchPlayer player in matchState.Players)
            {
                playerViews.Add(new MatchPlayerView
                {
                    UserId = player.PlayerId,
                    Nickname = nicknamesByUserId[player.PlayerId],
                    HandCardCount = player.HandCards.Count,
                    PileCardCount = player.PileCards.Count,
                    IsEliminated = player.IsEliminated,
                    SuccessfulChallengeCount = player.SuccessfulChallengeCount,
                    HasWithdrawnFromBet = player.HasWithdrawnFromBet,
                    HasLeftMatch = player.HasLeftMatch,
                    RevealedPileCards = ConvertToContractCards(FindRevealedPileCards(player))
                });
            }

            return playerViews;
        }

        /// <summary>
        /// Player shown as active in the UI. Shows the current player, the chooser during punishment, or nobody after
        /// the match ends.
        /// </summary>
        private static int FindPlayerInTurnUserId(MatchState matchState)
        {
            int playerInTurnUserId = matchState.PlayerInTurn.PlayerId;

            if (matchState.Phase == RoundPhase.PunishmentPending)
            {
                playerInTurnUserId = matchState.PendingPunishment.ChooserPlayerId;
            }
            else if (matchState.Phase == RoundPhase.MatchFinished)
            {
                playerInTurnUserId = NoUserId;
            }

            return playerInTurnUserId;
        }

        /// <summary>Revealed cards in a player's pile from top to bottom, visible to everyone.</summary>
        private static IReadOnlyList<CardKind> FindRevealedPileCards(MatchPlayer player)
        {
            IReadOnlyList<CardKind> revealedCards = player.PileCards
                .Reverse()
                .Take(player.RevealedCardCount)
                .ToList()
                .AsReadOnly();

            return revealedCards;
        }

        private static MatchChallengeView CreateChallengeView(MatchChallengeRecord challengeRecord)
        {
            MatchChallengeView challengeView = null;

            if (challengeRecord != null)
            {
                challengeView = new MatchChallengeView
                {
                    BettorUserId = challengeRecord.BettorUserId,
                    PromisedCount = challengeRecord.PromisedCount,
                    Outcome = challengeRecord.Outcome,
                    RevealedCards = challengeRecord.RevealedCards
                        .Select(card => new MatchRevealedCardView
                        {
                            PileOwnerUserId = card.PileOwnerUserId,
                            Card = card.Card
                        })
                        .ToList(),
                    PunishedUserId = challengeRecord.PunishedUserId,
                    PunishmentChooserUserId = challengeRecord.PunishmentChooserUserId,
                    PunishmentResolution = challengeRecord.PunishmentResolution
                };
            }

            return challengeView;
        }

        /// <summary>
        /// Pending punishment with the chooser, punished player and number of choices. Contains no cards.
        /// </summary>
        private static MatchPunishmentView CreatePunishmentView(MatchState matchState)
        {
            MatchPunishmentView punishmentView = null;
            PendingPunishment pendingPunishment = matchState.PendingPunishment;

            if (pendingPunishment != null)
            {
                MatchPlayer punishedPlayer = matchState.Players
                    .First(player => player.PlayerId == pendingPunishment.PunishedPlayerId);
                punishmentView = new MatchPunishmentView
                {
                    ChooserUserId = pendingPunishment.ChooserPlayerId,
                    PunishedUserId = pendingPunishment.PunishedPlayerId,
                    IsBlindChoice = pendingPunishment.IsBlindChoice,
                    CardSlotCount = punishedPlayer.HandCards.Count
                };
            }

            return punishmentView;
        }

        private static List<MatchCardKind> ConvertToContractCards(IReadOnlyList<CardKind> engineCards)
        {
            List<MatchCardKind> contractCards = new List<MatchCardKind>();

            if (engineCards != null)
            {
                contractCards.AddRange(engineCards.Select(ConvertToContractCard));
            }

            return contractCards;
        }
    }
}
