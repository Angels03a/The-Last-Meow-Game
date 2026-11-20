using System;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Tests MatchService bidding results for the requester and views for each player.</summary>
    public class MatchServiceBettingTests
    {
        private const int MinimumPlayerCount = 3;
        private const int TwoCards = 2;
        private const int OneCard = 1;

        /// <summary>Checks PlaceBet_ByThePlayerInTurn_DeliversOneViewPerSubscribedCallback.</summary>
        [Fact]
        public void PlaceBet_ByThePlayerInTurn_DeliversOneViewPerSubscribedCallback()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            MatchService service = new MatchService(scenario.Registry);
            int viewsBefore = scenario.CallbackOf(scenario.UserIds[1]).Views.Count;

            // Act
            ResultWithNotification<OperationResult> result =
                service.PlaceBet(scenario.MatchCallerOf(scenario.UserIds[0]), TwoCards);
            RunNotifications(result);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(MinimumPlayerCount, result.Notifications.Count);
            Assert.Equal(viewsBefore + 1, scenario.CallbackOf(scenario.UserIds[1]).Views.Count);
            Assert.Equal(TwoCards, scenario.CallbackOf(scenario.UserIds[1]).LastView.CurrentBet);
        }

        /// <summary>Checks PlaceBet_OutOfTurn_ReturnsTheErrorKeyAndNotifiesNobody.</summary>
        [Fact]
        public void PlaceBet_OutOfTurn_ReturnsTheErrorKeyAndNotifiesNobody()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            MatchService service = new MatchService(scenario.Registry);

            // Act
            ResultWithNotification<OperationResult> result =
                service.PlaceBet(scenario.MatchCallerOf(scenario.UserIds[1]), OneCard);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(MatchErrorKeys.NotYourTurn, result.OperationResult.ErrorKey);
            Assert.Empty(result.Notifications);
        }

        /// <summary>Checks WithdrawFromBet_AfterABetIsOpen_AcceptsAndNotifiesEveryone.</summary>
        [Fact]
        public void WithdrawFromBet_AfterABetIsOpen_AcceptsAndNotifiesEveryone()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            MatchService service = new MatchService(scenario.Registry);
            scenario.PlaceBet(scenario.UserIds[0], OneCard);

            // Act
            ResultWithNotification<OperationResult> result =
                service.WithdrawFromBet(scenario.MatchCallerOf(scenario.UserIds[1]));
            RunNotifications(result);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(MinimumPlayerCount, result.Notifications.Count);
        }

        /// <summary>Checks WithdrawFromBet_WithoutABet_ReturnsTheErrorKey.</summary>
        [Fact]
        public void WithdrawFromBet_WithoutABet_ReturnsTheErrorKey()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            MatchService service = new MatchService(scenario.Registry);

            // Act
            ResultWithNotification<OperationResult> result =
                service.WithdrawFromBet(scenario.MatchCallerOf(scenario.UserIds[0]));

            // Assert
            Assert.Equal(MatchErrorKeys.NoBetInProgress, result.OperationResult.ErrorKey);
        }

        private static MatchPlayScenario StartWithOneCardEach()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.PlaceOneSafeCardEach();

            return scenario;
        }

        private static void RunNotifications(ResultWithNotification<OperationResult> result)
        {
            Action[] notifications = result.Notifications.ToArray();
            Array.ForEach(notifications, notification => notification());
        }
    }
}
