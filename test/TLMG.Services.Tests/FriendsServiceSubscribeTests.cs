using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de FriendsService.SubscribeToFriends con solicitudes pendientes.</summary>
    public class FriendsServiceSubscribeTests
    {
        private const string FriendsChannelId = "friends-test";

        /// <summary>Verifica SubscribeToFriends_WithPendingRequests_ReturnsIncomingAndOutgoing.</summary>
        [Fact]
        public void SubscribeToFriends_WithPendingRequests_ReturnsIncomingAndOutgoing()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            scenario.Repository.Seed(FriendsScenario.CharlieUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            scenario.Repository.Seed(FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId,
                FriendshipState.Pending);
            scenario.Repository.Seed(FriendsScenario.CharlieUserId, FriendsScenario.AlphaUserId,
                FriendshipState.Rejected);
            FriendsCallbackStub callback = new FriendsCallbackStub();

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, callback);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(2, actual.OperationResult.PendingFriendRequests.Count);
            Assert.Single(actual.OperationResult.PendingFriendRequests,
                request => request.IsIncoming && request.UserId == FriendsScenario.CharlieUserId);
            Assert.Single(actual.OperationResult.PendingFriendRequests,
                request => !request.IsIncoming && request.UserId == FriendsScenario.AlphaUserId);
        }

        /// <summary>Verifica SubscribeToFriends_WithoutPendingRequests_ReturnsEmptyList.</summary>
        [Fact]
        public void SubscribeToFriends_WithoutPendingRequests_ReturnsEmptyList()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, new FriendsCallbackStub());

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Empty(actual.OperationResult.PendingFriendRequests);
        }

        /// <summary>Verifica SubscribeToFriends_WhenPendingLookupFails_ReturnsUnexpectedError.</summary>
        [Fact]
        public void SubscribeToFriends_WhenPendingLookupFails_ReturnsUnexpectedError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            scenario.Repository.FailPendingLookup = true;

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, new FriendsCallbackStub());

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal("Common_UnexpectedError", actual.OperationResult.ErrorKey);
            Assert.Null(scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.BravoUserId));
        }
    }
}
