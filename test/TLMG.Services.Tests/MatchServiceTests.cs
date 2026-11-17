using System;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas de MatchService: resultado para quien pidió y avisos que se entregan a cada suscrito con su vista.
    /// </summary>
    public class MatchServiceTests
    {
        private const int MinimumPlayerCount = 3;
        private const int NoViews = 0;
        private const int OneView = 1;
        private const int GuestIndex = 1;

        /// <summary>Verifica SubscribeToMatch_WithParticipant_ReturnsSuccessAndOwnViewNotification.</summary>
        [Fact]
        public void SubscribeToMatch_WithParticipant_ReturnsSuccessAndOwnViewNotification()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchService service = new MatchService(scenario.Registry);
            MatchCallbackStub callback = scenario.CallbackOf(scenario.CreatorUserId);

            // Act
            ResultWithNotification<OperationResult> result =
                service.SubscribeToMatch(scenario.MatchCallerOf(scenario.CreatorUserId), scenario.MatchId, callback);
            RunNotifications(result);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Null(result.OperationResult.ErrorKey);
            Assert.Equal(OneView, callback.Views.Count);
            Assert.Equal(scenario.MatchId, callback.LastView.MatchId);
        }

        /// <summary>Verifica SubscribeToMatch_WithInvalidToken_ReturnsErrorKeyAndNoNotifications.</summary>
        [Fact]
        public void SubscribeToMatch_WithInvalidToken_ReturnsErrorKeyAndNoNotifications()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchService service = new MatchService(scenario.Registry);
            MatchCallbackStub callback = new MatchCallbackStub();

            // Act
            ResultWithNotification<OperationResult> result = service.SubscribeToMatch(
                MatchPlayScenario.CallerWithToken("invalid"), scenario.MatchId, callback);
            RunNotifications(result);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.OperationResult.ErrorKey);
            Assert.Equal(NoViews, callback.Views.Count);
        }

        /// <summary>Verifica PlaceCard_InTurn_DeliversDifferentViewToEachSubscribedCallback.</summary>
        [Fact]
        public void PlaceCard_InTurn_DeliversDifferentViewToEachSubscribedCallback()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchService service = new MatchService(scenario.Registry);
            scenario.SubscribeAll();
            int guestUserId = scenario.UserIds[GuestIndex];
            int viewsBefore = scenario.CallbackOf(guestUserId).Views.Count;

            // Act
            ResultWithNotification<OperationResult> result =
                service.PlaceCard(scenario.MatchCallerOf(scenario.CreatorUserId), MatchCardKind.Safe);
            RunNotifications(result);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            MatchView creatorView = scenario.CallbackOf(scenario.CreatorUserId).LastView;
            MatchView guestView = scenario.CallbackOf(guestUserId).LastView;
            Assert.Equal(viewsBefore + OneView, scenario.CallbackOf(guestUserId).Views.Count);
            Assert.Equal(creatorView.Version, guestView.Version);
            Assert.NotEqual(creatorView.OwnHandCards.Count, guestView.OwnHandCards.Count);
            Assert.Empty(guestView.OwnPileCards);
        }

        /// <summary>Verifica PlaceCard_OutOfTurn_ReturnsErrorKeyAndNotifiesNobody.</summary>
        [Fact]
        public void PlaceCard_OutOfTurn_ReturnsErrorKeyAndNotifiesNobody()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchService service = new MatchService(scenario.Registry);
            scenario.SubscribeAll();
            int guestUserId = scenario.UserIds[GuestIndex];
            int viewsBefore = scenario.CallbackOf(scenario.CreatorUserId).Views.Count;

            // Act
            ResultWithNotification<OperationResult> result =
                service.PlaceCard(scenario.MatchCallerOf(guestUserId), MatchCardKind.Safe);
            RunNotifications(result);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(MatchErrorKeys.NotYourTurn, result.OperationResult.ErrorKey);
            Assert.Empty(result.Notifications);
            Assert.Equal(viewsBefore, scenario.CallbackOf(scenario.CreatorUserId).Views.Count);
        }

        /// <summary>Verifica RemoveMatchChannel_WithCurrentCallback_StopsFutureViewsForThatPlayer.</summary>
        [Fact]
        public void RemoveMatchChannel_WithCurrentCallback_StopsFutureViewsForThatPlayer()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchService service = new MatchService(scenario.Registry);
            scenario.SubscribeAll();
            int closingUserId = scenario.UserIds[GuestIndex];
            int viewsBefore = scenario.CallbackOf(closingUserId).Views.Count;
            service.RemoveMatchChannel(
                MatchPlayScenario.MatchChannelPrefix + closingUserId, scenario.CallbackOf(closingUserId));

            // Act
            ResultWithNotification<OperationResult> result =
                service.PlaceCard(scenario.MatchCallerOf(scenario.CreatorUserId), MatchCardKind.Safe);
            RunNotifications(result);

            // Assert
            Assert.Equal(viewsBefore, scenario.CallbackOf(closingUserId).Views.Count);
            Assert.Equal(MinimumPlayerCount - OneView, result.Notifications.Count);
        }

        private static void RunNotifications(ResultWithNotification<OperationResult> result)
        {
            Action[] notifications = result.Notifications.ToArray();
            Array.ForEach(notifications, notification => notification());
        }
    }
}
