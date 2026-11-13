using System;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de FriendsService.DeclineFriendRequest.</summary>
    public class FriendsServiceDeclineRequestTests
    {
        private const string NotValidKey = "Friends_RequestNotValidError";

        /// <summary>Verifica DeclineFriendRequest_ByReceiver_RejectsWithoutNotifyingTheSender.</summary>
        [Fact]
        public void DeclineFriendRequest_ByReceiver_RejectsWithoutNotifyingTheSender()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.ErrorKey);
            Assert.Equal(friendshipId, actual.OperationResult.FriendRequestId);
            Assert.Equal(FriendshipState.Rejected, scenario.Repository.Find(friendshipId).State);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.AlphaCallback.AddedFriends);
            Assert.Empty(scenario.AlphaCallback.ReceivedRequests);
            Assert.Equal(0, scenario.AlphaCallback.OtherCallCount);
        }

        /// <summary>Verifica DeclineFriendRequest_ByPlayerWhoIsNotTheReceiver_ReturnsNotValidError.</summary>
        [Fact]
        public void DeclineFriendRequest_ByPlayerWhoIsNotTheReceiver_ReturnsNotValidError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> bySender =
                scenario.Service.DeclineFriendRequest(scenario.AlphaToken, friendshipId);
            ResultWithNotification<OperationDeclineFriendRequestResult> byThirdPlayer =
                scenario.Service.DeclineFriendRequest(scenario.CharlieToken, friendshipId);

            // Assert
            Assert.Equal(NotValidKey, bySender.OperationResult.ErrorKey);
            Assert.Equal(NotValidKey, byThirdPlayer.OperationResult.ErrorKey);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
        }

        /// <summary>Verifica DeclineFriendRequest_WhenRequestIsNotPending_ReturnsNotValidError.</summary>
        [Theory]
        [InlineData(FriendshipState.Accepted)]
        [InlineData(FriendshipState.Rejected)]
        public void DeclineFriendRequest_WhenRequestIsNotPending_ReturnsNotValidError(FriendshipState state)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                state);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, actual.OperationResult.ErrorKey);
            Assert.Equal(state, scenario.Repository.Find(friendshipId).State);
        }

        /// <summary>Verifica DeclineFriendRequest_WithUnknownRequestOrInvalidSession_ReturnsError.</summary>
        [Theory]
        [InlineData(999, true, NotValidKey)]
        [InlineData(0, true, NotValidKey)]
        [InlineData(1, false, "Common_InvalidSessionError")]
        public void DeclineFriendRequest_WithUnknownRequestOrInvalidSession_ReturnsError(
            int friendRequestId,
            bool hasValidSession,
            string expectedKey)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            string token = hasValidSession ? scenario.BravoToken : null;

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(token, friendRequestId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(expectedKey, actual.OperationResult.ErrorKey);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(1).State);
        }

        /// <summary>Verifica DeclineFriendRequest_WhenAnotherChangeWinsTheRace_ReturnsNotValidError.</summary>
        [Fact]
        public void DeclineFriendRequest_WhenAnotherChangeWinsTheRace_ReturnsNotValidError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            scenario.Repository.ForceStateChangedOnWrite = true;

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, actual.OperationResult.ErrorKey);
        }

        [Fact]
        public void DeclineFriendRequest_WhenRepositoryFails_ThrowsInvalidOperationException()
        {
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            scenario.Repository.ThrowOnAnyCall = true;

            Assert.Throws<InvalidOperationException>(() =>
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId));
        }
    }
}
