using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de FriendsService.SendFriendRequest.</summary>
    public class FriendsServiceSendRequestTests
    {
        private const string InvalidSessionKey = "Common_InvalidSessionError";

        private const string UnexpectedKey = "Common_UnexpectedError";

        /// <summary>Verifica SendFriendRequest_WithExistingNickname_CreatesPendingRequestAndNotifiesReceiver.</summary>
        [Fact]
        public void SendFriendRequest_WithExistingNickname_CreatesPendingRequestAndNotifiesReceiver()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.NewFriend);
            Assert.Equal(FriendsScenario.BravoUserId, actual.OperationResult.SentFriendRequest.UserId);
            Assert.False(actual.OperationResult.SentFriendRequest.IsIncoming);
            FriendshipRepositoryStub.StoredFriendship row = Assert.Single(scenario.Repository.Rows);
            Assert.Equal(FriendsScenario.AlphaUserId, row.SenderUserId);
            Assert.Equal(FriendshipState.Pending, row.State);
            FriendRequest received = Assert.Single(scenario.BravoCallback.ReceivedRequests);
            Assert.Equal(FriendsScenario.AlphaNickname, received.Nickname);
            Assert.True(received.IsIncoming);
            Assert.Equal(row.FriendshipId, received.FriendRequestId);
        }

        /// <summary>Verifica SendFriendRequest_WithOfflineReceiver_SucceedsWithoutNotifications.</summary>
        [Fact]
        public void SendFriendRequest_WithOfflineReceiver_SucceedsWithoutNotifications()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario(false);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Empty(actual.Notifications);
            Assert.Single(scenario.Repository.Rows);
        }

        /// <summary>Verifica SendFriendRequest_WithInvalidNickname_ReturnsFormatError.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("abcdefghijklm")]
        [InlineData("alpha@uv.mx")]
        public void SendFriendRequest_WithInvalidNickname_ReturnsFormatError(string nickname)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, nickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal("Friends_NicknameFormatError", actual.OperationResult.ErrorKey);
            Assert.Empty(scenario.Repository.Rows);
        }

        /// <summary>Verifica SendFriendRequest_WithUnknownSelfOrFriend_ReturnsRuleError.</summary>
        [Theory]
        [InlineData("Zulu", "Friends_UserNotFoundError", false)]
        [InlineData("Alpha", "Friends_CannotRequestSelfError", false)]
        [InlineData("alpha", "Friends_CannotRequestSelfError", false)]
        [InlineData("Bravo", "Friends_AlreadyFriendsError", true)]
        public void SendFriendRequest_WithUnknownSelfOrFriend_ReturnsRuleError(
            string nickname,
            string expectedKey,
            bool areAlreadyFriends)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            if (areAlreadyFriends)
            {
                scenario.Repository.Seed(FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId,
                    FriendshipState.Accepted);
            }

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, nickname);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(expectedKey, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica SendFriendRequest_WithInvalidSession_ReturnsInvalidSessionError.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void SendFriendRequest_WithInvalidSession_ReturnsInvalidSessionError(string token)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(token, "Bravo");

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Empty(scenario.Repository.Rows);
        }

        /// <summary>Verifica SendFriendRequest_WithPendingRequestAlreadySent_ReturnsDuplicateError.</summary>
        [Fact]
        public void SendFriendRequest_WithPendingRequestAlreadySent_ReturnsDuplicateError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.Seed(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal("Friends_RequestAlreadySentError", actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
            Assert.Single(scenario.Repository.Rows);
        }

        /// <summary>Verifica SendFriendRequest_WithCrossedRequest_AcceptsItAndNotifiesBothPlayers.</summary>
        [Fact]
        public void SendFriendRequest_WithCrossedRequest_AcceptsItAndNotifiesBothPlayers()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId,
                FriendshipState.Pending);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.SentFriendRequest);
            Assert.Equal(FriendsScenario.BravoUserId, actual.OperationResult.NewFriend.UserId);
            Assert.True(actual.OperationResult.NewFriend.IsOnline);
            Assert.Equal(FriendshipState.Accepted, scenario.Repository.Find(friendshipId).State);
            Assert.Equal(2, actual.Notifications.Count);
            Assert.Equal(FriendsScenario.BravoUserId, Assert.Single(scenario.AlphaCallback.AddedFriends).UserId);
            Assert.Equal(FriendsScenario.AlphaUserId, Assert.Single(scenario.BravoCallback.AddedFriends).UserId);
            Assert.Empty(scenario.BravoCallback.ReceivedRequests);
        }

        /// <summary>Verifica SendFriendRequest_AfterRejection_ReactivatesRowWithCurrentSender.</summary>
        [Theory]
        [InlineData(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId)]
        [InlineData(FriendsScenario.BravoUserId, FriendsScenario.AlphaUserId)]
        public void SendFriendRequest_AfterRejection_ReactivatesRowWithCurrentSender(int oldSenderId, int oldReceiverId)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            int friendshipId = scenario.Repository.Seed(oldSenderId, oldReceiverId, FriendshipState.Rejected);

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");
            FriendsScenario.Deliver(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(friendshipId, actual.OperationResult.SentFriendRequest.FriendRequestId);
            FriendshipRepositoryStub.StoredFriendship row = Assert.Single(scenario.Repository.Rows);
            Assert.Equal(FriendshipState.Pending, row.State);
            Assert.Equal(FriendsScenario.AlphaUserId, row.SenderUserId);
            Assert.Equal(FriendsScenario.BravoUserId, row.ReceiverUserId);
            Assert.Single(scenario.BravoCallback.ReceivedRequests);
        }

        /// <summary>Verifica SendFriendRequest_WhenAnotherRequestWinsTheRace_ReturnsDuplicateError.</summary>
        [Fact]
        public void SendFriendRequest_WhenAnotherRequestWinsTheRace_ReturnsDuplicateError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.ForceDuplicateOnCreate = true;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal("Friends_RequestAlreadySentError", actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica SendFriendRequest_WhenRepositoryFails_ReturnsUnexpectedError.</summary>
        [Fact]
        public void SendFriendRequest_WhenRepositoryFails_ReturnsUnexpectedError()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Repository.ThrowOnAnyCall = true;

            // Act
            ResultWithNotification<OperationSendFriendRequestResult> actual =
                scenario.Service.SendFriendRequest(scenario.AlphaToken, "Bravo");

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(UnexpectedKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.SentFriendRequest);
            Assert.Empty(actual.Notifications);
        }
    }
}
