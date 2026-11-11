using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de LobbyService.ChangeCardsPerPlayer.</summary>
    public class LobbyServiceCardsPerPlayerTests
    {
        private const string SecondChannelId = "lobby2";

        private const string ThirdChannelId = "lobby3";

        private const int SecondUserId = 2;

        private const int ThirdUserId = 3;

        private const int DefaultCardsPerPlayer = 4;

        private const int SixCardsPerPlayer = 6;

        private const string InvalidSessionKey = "Common_InvalidSessionError";

        private const string NotInRoomKey = "Lobby_NotInRoomError";

        private const string UnexpectedKey = "Common_UnexpectedError";

        private const string NotValidKey = "Lobby_CardsPerPlayerNotValidError";

        /// <summary>Verifica ChangeCardsPerPlayer_WithDifferentValidValue_ReturnsNewValueAndNotifiesOthers.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValidValue_ReturnsNewValueAndNotifiesOthers()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    scenario.CreatorToken,
                    LobbyReadinessHelper.CreatorChannelId,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.ErrorKey);
            Assert.Equal(SixCardsPerPlayer, actual.OperationResult.Room.CardsPerPlayerCount);
            Assert.Equal(2, actual.Notifications.Count);
            Assert.Equal(SixCardsPerPlayer, actual.Notifications[0].LobbyRoom.CardsPerPlayerCount);
            Assert.Equal(0, secondCallback.CallCount);
            Assert.Equal(0, thirdCallback.CallCount);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithDifferentValue_LeavesEveryPlayerNotReady.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValue_LeavesEveryPlayerNotReady()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string secondToken = LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, null);
            string thirdToken = LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, null);
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyReadinessHelper.MarkReady(scenario, secondToken, SecondChannelId);
            LobbyReadinessHelper.MarkReady(scenario, thirdToken, ThirdChannelId);
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    secondToken, SecondChannelId, CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(3, actual.OperationResult.Room.Players.Count);
            Assert.All(actual.OperationResult.Room.Players, player => Assert.False(player.IsReady));
            Assert.False(actual.OperationResult.Room.CountdownEndsAtUtc.HasValue);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithSameValue_SucceedsWithoutResettingReady.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithSameValue_SucceedsWithoutResettingReady()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, new LobbyCallbackStub());
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    scenario.CreatorToken,
                    LobbyReadinessHelper.CreatorChannelId,
                    CreateRequest(scenario.RoomId, DefaultCardsPerPlayer));

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(DefaultCardsPerPlayer, actual.OperationResult.Room.CardsPerPlayerCount);
            Assert.Single(actual.OperationResult.Room.Players, player => player.IsReady);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithInvalidValue_ReturnsErrorAndKeepsRoomUnchanged.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(-4)]
        public void ChangeCardsPerPlayer_WithInvalidValue_ReturnsErrorAndKeepsRoomUnchanged(int cardsPerPlayerCount)
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    scenario.CreatorToken,
                    LobbyReadinessHelper.CreatorChannelId,
                    CreateRequest(scenario.RoomId, cardsPerPlayerCount));
            LobbyMembershipChange roomAfter = scenario.Registry.ChangeCardsPerPlayer(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId,
                scenario.RoomId,
                DefaultCardsPerPlayer);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
            Assert.Equal(DefaultCardsPerPlayer, roomAfter.Room.Start.CardsPerPlayerCount);
            Assert.Empty(roomAfter.RecipientCallbacks);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithInvalidSession_ReturnsInvalidSessionError.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangeCardsPerPlayer_WithInvalidSession_ReturnsInvalidSessionError(string token)
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    token,
                    LobbyReadinessHelper.CreatorChannelId,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithPlayerOutsideLobby_ReturnsNotInRoomError.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithPlayerOutsideLobby_ReturnsNotInRoomError()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string outsiderToken = scenario.Registry.StartSession(9, "Outsider", out SessionEnd replaced);
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    outsiderToken, "outsider", CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotInRoomKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithNullRequest_ReturnsInvalidSessionError.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithNullRequest_ReturnsInvalidSessionError()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId, null);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WhenRegistryFails_ReturnsUnexpectedError.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WhenRegistryFails_ReturnsUnexpectedError()
        {
            // Arrange
            LobbyService service = new LobbyService(new FailingSessionRegistryStub());

            // Act
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> actual =
                service.ChangeCardsPerPlayer(
                    "token", LobbyReadinessHelper.CreatorChannelId, CreateRequest(1, SixCardsPerPlayer));

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(UnexpectedKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        private static OperationChangeCardsPerPlayerRequest CreateRequest(int roomId, int cardsPerPlayerCount)
        {
            OperationChangeCardsPerPlayerRequest request = new OperationChangeCardsPerPlayerRequest
            {
                RoomId = roomId,
                CardsPerPlayerCount = cardsPerPlayerCount
            };

            return request;
        }
    }
}
