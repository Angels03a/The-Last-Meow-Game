using System;
using System.Collections.Generic;

using Xunit;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Chat;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of ChatService over the real SessionRegistry, so the delivery it builds comes from a real accepted
    /// message. The chat repository and the chat callbacks are local doubles. A successful send returns no response
    /// (OperationResult is null) and only deferred actions, so the tests run those actions as the service host does
    /// and then check what the callbacks received and what the repository stored. The registry rules for channels and
    /// rooms are tested in SessionRegistryChannelIdentityTests; here only their effect on the service result is
    /// checked.
    /// The send interval of the registry uses the system clock, so the limit is tested only by sending twice in a row.
    /// </summary>
    public class ChatServiceTests
    {
        private const int CreatorUserId = 1;
        private const int FirstGuestUserId = 2;
        private const int SecondGuestUserId = 3;
        private const int UnknownRoomOffset = 100;
        private const string CreatorNickname = "Creator";
        private const string ChatText = "Hello, cats";
        private static readonly Guid ClientMessageId = new Guid("11111111-2222-3333-4444-555555555555");

        private readonly LobbyScenario _lobby = new LobbyScenario();
        private readonly ChatRepositoryDouble _repository = new ChatRepositoryDouble();
        private readonly ChatCallbackDouble _creatorCallback = new ChatCallbackDouble();
        private readonly ChatCallbackDouble _firstGuestCallback = new ChatCallbackDouble();
        private readonly ChatCallbackDouble _secondGuestCallback = new ChatCallbackDouble();
        private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();

        public ChatServiceTests()
        {
            _tokensByUserId[CreatorUserId] = _lobby.CreatorToken;
            _tokensByUserId[FirstGuestUserId] =
                LobbyReadinessHelper.AddPlayer(_lobby, FirstGuestUserId, "lobby2", null);
            _tokensByUserId[SecondGuestUserId] =
                LobbyReadinessHelper.AddPlayer(_lobby, SecondGuestUserId, "lobby3", null);
        }

        [Fact]
        public void SubscribeToChat_WithRoomMember_ReturnsSuccessWithoutNotifications()
        {
            // Arrange
            ChatService service = CreateService();

            // Act
            ResultWithNotification<OperationResult> result =
                service.SubscribeToChat(ChatCallerOf(CreatorUserId), _creatorCallback);

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(string.Empty, result.OperationResult.ErrorKey);
            Assert.Empty(result.Notifications);
        }

        [Fact]
        public void SubscribeToChat_WithUnknownSession_RejectsWithInvalidSession()
        {
            // Arrange
            ChatService service = CreateService();

            // Act
            ResultWithNotification<OperationResult> result =
                service.SubscribeToChat(new SessionChannelCaller("unknown-token", "chat-unknown"), _creatorCallback);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.OperationResult.ErrorKey);
            Assert.Empty(result.Notifications);
        }

        [Fact]
        public void SubscribeToChat_WithPlayerOutsideAnyRoom_RejectsWithNotInRoom()
        {
            // Arrange
            ChatService service = CreateService();
            string lonelyToken = _lobby.Registry.StartRegisteredSession(9, "Lonely", out SessionEnd replaced);

            // Act
            ResultWithNotification<OperationResult> result =
                service.SubscribeToChat(new SessionChannelCaller(lonelyToken, "chat9"), _creatorCallback);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Lobby.NotInRoom, result.OperationResult.ErrorKey);
        }

        [Fact]
        public void SendChatMessage_WithValidText_ReturnsNoResponseAndDoesNothingUntilTheActionsRun()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));

            // Assert
            Assert.Null(result.OperationResult);
            Assert.NotEmpty(result.Notifications);
            Assert.Empty(_creatorCallback.ReceivedMessages);
            Assert.Empty(_firstGuestCallback.ReceivedMessages);
            Assert.Empty(_repository.SavedRecords);
        }

        [Fact]
        public void SendChatMessage_WithValidText_DeliversTheMessageWithTheSenderIdentityToEverySubscribedMember()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);
            DateTime beforeUtc = DateTime.UtcNow;

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(result);

            // Assert
            DateTime afterUtc = DateTime.UtcNow;
            AssertDeliveredMessage(_creatorCallback, beforeUtc, afterUtc);
            AssertDeliveredMessage(_firstGuestCallback, beforeUtc, afterUtc);
            AssertDeliveredMessage(_secondGuestCallback, beforeUtc, afterUtc);
        }

        [Fact]
        public void SendChatMessage_WhenAMemberHasNotSubscribed_DoesNotDeliverToThatMember()
        {
            // Arrange
            ChatService service = CreateService();
            service.SubscribeToChat(ChatCallerOf(CreatorUserId), _creatorCallback);
            service.SubscribeToChat(ChatCallerOf(FirstGuestUserId), _firstGuestCallback);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(result);

            // Assert
            Assert.Single(_creatorCallback.ReceivedMessages);
            Assert.Single(_firstGuestCallback.ReceivedMessages);
            Assert.Empty(_secondGuestCallback.ReceivedMessages);
        }

        [Fact]
        public void SendChatMessage_WithValidText_SavesTheMessageAsALobbyRecordOfTheSender()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(result);

            // Assert
            ChatMessageRecord record = Assert.Single(_repository.SavedRecords);
            Assert.Equal(CreatorUserId, record.UserId);
            Assert.Equal(ChatMessageScope.Lobby, record.Scope);
            Assert.Equal(_lobby.RoomId, record.RoomNumber);
            Assert.False(record.GameId.HasValue);
            Assert.Equal(ChatText, record.Text);
            Assert.Equal(Assert.Single(_creatorCallback.ReceivedMessages).SentAtUtc, record.SentAtUtc);
        }

        [Fact]
        public void SendChatMessage_WithSpacesAroundTheText_DeliversAndSavesTheTrimmedText()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, "   " + ChatText + "  "));
            ExecuteDeferredActions(result);

            // Assert
            Assert.Equal(ChatText, Assert.Single(_firstGuestCallback.ReceivedMessages).Text);
            Assert.Equal(ChatText, Assert.Single(_repository.SavedRecords).Text);
        }

        [Fact]
        public void SendChatMessage_WhenSavingFails_StillDeliversTheMessageToTheRoom()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);
            _repository.SaveStatus = ChatMessageSaveStatus.DataFailure;

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(result);

            // Assert
            Assert.Single(_repository.SavedRecords);
            Assert.Single(_creatorCallback.ReceivedMessages);
            Assert.Single(_firstGuestCallback.ReceivedMessages);
            Assert.Single(_secondGuestCallback.ReceivedMessages);
        }

        [Theory]
        [InlineData(null, ErrorKeys.Lobby.ChatEmpty)]
        [InlineData("", ErrorKeys.Lobby.ChatEmpty)]
        [InlineData("     ", ErrorKeys.Lobby.ChatEmpty)]
        [InlineData("hello!", ErrorKeys.Lobby.ChatCharacters)]
        public void SendChatMessage_WithInvalidText_RejectsWithTheTextErrorAndNothingIsDeliveredOrSaved(
            string text,
            string expectedErrorKey)
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, text));

            // Assert
            AssertRejectedWithoutSideEffects(result, expectedErrorKey);
        }

        [Fact]
        public void SendChatMessage_WithTextAboveTheMaximumLength_RejectsWithChatTooLong()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);
            string tooLongText = new string('a', 161);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, tooLongText));

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Lobby.ChatTooLong);
        }

        [Fact]
        public void SendChatMessage_WithTextOfTheMaximumLength_IsAccepted()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);
            string maximumText = new string('a', 160);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, maximumText));
            ExecuteDeferredActions(result);

            // Assert
            Assert.Null(result.OperationResult);
            Assert.Equal(maximumText, Assert.Single(_repository.SavedRecords).Text);
        }

        [Fact]
        public void SendChatMessage_WithNullRequest_RejectsBecauseTheMembershipIsCheckedBeforeTheText()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result =
                service.SendChatMessage(ChatCallerOf(CreatorUserId), null);

            // Assert
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Lobby.NotInRoom, result.OperationResult.ErrorKey);
            Assert.Equal(Guid.Empty, result.OperationResult.ClientMessageId);
            Assert.Empty(result.Notifications);
            Assert.Empty(_repository.SavedRecords);
        }

        [Fact]
        public void SendChatMessage_WithUnknownSession_RejectsWithInvalidSession()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                new SessionChannelCaller("unknown-token", "chat-unknown"), CreateRequest(_lobby.RoomId, ChatText));

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Common.InvalidSession);
        }

        [Fact]
        public void SendChatMessage_BeforeSubscribingToChat_RejectsWithChatNotSubscribed()
        {
            // Arrange
            ChatService service = CreateService();

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Lobby.ChatNotSubscribed);
        }

        [Fact]
        public void SendChatMessage_ToAnotherRoom_RejectsWithNotInRoom()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> result = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId + UnknownRoomOffset, ChatText));

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Lobby.NotInRoom);
        }

        [Fact]
        public void SendChatMessage_WithAChannelThatWasReplaced_RejectsAndOnlyTheNewChannelReceivesLaterMessages()
        {
            // Arrange
            ChatService service = CreateService();
            ChatCallbackDouble replacementCallback = new ChatCallbackDouble();
            SessionChannelCaller replacedCaller = new SessionChannelCaller(_tokensByUserId[CreatorUserId], "chat-old");
            SessionChannelCaller replacementCaller =
                new SessionChannelCaller(_tokensByUserId[CreatorUserId], "chat-new");
            service.SubscribeToChat(replacedCaller, _creatorCallback);
            service.SubscribeToChat(replacementCaller, replacementCallback);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> rejected = service.SendChatMessage(
                replacedCaller, CreateRequest(_lobby.RoomId, ChatText));
            ResultWithNotification<OperationSendChatMessageResult> accepted = service.SendChatMessage(
                replacementCaller, CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(rejected);
            ExecuteDeferredActions(accepted);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, rejected.OperationResult.ErrorKey);
            Assert.Empty(_creatorCallback.ReceivedMessages);
            Assert.Single(replacementCallback.ReceivedMessages);
            Assert.Single(_repository.SavedRecords);
        }

        [Fact]
        public void SendChatMessage_WhenThePlayerSendsTwiceInARow_RejectsTheSecondWithChatRateLimit()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);
            ResultWithNotification<OperationSendChatMessageResult> first = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(first);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> second = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(second);

            // Assert
            Assert.Null(first.OperationResult);
            Assert.False(second.OperationResult.IsSuccess);
            Assert.Equal(ErrorKeys.Lobby.ChatRateLimit, second.OperationResult.ErrorKey);
            Assert.Equal(ClientMessageId, second.OperationResult.ClientMessageId);
            Assert.Single(_firstGuestCallback.ReceivedMessages);
            Assert.Single(_repository.SavedRecords);
        }

        [Fact]
        public void SendChatMessage_WhenAnotherPlayerSendsRightAfter_IsNotAffectedByTheFirstPlayerLimit()
        {
            // Arrange
            ChatService service = CreateService();
            SubscribeRoomToChat(service);
            ResultWithNotification<OperationSendChatMessageResult> first = service.SendChatMessage(
                ChatCallerOf(CreatorUserId), CreateRequest(_lobby.RoomId, ChatText));
            ExecuteDeferredActions(first);

            // Act
            ResultWithNotification<OperationSendChatMessageResult> second = service.SendChatMessage(
                ChatCallerOf(FirstGuestUserId), CreateRequest(_lobby.RoomId, "Meow"));
            ExecuteDeferredActions(second);

            // Assert
            Assert.Null(second.OperationResult);
            Assert.Equal(2, _repository.SavedRecords.Count);
            Assert.Equal(FirstGuestUserId, _repository.SavedRecords[1].UserId);
            Assert.Equal("Meow", _secondGuestCallback.ReceivedMessages[1].Text);
        }

        private ChatService CreateService()
        {
            return new ChatService(_lobby.Registry, _repository);
        }

        private SessionChannelCaller ChatCallerOf(int userId)
        {
            return new SessionChannelCaller(_tokensByUserId[userId], "chat" + userId);
        }

        private void SubscribeRoomToChat(ChatService service)
        {
            service.SubscribeToChat(ChatCallerOf(CreatorUserId), _creatorCallback);
            service.SubscribeToChat(ChatCallerOf(FirstGuestUserId), _firstGuestCallback);
            service.SubscribeToChat(ChatCallerOf(SecondGuestUserId), _secondGuestCallback);
        }

        private static OperationSendChatMessageRequest CreateRequest(int roomId, string text)
        {
            return new OperationSendChatMessageRequest
            {
                ClientMessageId = ClientMessageId,
                RoomId = roomId,
                Text = text
            };
        }

        /// <summary>
        /// Runs the deferred actions of a result in order, as the service host does outside the lock.
        /// </summary>
        private static void ExecuteDeferredActions<TypeOfResult>(ResultWithNotification<TypeOfResult> result)
        {
            foreach (Action deferredAction in result.Notifications)
            {
                deferredAction();
            }
        }

        private void AssertDeliveredMessage(ChatCallbackDouble callback, DateTime beforeUtc, DateTime afterUtc)
        {
            ChatMessage message = Assert.Single(callback.ReceivedMessages);
            Assert.Equal(_lobby.RoomId, message.RoomId);
            Assert.Equal(ClientMessageId, message.ClientMessageId);
            Assert.Equal(CreatorUserId, message.SenderUserId);
            Assert.Equal(CreatorNickname, message.SenderNickname);
            Assert.Equal(ChatText, message.Text);
            Assert.InRange(message.SentAtUtc, beforeUtc, afterUtc);
        }

        private void AssertRejectedWithoutSideEffects(
            ResultWithNotification<OperationSendChatMessageResult> result,
            string expectedErrorKey)
        {
            Assert.False(result.OperationResult.IsSuccess);
            Assert.Equal(expectedErrorKey, result.OperationResult.ErrorKey);
            Assert.Equal(ClientMessageId, result.OperationResult.ClientMessageId);
            Assert.Empty(result.Notifications);
            Assert.Empty(_repository.SavedRecords);
            Assert.Empty(_creatorCallback.ReceivedMessages);
            Assert.Empty(_firstGuestCallback.ReceivedMessages);
            Assert.Empty(_secondGuestCallback.ReceivedMessages);
        }

        private sealed class ChatRepositoryDouble : IChatMessageRepository
        {
            internal ChatMessageSaveStatus SaveStatus { get; set; } = ChatMessageSaveStatus.Saved;

            internal List<ChatMessageRecord> SavedRecords { get; } = new List<ChatMessageRecord>();

            public ChatMessageSaveStatus SaveChatMessage(ChatMessageRecord chatMessageRecord)
            {
                SavedRecords.Add(chatMessageRecord);

                return SaveStatus;
            }
        }

        private sealed class ChatCallbackDouble : IChatManagerCallback
        {
            internal List<ChatMessage> ReceivedMessages { get; } = new List<ChatMessage>();

            public void OnChatMessageReceived(ChatMessage chatMessage)
            {
                ReceivedMessages.Add(chatMessage);
            }

            public void SubscribeToChatResult(OperationResult operationSubscribeToChatResult)
            {
                throw new NotSupportedException("The service does not answer through this callback.");
            }

            public void SendChatMessageResult(OperationSendChatMessageResult operationSendChatMessageResult)
            {
                throw new NotSupportedException("The service does not answer through this callback.");
            }

            public void ChatHeartbeatEcho(int sequence, string errorKey)
            {
                throw new NotSupportedException("Heartbeats are outside these tests.");
            }
        }
    }
}
