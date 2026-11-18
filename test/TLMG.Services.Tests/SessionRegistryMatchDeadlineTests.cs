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
    /// Pruebas del plazo del castigo con el reloj inyectado (D-54): el servidor no usa temporizadores, así que el
    /// plazo vencido se aplica al llegar la siguiente operación. Sin hilos ni esperas.
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

        /// <summary>Verifica SubscribeToMatch_JustBeforeTheLimit_KeepsThePunishmentPending.</summary>
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

        /// <summary>Verifica SubscribeToMatch_AtTheLimitWithBlindPunishment_ResolvesAtRandomWithTheFatalCat.</summary>
        [Fact]
        public void SubscribeToMatch_AtTheLimitWithBlindPunishment_ResolvesAtRandomWithTheFatalCat()
        {
            // Arrange: el azar elige el índice 0 de la mano del castigado, que es su Gato.
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

        /// <summary>Verifica SubscribeToMatch_AtTheLimitWithOwnPunishment_ResolvesAtRandomWithTheFatalCat.</summary>
        [Fact]
        public void SubscribeToMatch_AtTheLimitWithOwnPunishment_ResolvesAtRandomWithTheFatalCat()
        {
            // Arrange: la mano del apostador es [Safe, Safe, Safe, Gato] y el azar elige el último índice.
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

        /// <summary>Verifica ChoosePunishmentCard_AfterTheLimit_IsRejectedAfterApplyingTheRandomChoice.</summary>
        [Fact]
        public void ChoosePunishmentCard_AfterTheLimit_IsRejectedAfterApplyingTheRandomChoice()
        {
            // Arrange: el lugar 0 de quien elige sería una carta segura; el azar elimina el Gato.
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

        /// <summary>Verifica ChoosePunishmentCard_JustBeforeTheLimit_IsAccepted.</summary>
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

        /// <summary>Verifica PlaceCard_AfterTheLimit_AppliesTheExpiryAndThenTheRequestedAction.</summary>
        [Fact]
        public void PlaceCard_AfterTheLimit_AppliesTheExpiryAndThenTheRequestedAction()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[2]);
            scenario.Clock.Advance(_punishmentLimit);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.UserIds[0], MatchCardKind.Safe);

            // Assert: el plazo vencido y la carta nueva suben la versión una sola vez.
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(versionBefore + 1, punishedView.Version);
            Assert.Equal(1, punishedView.OwnPileCards.Count);
            Assert.Null(punishedView.Challenge);
        }

        /// <summary>Verifica LeaveRoom_AfterTheLimit_AppliesTheExpiryBeforeTheExit.</summary>
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

        /// <summary>Verifica SubscribeToMatch_WithoutAPunishmentAfterALongTime_ChangesNothing.</summary>
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
