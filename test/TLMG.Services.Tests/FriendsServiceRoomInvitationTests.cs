using System;
using System.Collections.Generic;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Friends;
using TLMG.Services.Sessions;

using TLMG.Services.Moderation;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of FriendsService room invitations with the real session registry and a manual clock. Declining a
    /// pending invitation is not reported to the server, so the invitee stays able to receive it again after the
    /// renotification interval. Notifications are deferred actions, so the tests run them and check the callbacks.
    /// </summary>
    public class FriendsServiceRoomInvitationTests
    {
        private const int InviterUserId = 1;

        private const int InviteeUserId = 2;

        private const int RenotifyIntervalSeconds = 5;

        private readonly ManualUtcClock _clock = new ManualUtcClock();

        private readonly FriendsCallbackStub _inviteeCallback = new FriendsCallbackStub();

        private readonly SessionRegistry _registry;

        private readonly FriendsService _service;

        private readonly string _inviterToken;

        private readonly string _inviteeToken;

        public FriendsServiceRoomInvitationTests()
        {
            FriendshipRepositoryStub repository = new FriendshipRepositoryStub();
            repository.AddUser(InviterUserId, "Inviter");
            repository.AddUser(InviteeUserId, "Invitee");
            repository.Seed(InviterUserId, InviteeUserId, FriendshipState.Accepted);
            _registry = new SessionRegistry(_clock.ReadUtcNow, count => 0);
            BlockPolicy blockPolicy = new BlockPolicy(new NoBlocksRepository());
            _registry.ConfigureBlockPolicy(blockPolicy);
            _service = new FriendsService(repository, _registry, _registry, blockPolicy);
            _inviterToken = StartSession(InviterUserId, "Inviter", new FriendsCallbackStub());
            _inviteeToken = StartSession(InviteeUserId, "Invitee", _inviteeCallback);
            _registry.CreateRoom(new SessionChannelCaller(_inviterToken, "inviter-lobby"), null);
        }

        [Fact]
        public void InviteFriendToRoom_FirstInvitation_NotifiesTheInviteeOnceWithTheInviterNickname()
        {
            // Act
            ResultWithNotification<OperationResult> actual = Invite();

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(string.Empty, actual.OperationResult.ErrorKey);
            RoomInvitation received = Assert.Single(_inviteeCallback.ReceivedInvitations);
            Assert.Equal("Inviter", received.InviterNickname);
        }

        [Fact]
        public void InviteFriendToRoom_RepeatedWhileStillPendingInsideTheInterval_SucceedsWithoutANewNotification()
        {
            // Arrange
            Invite();
            _clock.Advance(TimeSpan.FromSeconds(RenotifyIntervalSeconds - 1));

            // Act
            ResultWithNotification<OperationResult> repeated = Invite();

            // Assert
            Assert.True(repeated.OperationResult.IsSuccess);
            Assert.Equal(string.Empty, repeated.OperationResult.ErrorKey);
            Assert.Single(_inviteeCallback.ReceivedInvitations);
        }

        [Fact]
        public void InviteFriendToRoom_AfterTheInviteeIgnoredOrDeclinedAndTheIntervalPassed_NotifiesAgainWithTheSameId()
        {
            // Arrange
            Invite();
            _clock.Advance(TimeSpan.FromSeconds(RenotifyIntervalSeconds));

            // Act
            ResultWithNotification<OperationResult> second = Invite();

            // Assert
            Assert.True(second.OperationResult.IsSuccess);
            Assert.Equal(2, _inviteeCallback.ReceivedInvitations.Count);
            Assert.Equal(
                _inviteeCallback.ReceivedInvitations[0].InvitationId,
                _inviteeCallback.ReceivedInvitations[1].InvitationId);
        }

        [Fact]
        public void InviteFriendToRoom_RepeatedEverySecondForTenSeconds_NotifiesOnlyOncePerInterval()
        {
            // Act
            for (int second = 0; second < 10; second++)
            {
                Invite();
                _clock.Advance(TimeSpan.FromSeconds(1));
            }

            // Assert
            Assert.Equal(2, _inviteeCallback.ReceivedInvitations.Count);
        }

        [Fact]
        public void AcceptRoomInvitation_AfterTheSecondNotification_AuthorizesTheRoomAndConsumesTheInvitation()
        {
            // Arrange
            Invite();
            _clock.Advance(TimeSpan.FromSeconds(RenotifyIntervalSeconds));
            Invite();
            int invitationId = _inviteeCallback.ReceivedInvitations[1].InvitationId;

            // Act
            ResultWithNotification<OperationAcceptRoomInvitationResult> accepted =
                _service.AcceptRoomInvitation(_inviteeToken, invitationId);
            ResultWithNotification<OperationAcceptRoomInvitationResult> acceptedAgain =
                _service.AcceptRoomInvitation(_inviteeToken, invitationId);

            // Assert
            Assert.True(accepted.OperationResult.IsSuccess);
            Assert.Equal(string.Empty, accepted.OperationResult.ErrorKey);
            Assert.False(acceptedAgain.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.InvitationNotValid, acceptedAgain.OperationResult.ErrorKey);
        }

        [Fact]
        public void AcceptRoomInvitation_ByAnotherPlayer_IsRejected()
        {
            // Arrange
            Invite();
            int invitationId = _inviteeCallback.ReceivedInvitations[0].InvitationId;

            // Act
            ResultWithNotification<OperationAcceptRoomInvitationResult> actual =
                _service.AcceptRoomInvitation(_inviterToken, invitationId);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.InvitationNotValid, actual.OperationResult.ErrorKey);
        }

        [Fact]
        public void InviteFriendToRoom_AfterTheRoomClosed_RejectsWithoutNotifyingAndTheOldInvitationIsNotValid()
        {
            // Arrange
            Invite();
            int invitationId = _inviteeCallback.ReceivedInvitations[0].InvitationId;
            _registry.LeaveRoom(_inviterToken, out IReadOnlyList<MatchViewDelivery> deliveries);
            _clock.Advance(TimeSpan.FromSeconds(RenotifyIntervalSeconds));

            // Act
            ResultWithNotification<OperationResult> repeated = Invite();
            ResultWithNotification<OperationAcceptRoomInvitationResult> accepted =
                _service.AcceptRoomInvitation(_inviteeToken, invitationId);

            // Assert
            Assert.False(repeated.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Lobby.NotInRoom, repeated.OperationResult.ErrorKey);
            Assert.Single(_inviteeCallback.ReceivedInvitations);
            Assert.False(accepted.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.InvitationNotValid, accepted.OperationResult.ErrorKey);
        }

        [Fact]
        public void InviteFriendToRoom_WithUnknownSession_RejectsWithoutNotifying()
        {
            // Act
            ResultWithNotification<OperationResult> actual = _service.InviteFriendToRoom("unknown", InviteeUserId);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, actual.OperationResult.ErrorKey);
            Assert.Empty(_inviteeCallback.ReceivedInvitations);
        }

        [Fact]
        public void InviteFriendToRoom_WithAPlayerWhoIsNotAFriend_RejectsWithoutNotifying()
        {
            // Act
            ResultWithNotification<OperationResult> actual = _service.InviteFriendToRoom(_inviterToken, 99);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.CannotInvite, actual.OperationResult.ErrorKey);
            Assert.Empty(_inviteeCallback.ReceivedInvitations);
        }

        private ResultWithNotification<OperationResult> Invite()
        {
            ResultWithNotification<OperationResult> result = _service.InviteFriendToRoom(_inviterToken, InviteeUserId);
            FriendsScenario.Deliver(result.Notifications);

            return result;
        }

        private string StartSession(int userId, string nickname, FriendsCallbackStub callback)
        {
            string token = _registry.StartRegisteredSession(userId, nickname, out SessionEnd replacedSession);
            _registry.SubscribeFriendsChannel(token, "friends" + userId, callback, Array.Empty<int>());

            return token;
        }
    }
}
