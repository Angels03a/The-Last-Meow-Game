using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests reveal order, the challenge outcome kept by the registry and public views of revealed cards.
    /// </summary>
    public class SessionRegistryMatchChallengeTests
    {
        private const int MinimumPlayerCount = 3;
        private const int PromisedTwoCards = 2;
        private const int SecondRound = 2;
        private const int CardsPerPlayer = MatchPlayScenario.CardsPerPlayerCount;
        private const int UnknownUserId = 99;
        private const int NoUserId = 0;

        /// <summary>Checks RevealCard_OwnPileWithABetOfOne_SucceedsAndStartsTheNextRound.</summary>
        [Fact]
        public void RevealCard_OwnPileWithABetOfOne_SucceedsAndStartsTheNextRound()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, 1);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Assert
            MatchView view = delivery.ViewDeliveries[0].MatchView;
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(SecondRound, view.RoundNumber);
            Assert.Equal(MatchRoundPhase.PlacingCards, view.Phase);
            Assert.Equal(scenario.UserIds[0], view.PlayerInTurnUserId);
            Assert.Equal(1, view.Players.Single(player => player.UserId == scenario.UserIds[0])
                .SuccessfulChallengeCount);
            Assert.Equal(MatchChallengeOutcome.Succeeded, view.Challenge.Outcome);
            Assert.Equal(CardsPerPlayer, view.Players.Single(player => player.UserId == scenario.UserIds[0])
                .HandCardCount);
        }

        /// <summary>Checks RevealCard_SecondSuccessfulChallenge_FinishesTheMatchWithTheWinner.</summary>
        [Fact]
        public void RevealCard_SecondSuccessfulChallenge_FinishesTheMatchWithTheWinner()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.WinRoundWithABetOfOne();
            scenario.ReachChallengeReady(MatchCardKind.Safe, 1);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Assert
            Assert.All(
                delivery.ViewDeliveries,
                item => Assert.Equal(MatchRoundPhase.MatchFinished, item.MatchView.Phase));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(scenario.UserIds[0], item.MatchView.WinnerUserId));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(NoUserId, item.MatchView.PlayerInTurnUserId));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(
                MatchChallengeOutcome.Succeeded, item.MatchView.Challenge.Outcome));
            Assert.All(delivery.ViewDeliveries, item => Assert.False(item.MatchView.CanPlaceCard));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(3, item.MatchView.FinalStandings.Count));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(scenario.UserIds[0],
                item.MatchView.FinalStandings[0].UserId));
        }

        /// <summary>Checks RevealCard_PartialReveal_ShowsTheRevealedCardToEveryoneAndKeepsTheChallengeOpen.</summary>
        [Fact]
        public void RevealCard_PartialReveal_ShowsTheRevealedCardToEveryoneAndKeepsTheChallengeOpen()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.FatalCat, PromisedTwoCards);
            int bettorUserId = scenario.UserIds[0];

            // Act
            MatchDelivery delivery = scenario.Reveal(bettorUserId, bettorUserId);

            // Assert
            MatchView otherView = delivery.ViewDeliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(scenario.UserIds[2]))).MatchView;
            MatchPlayerView bettor = otherView.Players.Single(player => player.UserId == bettorUserId);
            MatchPlayerView second = otherView.Players.Single(player => player.UserId == scenario.UserIds[1]);
            Assert.Equal(new[] { MatchCardKind.Safe }, bettor.RevealedPileCards);
            Assert.Empty(second.RevealedPileCards);
            Assert.Equal(MatchChallengeOutcome.InProgress, otherView.Challenge.Outcome);
            Assert.Single(otherView.Challenge.RevealedCards);
            Assert.Equal(MatchRoundPhase.ChallengeReady, otherView.Phase);
            Assert.True(delivery.ViewDeliveries[0].MatchView.CanRevealCard);
        }

        /// <summary>Checks RevealCard_OtherPileAfterTheOwnOne_CompletesThePromise.</summary>
        [Fact]
        public void RevealCard_OtherPileAfterTheOwnOne_CompletesThePromise()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);
            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[1]);

            // Assert
            MatchChallengeView challenge = delivery.ViewDeliveries[0].MatchView.Challenge;
            Assert.Equal(MatchChallengeOutcome.Succeeded, challenge.Outcome);
            Assert.Equal(
                new[] { scenario.UserIds[0], scenario.UserIds[1] },
                challenge.RevealedCards.Select(card => card.PileOwnerUserId).ToArray());
        }

        /// <summary>Checks RevealCard_FatalCatOfAnotherPlayer_FailsAndOpensABlindPunishment.</summary>
        [Fact]
        public void RevealCard_FatalCatOfAnotherPlayer_FailsAndOpensABlindPunishment()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.FatalCat, PromisedTwoCards);
            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[1]);

            // Assert
            MatchView view = delivery.ViewDeliveries[2].MatchView;
            Assert.Equal(MatchRoundPhase.PunishmentPending, view.Phase);
            Assert.Equal(scenario.UserIds[1], view.Punishment.ChooserUserId);
            Assert.Equal(scenario.UserIds[0], view.Punishment.PunishedUserId);
            Assert.True(view.Punishment.IsBlindChoice);
            Assert.Equal(CardsPerPlayer, view.Punishment.CardSlotCount);
            Assert.Equal(MatchChallengeOutcome.Failed, view.Challenge.Outcome);
            Assert.Equal(MatchPunishmentResolution.Pending, view.Challenge.PunishmentResolution);
            Assert.Equal(MatchCardKind.FatalCat, view.Challenge.RevealedCards.Last().Card);
            Assert.Equal(scenario.UserIds[1], view.Challenge.RevealedCards.Last().PileOwnerUserId);
        }

        /// <summary>Checks RevealCard_OwnFatalCat_FailsAndTheBettorChoosesSeeingHisHand.</summary>
        [Fact]
        public void RevealCard_OwnFatalCat_FailsAndTheBettorChoosesSeeingHisHand()
        {
            // Arrange & Act
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachOwnPunishment();

            // Assert
            MatchView bettorView = scenario.ReadViewAs(scenario.UserIds[0]);
            Assert.False(bettorView.Punishment.IsBlindChoice);
            Assert.Equal(scenario.UserIds[0], bettorView.Punishment.ChooserUserId);
            Assert.True(bettorView.CanChoosePunishmentCard);
            Assert.Contains(MatchCardKind.FatalCat, bettorView.OwnHandCards);
        }

        /// <summary>Checks RevealCard_ByAPlayerWhoIsNotTheBettor_IsRejectedWithNotTheBettor.</summary>
        [Fact]
        public void RevealCard_ByAPlayerWhoIsNotTheBettor_IsRejectedWithNotTheBettor()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[0]);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[1], scenario.UserIds[1]);

            // Assert
            Assert.Equal(MatchErrorKeys.NotTheBettor, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.UserIds[0]));
        }

        /// <summary>Checks RevealCard_AnotherPileBeforeTheOwnOne_IsRejectedWithOwnPileFirst.</summary>
        [Fact]
        public void RevealCard_AnotherPileBeforeTheOwnOne_IsRejectedWithOwnPileFirst()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[1]);

            // Assert
            Assert.Equal(MatchErrorKeys.OwnPileFirst, delivery.ErrorKey);
        }

        /// <summary>Checks RevealCard_OnAPileWithNothingLeftToFlip_IsRejectedWithPileEmpty.</summary>
        [Fact]
        public void RevealCard_OnAPileWithNothingLeftToFlip_IsRejectedWithPileEmpty()
        {
            // Arrange: The bidder revealed their entire one-card pile and still needs two reveals in total.
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);
            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Assert
            Assert.Equal(MatchErrorKeys.PileEmpty, delivery.ErrorKey);
        }

        /// <summary>Checks RevealCard_OnAPlayerWhoIsNotInTheMatch_IsRejectedWithPileOwnerNotInMatch.</summary>
        [Fact]
        public void RevealCard_OnAPlayerWhoIsNotInTheMatch_IsRejectedWithPileOwnerNotInMatch()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);
            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], UnknownUserId);

            // Assert
            Assert.Equal(MatchErrorKeys.PileOwnerNotInMatch, delivery.ErrorKey);
        }

        /// <summary>Checks RevealCard_WithoutAChallenge_IsRejectedWithNoChallenge.</summary>
        [Fact]
        public void RevealCard_WithoutAChallenge_IsRejectedWithNoChallenge()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Assert
            Assert.Equal(MatchErrorKeys.NoChallenge, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCard_AfterASuccessfulChallenge_ClearsTheChallengeOutcome.</summary>
        [Fact]
        public void PlaceCard_AfterASuccessfulChallenge_ClearsTheChallengeOutcome()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.WinRoundWithABetOfOne();

            // Act
            MatchDelivery delivery = scenario.Place(scenario.UserIds[0], MatchCardKind.Safe);

            // Assert
            Assert.All(delivery.ViewDeliveries, item => Assert.Null(item.MatchView.Challenge));
        }

        /// <summary>Checks SubscribeToMatch_AfterASuccessfulChallenge_StillShowsTheOutcomeUntilTheNextCard.</summary>
        [Fact]
        public void SubscribeToMatch_AfterASuccessfulChallenge_StillShowsTheOutcomeUntilTheNextCard()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.WinRoundWithABetOfOne();

            // Act
            MatchView view = scenario.ReadViewAs(scenario.UserIds[2]);

            // Assert
            Assert.Equal(MatchChallengeOutcome.Succeeded, view.Challenge.Outcome);
            Assert.Equal(scenario.UserIds[0], view.Challenge.BettorUserId);
        }

        private static MatchPlayScenario NewScenario()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            return scenario;
        }
    }
}
