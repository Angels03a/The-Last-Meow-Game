using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests punishment deadlines with the supplied clock (D-54). The next operation applies an expired deadline. The
    /// server uses no timers and the tests need no threads or waits.
    /// </summary>
    public class SessionRegistryMatchDeadlineTests
    {
        private const int MinimumPlayerCount = 3;
        private const int FatalCatIndexInBlindHand = 0;
        private const int FatalCatIndexInOwnHand = 3;
        private const int LivesAfterPunishment = MatchPlayScenario.CardsPerPlayerCount - 1;
        private const int SafeCardSlot = 0;
        private const int OneMillisecond = 1;

        private static readonly TimeSpan _punishmentLimit =
            TimeSpan.FromSeconds(ValidationRules.PunishmentChoiceSeconds);

        /// <summary>Checks SubscribeToMatch_JustBeforeTheLimit_KeepsThePunishmentPending.</summary>
        [Fact]
        public void SubscribeToMatch_JustBeforeTheLimit_KeepsThePunishmentPending()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[2]);
            scenario.Clock.Advance(_punishmentLimit - TimeSpan.FromMilliseconds(OneMillisecond));

            // Act
            MatchView view = scenario.ReadViewAs(scenario.UserIds[2]);

            // Assert
            Assert.Equal(MatchRoundPhase.PunishmentPending, view.Phase);
            Assert.Equal(versionBefore, view.Version);
        }

        /// <summary>Checks SubscribeToMatch_AtTheLimitWithBlindPunishment_ResolvesAtRandomWithTheFatalCat.</summary>
        [Fact]
        public void SubscribeToMatch_AtTheLimitWithBlindPunishment_ResolvesAtRandomWithTheFatalCat()
        {
            // Arrange: The random choice picks index 0, the Fatal Cat in the punished player's hand.
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[2]);
            scenario.RandomSource.Enqueue(FatalCatIndexInBlindHand);
            scenario.Clock.Advance(_punishmentLimit);

            // Act
            MatchDelivery delivery = scenario.Subscribe(scenario.UserIds[2]);

            // Assert
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Equal(MinimumPlayerCount, delivery.ViewDeliveries.Count);
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(versionBefore + 1, item.MatchView.Version));
            Assert.Equal(MatchPunishmentResolution.ChosenAtRandom, punishedView.Challenge.PunishmentResolution);
            Assert.DoesNotContain(MatchCardKind.FatalCat, punishedView.OwnHandCards);
            Assert.Equal(LivesAfterPunishment, punishedView.OwnHandCards.Count);
            Assert.Equal(MatchRoundPhase.PlacingCards, punishedView.Phase);
        }

        /// <summary>Checks SubscribeToMatch_AtTheLimitWithOwnPunishment_ResolvesAtRandomWithTheFatalCat.</summary>
        [Fact]
        public void SubscribeToMatch_AtTheLimitWithOwnPunishment_ResolvesAtRandomWithTheFatalCat()
        {
            // Arrange: The bidder holds [Safe, Safe, Safe, FatalCat] and the random choice picks the last index.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.ReachOwnPunishment();
            scenario.RandomSource.Enqueue(FatalCatIndexInOwnHand);
            scenario.Clock.Advance(_punishmentLimit);

            // Act
            MatchDelivery delivery = scenario.Subscribe(scenario.UserIds[2]);

            // Assert
            MatchView bettorView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.DoesNotContain(MatchCardKind.FatalCat, bettorView.OwnHandCards);
            Assert.Equal(MatchPunishmentResolution.ChosenAtRandom, bettorView.Challenge.PunishmentResolution);
        }

        /// <summary>Checks ChoosePunishmentCard_AfterTheLimit_IsRejectedAfterApplyingTheRandomChoice.</summary>
        [Fact]
        public void ChoosePunishmentCard_AfterTheLimit_IsRejectedAfterApplyingTheRandomChoice()
        {
            // Arrange: The chooser's slot 0 would be a safe card. The random choice removes the Fatal Cat.
            MatchPlayScenario scenario = StartBlindPunishment();
            scenario.RandomSource.Enqueue(FatalCatIndexInBlindHand);
            scenario.Clock.Advance(_punishmentLimit + TimeSpan.FromMilliseconds(OneMillisecond));

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], SafeCardSlot);

            // Assert
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Equal(MatchErrorKeys.NoPunishmentPending, delivery.ErrorKey);
            Assert.Equal(MinimumPlayerCount, delivery.ViewDeliveries.Count);
            Assert.DoesNotContain(MatchCardKind.FatalCat, punishedView.OwnHandCards);
            Assert.Equal(MatchPunishmentResolution.ChosenAtRandom, punishedView.Challenge.PunishmentResolution);
        }

        /// <summary>Checks ChoosePunishmentCard_JustBeforeTheLimit_IsAccepted.</summary>
        [Fact]
        public void ChoosePunishmentCard_JustBeforeTheLimit_IsAccepted()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            scenario.Clock.Advance(_punishmentLimit - TimeSpan.FromMilliseconds(OneMillisecond));

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], SafeCardSlot);

            // Assert
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(MatchPunishmentResolution.ChosenByChooser, punishedView.Challenge.PunishmentResolution);
        }

        /// <summary>Checks PlaceCard_AfterTheLimit_AppliesTheExpiryAndThenTheRequestedAction.</summary>
        [Fact]
        public void PlaceCard_AfterTheLimit_AppliesTheExpiryAndThenTheRequestedAction()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[2]);
            scenario.Clock.Advance(_punishmentLimit);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.UserIds[0], MatchCardKind.Safe);

            // Assert: The expired deadline and the new card together increase the version once.
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(versionBefore + 1, punishedView.Version);
            Assert.Single(punishedView.OwnPileCards);
            Assert.Null(punishedView.Challenge);
        }

        /// <summary>Checks LeaveRoom_AfterTheLimit_AppliesTheExpiryBeforeTheExit.</summary>
        [Fact]
        public void LeaveRoom_AfterTheLimit_AppliesTheExpiryBeforeTheExit()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            scenario.Clock.Advance(_punishmentLimit);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[2]), out deliveries);

            // Assert
            MatchView chooserView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(scenario.UserIds[1]))).MatchView;
            Assert.Equal(MatchPunishmentResolution.ChosenAtRandom, chooserView.Challenge.PunishmentResolution);
            Assert.True(chooserView.Players.Single(player => player.UserId == scenario.UserIds[2]).HasLeftMatch);
        }

        /// <summary>Checks SubscribeToMatch_WithoutAPunishmentAfterALongTime_ChangesNothing.</summary>
        [Fact]
        public void SubscribeToMatch_WithoutAPunishmentAfterALongTime_ChangesNothing()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[0]);
            scenario.Clock.Advance(_punishmentLimit + _punishmentLimit);

            // Act
            int versionAfter = scenario.ReadVersionAs(scenario.UserIds[0]);

            // Assert
            Assert.Equal(versionBefore, versionAfter);
        }

        private static MatchPlayScenario StartBlindPunishment()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.ReachBlindPunishment();

            return scenario;
        }

        private static MatchView ViewOf(MatchDelivery delivery, MatchPlayScenario scenario, int userId)
        {
            return delivery.ViewDeliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(userId))).MatchView;
        }
    }
}
