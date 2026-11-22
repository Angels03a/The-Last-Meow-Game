using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of FriendsService.SendFriendRequest with an in-memory repository and the real session registry.
    /// Notifications are deferred actions, so the tests run them and check what each callback received.
    /// </summary>
    public class FriendsServiceSendRequestTests
    {
        [Fact]
        public void SendFriendRequest_WithExistingNickname_CreatesPendingRequestAndNotifiesOnlyTheReceiver()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(string.Empty, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.NewFriend);
            FriendshipRepositoryStub.StoredFriendship row = Assert.Single(scenario.Repository.Rows);
            Assert.Equal(FriendsScenario.AlphaUserId, row.SenderUserId);
            Assert.Equal(FriendsScenario.BravoUserId, row.ReceiverUserId);
            Assert.Equal(FriendshipState.Pending, row.State);
            FriendRequest sent = actual.OperationResult.SentFriendRequest;
            Assert.Equal(row.FriendshipId, sent.FriendRequestId);
            Assert.Equal(FriendsScenario.BravoUserId, sent.UserId);
            Assert.Equal(FriendsScenario.BravoNickname, sent.Nickname);
            Assert.False(sent.IsIncoming);
            FriendRequest received = Assert.Single(scenario.BravoCallback.ReceivedRequests);
            Assert.Equal(row.FriendshipId, received.FriendRequestId);
            Assert.Equal(FriendsScenario.AlphaUserId, received.UserId);
            Assert.Equal(FriendsScenario.AlphaNickname, received.Nickname);
            Assert.True(received.IsIncoming);
            Assert.Empty(scenario.AlphaCallback.ReceivedRequests);
            Assert.Empty(scenario.AlphaCallback.AddedFriends);
        }

        [Fact]
        public void SendFriendRequest_WithPaddedNickname_UsesTheTrimmedNickname()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            OperationSendFriendRequestResult actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "  Bravo  ").OperationResult;

            // Assert
            Assert.True(actual.IsSuccess);
            Assert.Equal(FriendsScenario.BravoNickname, actual.SentFriendRequest.Nickname);
        }

        [Fact]
        public void SendFriendRequest_WithUnknownCandidateStatus_RejectsWithoutWriteOrNotification()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.CandidateStatusOverride = (FriendLookupStatus)999;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> result =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, result.OperationResult.ErrorKey);
            Assert.Empty(scenario.Repository.Rows);
            Assert.Empty(result.Notifications);
        }

        [Fact]
        public void SendFriendRequest_WithUnknownPairStatus_RejectsWithoutWriteOrNotification()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.RecordStatusOverride = (FriendLookupStatus)999;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> result =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, result.OperationResult.ErrorKey);
            Assert.Empty(scenario.Repository.Rows);
            Assert.Empty(result.Notifications);
        }

        [Fact]
        public void SendFriendRequest_WithOfflineReceiver_StoresTheRequestWithoutNotifications()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Empty(actual.Notifications);
            FriendshipRepositoryStub.StoredFriendship row = Assert.Single(scenario.Repository.Rows);
            Assert.Equal(FriendshipState.Pending, row.State);
            Assert.Equal(row.FriendshipId, actual.OperationResult.SentFriendRequest.FriendRequestId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")]
        [InlineData("abcdefghijklm")]
        [InlineData("alpha@uv.mx")]
        public void SendFriendRequest_WithInvalidNickname_RejectsWithFormatErrorAndStoresNothing(string nickname)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, nickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.NicknameFormat, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.Repository.Rows);
        }

        [Fact]
        public void SendFriendRequest_WithUnknownNickname_RejectsWithUserNotFound()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Zulu");

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.UserNotFound, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.Repository.Rows);
        }

        [Theory]
        [InlineData("Alpha")]
        [InlineData("alpha")]
        [InlineData("  Alpha  ")]
        public void SendFriendRequest_ToOneself_RejectsWithCannotRequestSelf(string nickname)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, nickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.CannotRequestSelf, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.Repository.Rows);
        }

        [Theory]
        [InlineData(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId)]
        [InlineData(FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId)]
        public void SendFriendRequest_WhenAlreadyFriends_RejectsWithoutChangingTheFriendship(
            int originalSenderId,
            int originalReceiverId)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(originalSenderId, originalReceiverId, FriendshipState.Accepted);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.AlreadyFriends, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Equal(FriendshipState.Accepted, Assert.Single(scenario.Repository.Rows).State);
            Assert.Equal(friendshipId, scenario.Repository.Rows[0].FriendshipId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void SendFriendRequest_WithInvalidSession_RejectsWithInvalidSessionAndStoresNothing(string token)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(token, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.Repository.Rows);
        }

        [Fact]
        public void SendFriendRequest_WithPendingRequestAlreadySent_RejectsAsDuplicateWithoutNotifying()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.RequestAlreadySent, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.BravoCallback.ReceivedRequests);
            Assert.Single(scenario.Repository.Rows);
        }

        [Fact]
        public void SendFriendRequest_WithCrossedRequest_AcceptsItAndTellsEachPlayerAboutTheOther()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(
                FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId, FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.SentFriendRequest);
            Assert.Equal(FriendsScenario.BravoUserId, actual.OperationResult.NewFriend.UserId);
            Assert.Equal(FriendsScenario.BravoNickname, actual.OperationResult.NewFriend.Nickname);
            Assert.True(actual.OperationResult.NewFriend.IsOnline);
            Assert.Equal(FriendshipState.Accepted, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(2, actual.Notifications.Count);
            Friend addedForAlpha = Assert.Single(scenario.AlphaCallback.AddedFriends);
            Assert.Equal(FriendsScenario.BravoUserId, addedForAlpha.UserId);
            Assert.Equal(FriendsScenario.BravoNickname, addedForAlpha.Nickname);
            Friend addedForBravo = Assert.Single(scenario.BravoCallback.AddedFriends);
            Assert.Equal(FriendsScenario.AlphaUserId, addedForBravo.UserId);
            Assert.Equal(FriendsScenario.AlphaNickname, addedForBravo.Nickname);
            Assert.Empty(scenario.BravoCallback.ReceivedRequests);
        }

        [Theory]
        [InlineData(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId)]
        [InlineData(FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId)]
        public void SendFriendRequest_AfterRejection_ReactivatesTheRowWithTheCurrentSender(
            int oldSenderId,
            int oldReceiverId)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(oldSenderId, oldReceiverId, FriendshipState.Rejected);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(friendshipId, actual.OperationResult.SentFriendRequest.FriendRequestId);
            FriendshipRepositoryStub.StoredFriendship row = Assert.Single(scenario.Repository.Rows);
            Assert.Equal(FriendshipState.Pending, row.State);
            Assert.Equal(FriendsScenario.AlphaUserId, row.SenderUserId);
            Assert.Equal(FriendsScenario.BravoUserId, row.ReceiverUserId);
            FriendRequest received = Assert.Single(scenario.BravoCallback.ReceivedRequests);
            Assert.Equal(friendshipId, received.FriendRequestId);
            Assert.Equal(FriendsScenario.AlphaNickname, received.Nickname);
        }

        [Fact]
        public void SendFriendRequest_WhenAnotherRequestWinsTheRace_RejectsAsDuplicateWithoutNotifying()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.ForceDuplicateOnCreate = true;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Friends.RequestAlreadySent, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void SendFriendRequest_WhenTheNicknameLookupFails_RejectsWithUnexpectedErrorAndStoresNothing()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.FailCandidateLookup = true;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.Repository.Rows);
        }

        [Fact]
        public void SendFriendRequest_WhenTheFriendshipLookupFails_RejectsWithUnexpectedErrorAndStoresNothing()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.FailRecordLookup = true;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Empty(scenario.Repository.Rows);
        }

        [Fact]
        public void SendFriendRequest_WhenTheRepositoryThrows_PropagatesTheExceptionWithoutStoringAnything()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.ThrowOnAnyCall = true;

            // Act
            Action send = () => scenario.Service.SendFriendRequest(scenario.AlphaToken, FriendsScenario.BravoNickname);

            // Assert
            Assert.Throws<InvalidOperationException>(send);
            Assert.Empty(scenario.Repository.Rows);
        }
    }
}
