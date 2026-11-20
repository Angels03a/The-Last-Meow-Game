using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Characterization tests for session identity, channel ownership and room membership in the chat and in the
    /// match. They record what the registry does today so that a later refactor of the two identity checks cannot
    /// change it silently (BL-183). They do not unify the two policies: the chat and the match keep their own channel
    /// maps and their own membership rules. Guest players are not covered here because the registry has no entry
    /// point that creates a guest session yet (V3: guests may play, but they cannot use the chat).
    /// Each test sends at most one accepted chat message per player, because the chat rate limit reads the system
    /// clock and not the injected one.
    /// </summary>
    public class SessionRegistryChannelIdentityTests
    {
        private const int MinimumPlayerCount = 3;
        private const int FirstPlayerIndex = 0;
        private const int SecondPlayerIndex = 1;
        private const int ThirdPlayerIndex = 2;
        private const int OutsiderUserId = 99;
        private const int UnknownRoomId = 9999;
        private const string ChatChannelPrefix = "chat";
        private const string SharedChannelId = "shared";
        private const string ReplacementChatChannelId = "chat-replacement";
        private const string UnknownToken = "unknown-token";
        private const string ChatText = "hello";

        /// <summary>Shapes of caller that the identity check of the chat and of the match must treat alike.</summary>
        public enum CallerShape
        {
            OwnChannel,
            UnknownToken,
            NullToken,
            EmptyToken,
            NullChannel,
            EmptyChannel,
            ChannelOwnedByAnotherPlayer
        }

        /// <summary>A room member with a valid session and a free channel subscribes to the chat.</summary>
        [Fact]
        public void SubscribeToChat_WithRoomMember_AcceptsAndRegistersTheChannel()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);

            // Act
            string errorKey = scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub());

            // Assert
            Assert.Null(errorKey);
            Assert.Null(SendChat(scenario, caller, out ChatDelivery delivery));
            Assert.NotNull(delivery);
        }

        /// <summary>A missing or unknown token never subscribes to the chat.</summary>
        /// <param name="sessionToken">Token received from the client.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(UnknownToken)]
        public void SubscribeToChat_WithInvalidToken_RejectsWithInvalidSession(string sessionToken)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = new LobbyCaller(sessionToken, ChatChannelPrefix + FirstPlayerIndex);

            // Act
            string errorKey = scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub());

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
        }

        /// <summary>A caller without channel id never subscribes to the chat.</summary>
        /// <param name="channelId">Channel id received from the client.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SubscribeToChat_WithoutChannelId_RejectsWithInvalidSession(string channelId)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = new LobbyCaller(scenario.TokenOf(scenario.UserIds[FirstPlayerIndex]), channelId);

            // Act
            string errorKey = scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub());

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
        }

        /// <summary>A null callback is rejected as an invalid session and nothing is registered.</summary>
        [Fact]
        public void SubscribeToChat_WithNullCallback_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);

            // Act
            string errorKey = scenario.Registry.SubscribeToChat(caller, null);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
            Assert.Equal(ErrorKeys.Lobby.ChatNotSubscribed, SendChat(scenario, caller, out _));
        }

        /// <summary>A valid session without a room cannot subscribe to the chat.</summary>
        [Fact]
        public void SubscribeToChat_WithoutRoom_RejectsWithNotInRoom()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            string outsiderToken = scenario.Registry.StartRegisteredSession(OutsiderUserId, "Outsider", out _);
            LobbyCaller caller = new LobbyCaller(outsiderToken, ChatChannelPrefix + OutsiderUserId);

            // Act
            string errorKey = scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub());

            // Assert
            Assert.Equal(ErrorKeys.Lobby.NotInRoom, errorKey);
        }

        /// <summary>A channel already owned by another player cannot be taken, and the owner keeps it.</summary>
        [Fact]
        public void SubscribeToChat_WithChannelOfAnotherPlayer_RejectsAndKeepsTheOwnerSubscription()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller owner = SharedChannelCallerOf(scenario, FirstPlayerIndex);
            LobbyCaller intruder = SharedChannelCallerOf(scenario, SecondPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(owner, new ChatCallbackStub()));

            // Act
            string errorKey = scenario.Registry.SubscribeToChat(intruder, new ChatCallbackStub());

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
            Assert.Null(SendChat(scenario, owner, out _));
        }

        /// <summary>Sending without subscribing is rejected as not subscribed.</summary>
        [Fact]
        public void AcceptChatMessage_WithoutSubscription_RejectsWithChatNotSubscribed()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);

            // Act
            string errorKey = SendChat(scenario, caller, out ChatDelivery delivery);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatNotSubscribed, errorKey);
            Assert.Null(delivery);
        }

        /// <summary>A missing or unknown token never sends a chat message.</summary>
        /// <param name="sessionToken">Token received from the client.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(UnknownToken)]
        public void AcceptChatMessage_WithInvalidToken_RejectsWithInvalidSession(string sessionToken)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller subscribed = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(subscribed, new ChatCallbackStub()));
            LobbyCaller caller = new LobbyCaller(sessionToken, subscribed.ChannelId);

            // Act
            string errorKey = SendChat(scenario, caller, out ChatDelivery delivery);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
            Assert.Null(delivery);
        }

        /// <summary>Using the chat channel of another player is rejected as an invalid session.</summary>
        [Fact]
        public void AcceptChatMessage_WithChannelOfAnotherPlayer_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller owner = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(owner, new ChatCallbackStub()));
            LobbyCaller intruder = new LobbyCaller(
                scenario.TokenOf(scenario.UserIds[SecondPlayerIndex]), owner.ChannelId);

            // Act
            string errorKey = SendChat(scenario, intruder, out ChatDelivery delivery);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
            Assert.Null(delivery);
        }

        /// <summary>A channel other than the subscribed one is rejected as an invalid session.</summary>
        [Fact]
        public void AcceptChatMessage_WithAnotherChannelThanSubscribed_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller subscribed = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(subscribed, new ChatCallbackStub()));
            LobbyCaller other = new LobbyCaller(subscribed.SessionToken, ReplacementChatChannelId);

            // Act
            string errorKey = SendChat(scenario, other, out ChatDelivery delivery);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, errorKey);
            Assert.Null(delivery);
        }

        /// <summary>A message addressed to a room the sender is not in is rejected as not in the room.</summary>
        [Fact]
        public void AcceptChatMessage_ToAnotherRoom_RejectsWithNotInRoom()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub()));

            // Act
            string errorKey = SendChat(scenario, caller, UnknownRoomId, null, out ChatDelivery delivery);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.NotInRoom, errorKey);
            Assert.Null(delivery);
        }

        /// <summary>The validation error of the text is returned after identity and membership pass.</summary>
        [Fact]
        public void AcceptChatMessage_WithInvalidText_ReturnsTheValidationErrorWithoutDelivery()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub()));

            // Act
            string errorKey = SendChat(
                scenario, caller, scenario.RoomId, ErrorKeys.Lobby.ChatEmpty, out ChatDelivery delivery);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatEmpty, errorKey);
            Assert.Null(delivery);
        }

        /// <summary>An accepted message reaches only the subscribed members of the room, the sender included.</summary>
        [Fact]
        public void AcceptChatMessage_WhenAccepted_ReachesOnlySubscribedRoomMembers()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            ChatCallbackStub senderCallback = new ChatCallbackStub();
            ChatCallbackStub otherCallback = new ChatCallbackStub();
            LobbyCaller sender = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(sender, senderCallback));
            Assert.Null(scenario.Registry.SubscribeToChat(ChatCallerOf(scenario, SecondPlayerIndex), otherCallback));

            // Act
            string errorKey = SendChat(scenario, sender, out ChatDelivery delivery);

            // Assert
            Assert.Null(errorKey);
            Assert.Equal(scenario.UserIds[FirstPlayerIndex], delivery.Sender.UserId);
            Assert.Equal(2, delivery.Recipients.Count);
            Assert.Contains(senderCallback, delivery.Recipients);
            Assert.Contains(otherCallback, delivery.Recipients);
        }

        /// <summary>
        /// A new subscription replaces the old channel: the old channel id is rejected, closing it afterwards keeps
        /// the new one, and closing the current one leaves the player unsubscribed.
        /// </summary>
        [Fact]
        public void SubscribeToChat_WithReplacementChannel_InvalidatesTheOldChannelAndKeepsTheNewOne()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller original = ChatCallerOf(scenario, FirstPlayerIndex);
            LobbyCaller replacement = new LobbyCaller(original.SessionToken, ReplacementChatChannelId);
            Assert.Null(scenario.Registry.SubscribeToChat(original, new ChatCallbackStub()));
            Assert.Null(scenario.Registry.SubscribeToChat(replacement, new ChatCallbackStub()));

            // Act and assert: the replaced channel is rejected; closing it does not remove the current one.
            Assert.Equal(ErrorKeys.Common.InvalidSession, SendChat(scenario, original, out _));
            scenario.Registry.RemoveChatChannel(original.ChannelId);
            Assert.Null(SendChat(scenario, replacement, out _));

            // Act and assert: closing the current channel leaves the player unsubscribed.
            scenario.Registry.RemoveChatChannel(replacement.ChannelId);
            Assert.Equal(ErrorKeys.Lobby.ChatNotSubscribed, SendChat(scenario, replacement, out _));
        }

        /// <summary>Leaving the room removes the chat channel and the player can no longer subscribe.</summary>
        [Fact]
        public void LeaveRoom_AfterSubscribingToChat_RemovesTheChatChannel()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub()));

            // Act
            scenario.Registry.LeaveRoom(caller.SessionToken, out _);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatNotSubscribed, SendChat(scenario, caller, out _));
            Assert.Equal(ErrorKeys.Lobby.NotInRoom, scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub()));
        }

        /// <summary>Ending the session invalidates the token and the chat channel at once.</summary>
        [Fact]
        public void EndSession_AfterSubscribingToChat_RejectsFurtherMessagesAsInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            LobbyCaller caller = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub()));

            // Act
            scenario.Registry.EndSession(caller.SessionToken, out _);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, SendChat(scenario, caller, out ChatDelivery delivery));
            Assert.Null(delivery);
        }

        /// <summary>
        /// The same caller shape gets the same verdict from the chat and from the match: only an own, free channel
        /// with a valid token passes; every other shape is an invalid session. Each map is checked on its own.
        /// </summary>
        /// <param name="shape">Shape of the caller under test.</param>
        [Theory]
        [InlineData(CallerShape.OwnChannel)]
        [InlineData(CallerShape.UnknownToken)]
        [InlineData(CallerShape.NullToken)]
        [InlineData(CallerShape.EmptyToken)]
        [InlineData(CallerShape.NullChannel)]
        [InlineData(CallerShape.EmptyChannel)]
        [InlineData(CallerShape.ChannelOwnedByAnotherPlayer)]
        public void SubscribeToChatAndToMatch_WithTheSameCallerShape_ReachTheSameVerdict(CallerShape shape)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            int ownUserId = scenario.UserIds[FirstPlayerIndex];
            int otherUserId = scenario.UserIds[SecondPlayerIndex];
            LobbyCaller otherCaller = SharedChannelCallerOf(scenario, SecondPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(otherCaller, new ChatCallbackStub()));
            Assert.Null(scenario.Registry.SubscribeToMatch(
                otherCaller, scenario.MatchId, scenario.CallbackOf(otherUserId)).ErrorKey);
            LobbyCaller caller = BuildCaller(scenario, ownUserId, shape);
            string expectedErrorKey = shape == CallerShape.OwnChannel ? null : ErrorKeys.Common.InvalidSession;

            // Act
            string chatErrorKey = scenario.Registry.SubscribeToChat(caller, new ChatCallbackStub());
            string matchErrorKey = scenario.Registry.SubscribeToMatch(
                caller, scenario.MatchId, scenario.CallbackOf(ownUserId)).ErrorKey;

            // Assert
            Assert.Equal(expectedErrorKey, chatErrorKey);
            Assert.Equal(expectedErrorKey, matchErrorKey);
        }

        /// <summary>
        /// The chat and the match are independent channels of the same session: closing one leaves the other
        /// working, and subscribing to one never subscribes to the other.
        /// </summary>
        [Fact]
        public void ChatAndMatchChannels_OfTheSameSession_AreIndependent()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            int userId = scenario.UserIds[FirstPlayerIndex];
            LobbyCaller chatCaller = ChatCallerOf(scenario, FirstPlayerIndex);
            Assert.Null(scenario.Registry.SubscribeToChat(chatCaller, new ChatCallbackStub()));
            Assert.Equal(MatchErrorKeys.NotSubscribed, scenario.Place(userId, MatchCardKind.Safe).ErrorKey);
            scenario.SubscribeAll();

            // Act and assert: closing the match channel keeps the chat working and rejects match actions.
            Assert.True(scenario.CloseMatchChannelOf(userId));
            Assert.Null(SendChat(scenario, chatCaller, out _));
            Assert.Equal(MatchErrorKeys.NotSubscribed, scenario.Place(userId, MatchCardKind.Safe).ErrorKey);

            // Act and assert: the match subscription is restored and verified before the chat is closed.
            Assert.Null(scenario.Subscribe(userId).ErrorKey);
            Assert.Null(scenario.Place(userId, MatchCardKind.Safe).ErrorKey);

            // Act and assert: closing the chat leaves it unsubscribed, and the match channel keeps working
            // without subscribing to the match again (the turn comes back to the same player after one lap).
            scenario.Registry.RemoveChatChannel(chatCaller.ChannelId);
            Assert.Equal(ErrorKeys.Lobby.ChatNotSubscribed, SendChat(scenario, chatCaller, out _));
            Assert.Null(scenario.Place(scenario.UserIds[SecondPlayerIndex], MatchCardKind.Safe).ErrorKey);
            Assert.Null(scenario.Place(scenario.UserIds[ThirdPlayerIndex], MatchCardKind.Safe).ErrorKey);
            Assert.Null(scenario.Place(userId, MatchCardKind.Safe).ErrorKey);
        }

        /// <summary>A closed match channel rejects actions as not subscribed until a new subscription.</summary>
        [Fact]
        public void PlaceCard_AfterTheMatchChannelClosed_RejectsWithNotSubscribed()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.UserIds[FirstPlayerIndex];
            Assert.True(scenario.CloseMatchChannelOf(userId));

            // Act
            MatchDelivery delivery = scenario.Place(userId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.NotSubscribed, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
        }

        /// <summary>A player who is still in the match room keeps a valid identity for both channels.</summary>
        [Fact]
        public void MatchAndChat_ForAnActiveMatchPlayer_AcceptTheSameSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.UserIds[ThirdPlayerIndex];
            LobbyCaller chatCaller = ChatCallerOf(scenario, ThirdPlayerIndex);

            // Act
            string chatErrorKey = scenario.Registry.SubscribeToChat(chatCaller, new ChatCallbackStub());
            MatchDelivery matchDelivery = scenario.Subscribe(userId);

            // Assert
            Assert.Null(chatErrorKey);
            Assert.Null(matchDelivery.ErrorKey);
            Assert.Equal(chatCaller.SessionToken, scenario.TokenOf(userId));
        }

        private static LobbyCaller ChatCallerOf(MatchPlayScenario scenario, int playerIndex)
        {
            int userId = scenario.UserIds[playerIndex];

            return new LobbyCaller(scenario.TokenOf(userId), ChatChannelPrefix + userId);
        }

        private static LobbyCaller SharedChannelCallerOf(MatchPlayScenario scenario, int playerIndex)
        {
            return new LobbyCaller(scenario.TokenOf(scenario.UserIds[playerIndex]), SharedChannelId);
        }

        private static LobbyCaller BuildCaller(MatchPlayScenario scenario, int userId, CallerShape shape)
        {
            string token = scenario.TokenOf(userId);
            string channelId = "own-" + userId;
            LobbyCaller caller;

            switch (shape)
            {
                case CallerShape.UnknownToken:
                    caller = new LobbyCaller(UnknownToken, channelId);
                    break;
                case CallerShape.NullToken:
                    caller = new LobbyCaller(null, channelId);
                    break;
                case CallerShape.EmptyToken:
                    caller = new LobbyCaller(string.Empty, channelId);
                    break;
                case CallerShape.NullChannel:
                    caller = new LobbyCaller(token, null);
                    break;
                case CallerShape.EmptyChannel:
                    caller = new LobbyCaller(token, string.Empty);
                    break;
                case CallerShape.ChannelOwnedByAnotherPlayer:
                    caller = new LobbyCaller(token, SharedChannelId);
                    break;
                default:
                    caller = new LobbyCaller(token, channelId);
                    break;
            }

            return caller;
        }

        private static string SendChat(MatchPlayScenario scenario, LobbyCaller caller, out ChatDelivery delivery)
        {
            return SendChat(scenario, caller, scenario.RoomId, null, out delivery);
        }

        private static string SendChat(
            MatchPlayScenario scenario,
            LobbyCaller caller,
            int roomId,
            string validationErrorKey,
            out ChatDelivery delivery)
        {
            ChatSubmission submission = new ChatSubmission
            {
                RoomId = roomId,
                ClientMessageId = Guid.NewGuid(),
                Text = ChatText,
                ValidationErrorKey = validationErrorKey
            };

            return scenario.Registry.AcceptChatMessage(caller, submission, out delivery);
        }

        /// <summary>Chat callback that ignores everything; the tests read the registry results instead.</summary>
        private sealed class ChatCallbackStub : IChatManagerCallback
        {
            public void SubscribeToChatResult(OperationResult operationSubscribeToChatResult)
            {
            }

            public void SendChatMessageResult(OperationSendChatMessageResult operationSendChatMessageResult)
            {
            }

            public void OnChatMessageReceived(ChatMessage chatMessage)
            {
            }

            public void ChatHeartbeatEcho(int sequence, string errorKey)
            {
            }
        }
    }
}
