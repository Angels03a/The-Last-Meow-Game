using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de FriendsService.AcceptFriendRequest.</summary>
    public class FriendsServiceAcceptRequestTests
    {
        private const string NotValidKey = "Friends_RequestNotValidError";

        /// <summary>Verifica AcceptFriendRequest_ByReceiver_AcceptsAndNotifiesBothPlayers.</summary>
        [Fact]
        public void AcceptFriendRequest_ByReceiver_AcceptsAndNotifiesBothPlayers()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.ErrorKey);
            Assert.Equal(friendshipId, actual.OperationResult.FriendRequestId);
            Assert.Equal(FriendsScenario.AlphaUserId, actual.OperationResult.NewFriend.UserId);
            Assert.True(actual.OperationResult.NewFriend.IsOnline);
            Assert.Equal(FriendshipState.Accepted, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(FriendsScenario.BravoUserId, Assert.Single(scenario.AlphaCallback.AddedFriends).UserId);
            Assert.Equal(FriendsScenario.AlphaUserId, Assert.Single(scenario.BravoCallback.AddedFriends).UserId);
        }

        /// <summary>Verifica AcceptFriendRequest_WithOfflineSender_NotifiesOnlyTheReceiver.</summary>
        [Fact]
        public void AcceptFriendRequest_WithOfflineSender_NotifiesOnlyTheReceiver()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.CharlieUserId, FriendsScenario.AlphaUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.AlphaToken, friendshipId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.False(actual.OperationResult.NewFriend.IsOnline);
            Assert.Equal(1, actual.Notifications.Count);
            Assert.Single(scenario.AlphaCallback.AddedFriends);
        }

        /// <summary>Verifica AcceptFriendRequest_ByPlayerWhoIsNotTheReceiver_ReturnsNotValidError.</summary>
        [Fact]
        public void AcceptFriendRequest_ByPlayerWhoIsNotTheReceiver_ReturnsNotValidError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> bySender =
                scenario.Service.AcceptFriendRequest(scenario.AlphaToken, friendshipId);
            ResultWithNotification<OperationAcceptFriendRequestResult> byThirdPlayer =
                scenario.Service.AcceptFriendRequest(scenario.CharlieToken, friendshipId);

            // Assert
            Assert.Equal(NotValidKey, bySender.OperationResult.ErrorKey);
            Assert.Equal(NotValidKey, byThirdPlayer.OperationResult.ErrorKey);
            Assert.Empty(bySender.Notifications);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
        }

        /// <summary>Verifica AcceptFriendRequest_Twice_ReturnsNotValidErrorTheSecondTime.</summary>
        [Fact]
        public void AcceptFriendRequest_Twice_ReturnsNotValidErrorTheSecondTime()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> first =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);
            ResultWithNotification<OperationAcceptFriendRequestResult> second =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.True(first.OperationResult.IsSuccess);
            Assert.False(second.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, second.OperationResult.ErrorKey);
            Assert.Empty(second.Notifications);
        }

        /// <summary>Verifica AcceptFriendRequest_WithUnknownRequestOrInvalidSession_ReturnsError.</summary>
        [Theory]
        [InlineData(999, true, NotValidKey)]
        [InlineData(0, true, NotValidKey)]
        [InlineData(-1, true, NotValidKey)]
        [InlineData(1, false, "Common_InvalidSessionError")]
        public void AcceptFriendRequest_WithUnknownRequestOrInvalidSession_ReturnsError(
            int friendRequestId,
            bool hasValidSession,
            string expectedKey)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            string token = hasValidSession ? scenario.BravoToken : "unknown";

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(token, friendRequestId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(expectedKey, actual.OperationResult.ErrorKey);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(1).State);
        }

        /// <summary>Verifica AcceptFriendRequest_WhenAnotherChangeWinsTheRace_ReturnsNotValidError.</summary>
        [Fact]
        public void AcceptFriendRequest_WhenAnotherChangeWinsTheRace_ReturnsNotValidError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            scenario.Repository.ForceStateChangedOnWrite = true;

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica AcceptFriendRequest_WhenRepositoryFails_ReturnsUnexpectedError.</summary>
        [Fact]
        public void AcceptFriendRequest_WhenRepositoryFails_ReturnsUnexpectedError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);
            scenario.Repository.ThrowOnAnyCall = true;

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal("Common_UnexpectedError", actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.NewFriend);
            Assert.Empty(actual.Notifications);
        }
    }
}
