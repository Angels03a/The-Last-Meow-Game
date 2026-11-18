using System;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de MatchService para revelar cartas y elegir la carta del castigo.</summary>
    public class MatchServiceChallengeTests
    {
        private const int MinimumPlayerCount = 3;
        private const int PromisedTwoCards = 2;

        /// <summary>Verifica RevealCard_ByTheBettor_DeliversTheRevealedCardToEverySubscribedCallback.</summary>
        [Fact]
        public void RevealCard_ByTheBettor_DeliversTheRevealedCardToEverySubscribedCallback()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);
            MatchService service = new MatchService(scenario.Registry);

            // Act
            ResultWithNotification<OperationResult> result =
                service.RevealCard(scenario.MatchCallerOf(scenario.UserIds[0]), scenario.UserIds[0]);
            RunNotifications(result);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(MinimumPlayerCount, result.Notifications.Count);
            Assert.Equal(1, scenario.CallbackOf(scenario.UserIds[2]).LastView.Challenge.RevealedCards.Count);
        }

        /// <summary>Verifica RevealCard_ByAnotherPlayer_ReturnsTheErrorKeyAndNotifiesNobody.</summary>
        [Fact]
        public void RevealCard_ByAnotherPlayer_ReturnsTheErrorKeyAndNotifiesNobody()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachChallengeReady(MatchCardKind.Safe, PromisedTwoCards);
            MatchService service = new MatchService(scenario.Registry);

            // Act
            ResultWithNotification<OperationResult> result =
                service.RevealCard(scenario.MatchCallerOf(scenario.UserIds[1]), scenario.UserIds[1]);

            // Assert
            Assert.Equal(MatchErrorKeys.NotTheBettor, result.OperationResult.ErrorKey);
            Assert.Empty(result.Notifications);
        }

        /// <summary>Verifica ChoosePunishmentCard_ByTheChooser_AcceptsAndNotifiesEveryone.</summary>
        [Fact]
        public void ChoosePunishmentCard_ByTheChooser_AcceptsAndNotifiesEveryone()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachBlindPunishment();
            MatchService service = new MatchService(scenario.Registry);

            // Act
            ResultWithNotification<OperationResult> result =
                service.ChoosePunishmentCard(scenario.MatchCallerOf(scenario.UserIds[1]), 0);
            RunNotifications(result);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(MinimumPlayerCount, result.Notifications.Count);
            Assert.Null(scenario.CallbackOf(scenario.UserIds[2]).LastView.Punishment);
        }

        /// <summary>Verifica ChoosePunishmentCard_ByTheWrongPlayer_ReturnsTheErrorKey.</summary>
        [Fact]
        public void ChoosePunishmentCard_ByTheWrongPlayer_ReturnsTheErrorKey()
        {
            // Arrange
            MatchPlayScenario scenario = NewScenario();
            scenario.ReachBlindPunishment();
            MatchService service = new MatchService(scenario.Registry);

            // Act
            ResultWithNotification<OperationResult> result =
                service.ChoosePunishmentCard(scenario.MatchCallerOf(scenario.UserIds[2]), 0);

            // Assert
            Assert.Equal(MatchErrorKeys.NotTheChooser, result.OperationResult.ErrorKey);
        }

        private static MatchPlayScenario NewScenario()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            return scenario;
        }

        private static void RunNotifications(ResultWithNotification<OperationResult> result)
        {
            Action[] notifications = result.Notifications.ToArray();
            Array.ForEach(notifications, notification => notification());
        }
    }
}
