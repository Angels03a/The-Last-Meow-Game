using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of FriendsService.DeclineFriendRequest. Declining stores the rejection and tells nobody; every other
    /// case is rejected and leaves the request as it was.
    /// </summary>
    public class FriendsServiceDeclineRequestTests
    {
        [Fact]
        public void DeclineFriendRequest_ByReceiver_RejectsTheRequestWithoutNotifyingTheSender()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

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
            Assert.Empty(scenario.BravoCallback.AddedFriends);
        }

        [Fact]
        public void DeclineFriendRequest_ByPlayerWhoIsNotTheReceiver_RejectsAndLeavesTheRequestPending()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> bySender =
                scenario.Service.DeclineFriendRequest(scenario.AlphaToken, friendshipId);
            ResultWithNotification<OperationDeclineFriendRequestResult> byThirdPlayer =
                scenario.Service.DeclineFriendRequest(scenario.CharlieToken, friendshipId);

            // Assert
            AssertRejected(bySender, ErrorKeys.Friends.RequestNotValid);
            AssertRejected(byThirdPlayer, ErrorKeys.Friends.RequestNotValid);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Theory]
        [InlineData(FriendshipState.Accepted)]
        [InlineData(FriendshipState.Rejected)]
        public void DeclineFriendRequest_WhenAlreadyResolved_RejectsAndKeepsItsState(FriendshipState state)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, state);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Friends.RequestNotValid);
            Assert.Equal(state, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Theory]
        [InlineData(999)]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeclineFriendRequest_WithUnknownRequestId_RejectsAndLeavesOthersPending(int friendRequestId)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int existingId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendRequestId);

            // Assert
            AssertRejected(actual, ErrorKeys.Friends.RequestNotValid);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(existingId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void DeclineFriendRequest_WithInvalidSession_RejectsAndLeavesTheRequestPending(string token)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(token, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Common.InvalidSession);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Fact]
        public void DeclineFriendRequest_WhenAnotherChangeWinsTheRace_RejectsWithoutNotifying()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            scenario.Repository.ForceStateChangedOnWrite = true;

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Friends.RequestNotValid);
        }

        [Fact]
        public void DeclineFriendRequest_WhenTheRequestLookupFails_RejectsWithUnexpectedErrorAndKeepsItPending()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            scenario.Repository.FailRecordLookup = true;

            // Act
            ResultWithNotification<OperationDeclineFriendRequestResult> actual =
                scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Common.Unexpected);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
        }

        [Fact]
        public void DeclineFriendRequest_WhenTheRepositoryThrows_PropagatesTheExceptionAndKeepsTheRequestPending()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            scenario.Repository.ThrowOnAnyCall = true;

            // Act
            Action decline = () => scenario.Service.DeclineFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.Throws<InvalidOperationException>(decline);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
        }

        private static void AssertRejected(
            ResultWithNotification<OperationDeclineFriendRequestResult> actual,
            string expectedErrorKey)
        {
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(expectedErrorKey, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }
    }
}
