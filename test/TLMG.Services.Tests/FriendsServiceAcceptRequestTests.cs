using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of FriendsService.AcceptFriendRequest. Only the receiver of a pending request can accept it; every
    /// rejection must leave the request as it was and notify nobody.
    /// </summary>
    public class FriendsServiceAcceptRequestTests
    {
        [Fact]
        public void AcceptFriendRequest_ByReceiver_AcceptsAndTellsEachPlayerAboutTheOther()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.ErrorKey);
            Assert.Equal(friendshipId, actual.OperationResult.FriendRequestId);
            Assert.Equal(FriendsScenario.AlphaUserId, actual.OperationResult.NewFriend.UserId);
            Assert.Equal(FriendsScenario.AlphaNickname, actual.OperationResult.NewFriend.Nickname);
            Assert.True(actual.OperationResult.NewFriend.IsOnline);
            Assert.Equal(FriendshipState.Accepted, scenario.Repository.Find(friendshipId).State);
            Friend addedForAlpha = Assert.Single(scenario.AlphaCallback.AddedFriends);
            Assert.Equal(FriendsScenario.BravoUserId, addedForAlpha.UserId);
            Assert.Equal(FriendsScenario.BravoNickname, addedForAlpha.Nickname);
            Friend addedForBravo = Assert.Single(scenario.BravoCallback.AddedFriends);
            Assert.Equal(FriendsScenario.AlphaUserId, addedForBravo.UserId);
            Assert.Equal(FriendsScenario.AlphaNickname, addedForBravo.Nickname);
        }

        [Fact]
        public void AcceptFriendRequest_WithOfflineSender_ReportsHimOfflineAndNotifiesOnlyTheReceiver()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.CharlieUserId, FriendsScenario.AlphaUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.AlphaToken, friendshipId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(FriendsScenario.CharlieUserId, actual.OperationResult.NewFriend.UserId);
            Assert.False(actual.OperationResult.NewFriend.IsOnline);
            Assert.Equal(FriendshipState.Accepted, scenario.Repository.Find(friendshipId).State);
            Assert.Single(actual.Notifications);
            Friend addedForAlpha = Assert.Single(scenario.AlphaCallback.AddedFriends);
            Assert.Equal(FriendsScenario.CharlieUserId, addedForAlpha.UserId);
            Assert.False(addedForAlpha.IsOnline);
            Assert.Empty(scenario.BravoCallback.AddedFriends);
        }

        [Fact]
        public void AcceptFriendRequest_ByPlayerWhoIsNotTheReceiver_RejectsAndLeavesTheRequestPending()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> bySender =
                scenario.Service.AcceptFriendRequest(scenario.AlphaToken, friendshipId);
            ResultWithNotification<OperationAcceptFriendRequestResult> byThirdPlayer =
                scenario.Service.AcceptFriendRequest(scenario.CharlieToken, friendshipId);

            // Assert
            AssertRejected(bySender, ErrorKeys.Friends.RequestNotValid);
            AssertRejected(byThirdPlayer, ErrorKeys.Friends.RequestNotValid);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Fact]
        public void AcceptFriendRequest_Twice_RejectsTheSecondTimeWithoutDuplicatingTheFriendship()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> first =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);
            ResultWithNotification<OperationAcceptFriendRequestResult> second =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);
            FriendsScenario.Deliver(first.Notifications);
            FriendsScenario.Deliver(second.Notifications);

            // Assert
            Assert.True(first.OperationResult.IsSuccess);
            AssertRejected(second, ErrorKeys.Friends.RequestNotValid);
            Assert.Single(scenario.Repository.Rows);
            Assert.Single(scenario.AlphaCallback.AddedFriends);
            Assert.Single(scenario.BravoCallback.AddedFriends);
        }

        [Theory]
        [InlineData(FriendshipState.Accepted)]
        [InlineData(FriendshipState.Rejected)]
        public void AcceptFriendRequest_WhenAlreadyResolved_RejectsAndKeepsItsState(FriendshipState state)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, state);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Friends.RequestNotValid);
            Assert.Equal(state, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Theory]
        [InlineData(999)]
        [InlineData(0)]
        [InlineData(-1)]
        public void AcceptFriendRequest_WithUnknownRequestId_RejectsAndLeavesOthersPending(int friendRequestId)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int existingId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendRequestId);

            // Assert
            AssertRejected(actual, ErrorKeys.Friends.RequestNotValid);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(existingId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void AcceptFriendRequest_WithInvalidSession_RejectsAndLeavesTheRequestPending(string token)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(token, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Common.InvalidSession);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(0, scenario.Repository.AnswerWriteCount);
        }

        [Fact]
        public void AcceptFriendRequest_WhenAnotherChangeWinsTheRace_RejectsWithoutNotifying()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            scenario.Repository.ForceStateChangedOnWrite = true;

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            AssertRejected(actual, ErrorKeys.Friends.RequestNotValid);
            Assert.Empty(scenario.AlphaCallback.AddedFriends);
            Assert.Empty(scenario.BravoCallback.AddedFriends);
        }

        [Fact]
        public void AcceptFriendRequest_WhenTheRequestLookupFails_RejectsWithUnexpectedErrorAndKeepsItPending()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            scenario.Repository.FailRecordLookup = true;

            // Act
            ResultWithNotification<OperationAcceptFriendRequestResult> actual =
                scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            AssertRejected(actual, ErrorKeys.Common.Unexpected);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
        }

        [Fact]
        public void AcceptFriendRequest_WhenTheRepositoryThrows_PropagatesTheExceptionAndKeepsTheRequestPending()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            scenario.Repository.ThrowOnAnyCall = true;

            // Act
            Action accept = () => scenario.Service.AcceptFriendRequest(scenario.BravoToken, friendshipId);

            // Assert
            Assert.Throws<InvalidOperationException>(accept);
            Assert.Equal(FriendshipState.Pending, scenario.Repository.Find(friendshipId).State);
        }

        private static void AssertRejected(
            ResultWithNotification<OperationAcceptFriendRequestResult> actual,
            string expectedErrorKey)
        {
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(expectedErrorKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.NewFriend);
            Assert.Empty(actual.Notifications);
        }
    }
}
