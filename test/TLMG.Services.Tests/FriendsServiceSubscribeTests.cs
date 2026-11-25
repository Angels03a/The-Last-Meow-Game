using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of FriendsService.SubscribeToFriends: the friend list with its online flags, the pending requests and
    /// the presence notice that online friends receive. Rejections must not register the friends channel.
    /// </summary>
    public class FriendsServiceSubscribeTests
    {
        private const string FriendsChannelId = "friends-test";

        [Fact]
        public void SubscribeToFriends_WithPendingRequests_ReturnsIncomingAndOutgoingOnly()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            int incomingId = scenario.Repository.Seed(
                FriendsScenario.CharlieUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);
            int outgoingId = scenario.Repository.Seed(
                FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId, FriendshipState.Pending);
            scenario.Repository.Seed(
                FriendsScenario.CharlieUserId, FriendsScenario.AlphaUserId, FriendshipState.Rejected);

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, new FriendsCallbackStub());

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(2, actual.OperationResult.PendingFriendRequests.Count);
            FriendRequest incoming = Assert.Single(actual.OperationResult.PendingFriendRequests,
                request => request.IsIncoming);
            Assert.Equal(incomingId, incoming.FriendRequestId);
            Assert.Equal(FriendsScenario.CharlieUserId, incoming.UserId);
            Assert.Equal(FriendsScenario.CharlieNickname, incoming.Nickname);
            FriendRequest outgoing = Assert.Single(actual.OperationResult.PendingFriendRequests,
                request => !request.IsIncoming);
            Assert.Equal(outgoingId, outgoing.FriendRequestId);
            Assert.Equal(FriendsScenario.AlphaUserId, outgoing.UserId);
        }

        [Fact]
        public void SubscribeToFriends_WithoutFriendsOrRequests_ReturnsEmptyListsAndRegistersTheChannel()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            FriendsCallbackStub callback = new FriendsCallbackStub();

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, callback);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Empty(actual.OperationResult.Friends);
            Assert.Empty(actual.OperationResult.PendingFriendRequests);
            Assert.Empty(actual.Notifications);
            Assert.Same(callback, scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.BravoUserId));
        }

        [Fact]
        public void SubscribeToFriends_WithAcceptedFriends_ReturnsThemWithOnlineFlagsAndTellsOnlineFriends()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Accepted);
            scenario.Repository.Seed(
                FriendsScenario.BravoUserId, FriendsScenario.CharlieUserId, FriendshipState.Accepted);

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, new FriendsCallbackStub());
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(2, actual.OperationResult.Friends.Count);
            Friend alpha = Assert.Single(actual.OperationResult.Friends,
                friend => friend.UserId == FriendsScenario.AlphaUserId);
            Assert.Equal(FriendsScenario.AlphaNickname, alpha.Nickname);
            Assert.True(alpha.IsOnline);
            Friend charlie = Assert.Single(actual.OperationResult.Friends,
                friend => friend.UserId == FriendsScenario.CharlieUserId);
            Assert.False(charlie.IsOnline);
            Friend presence = Assert.Single(scenario.AlphaCallback.PresenceChanges);
            Assert.Equal(FriendsScenario.BravoUserId, presence.UserId);
            Assert.Equal(FriendsScenario.BravoNickname, presence.Nickname);
            Assert.True(presence.IsOnline);
        }

        [Fact]
        public void SubscribeToFriends_WhenAlreadySubscribed_ReplacesTheChannelWithoutAnnouncingPresenceAgain()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.Seed(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Accepted);
            FriendsCallbackStub newCallback = new FriendsCallbackStub();

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.AlphaToken, FriendsChannelId, newCallback);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.BravoCallback.PresenceChanges);
            Assert.Same(newCallback, scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.AlphaUserId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void SubscribeToFriends_WithInvalidSession_RejectsWithInvalidSessionAndRegistersNothing(string token)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(token, FriendsChannelId, new FriendsCallbackStub());

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Null(scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.BravoUserId));
        }

        [Fact]
        public void SubscribeToFriends_WhenThePendingLookupFails_RejectsWithUnexpectedErrorAndRegistersNothing()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            scenario.Repository.FailPendingLookup = true;

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, new FriendsCallbackStub());

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Null(scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.BravoUserId));
        }

        [Fact]
        public void SubscribeToFriends_WhenTheFriendListLookupFails_RejectsWithUnexpectedErrorAndRegistersNothing()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);
            scenario.Repository.FailFriendListLookup = true;

            // Act
            ResultWithNotification<OperationSubscribeToFriendsResult> actual =
                scenario.Service.SubscribeToFriends(scenario.BravoToken, FriendsChannelId, new FriendsCallbackStub());

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Null(scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.BravoUserId));
        }
    }
}
