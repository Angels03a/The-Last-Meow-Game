using System;
using System.Collections.Generic;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    public class LobbyServiceCardsPerPlayerTests
    {
        private const string SecondChannelId = "lobby2";
        private const string ThirdChannelId = "lobby3";
        private const int SecondUserId = 2;
        private const int ThirdUserId = 3;
        private const int DefaultCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int OtherPlayersCount = 2;
        private const int NoCalls = 0;
        private const int OneCall = 1;
        private const string InvalidSessionKey = "Common_InvalidSessionError";
        private const string NotInRoomKey = "Lobby_NotInRoomError";
        private const string UnexpectedKey = "Common_UnexpectedError";
        private const string NotValidKey = "Lobby_CardsPerPlayerNotValidError";

        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValidValue_ReturnsNewValueAndNoticesForOthersWithoutSending()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Null(actual.OperationResult.ErrorKey);
            Assert.Equal(SixCardsPerPlayer, actual.OperationResult.Room.CardsPerPlayerCount);
            Assert.Equal(OtherPlayersCount, actual.Notifications.Count);
            Assert.Equal(NoCalls, secondCallback.CallCount);
            Assert.Equal(NoCalls, thirdCallback.CallCount);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WhenNoticesRun_SendsNewRoomToEachOtherPlayer()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));
            RunNotifications(actual.Notifications);

            // Assert
            Assert.Equal(OneCall, secondCallback.CallCount);
            Assert.Equal(OneCall, thirdCallback.CallCount);
            Assert.Equal(SixCardsPerPlayer, secondCallback.LastLobbyRoom.CardsPerPlayerCount);
            Assert.Equal(SixCardsPerPlayer, thirdCallback.LastLobbyRoom.CardsPerPlayerCount);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValue_LeavesEveryPlayerNotReady()
        {
            LobbyScenario scenario = new LobbyScenario();
            string secondToken = LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, null);
            string thirdToken = LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, null);
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyReadinessHelper.MarkReady(scenario, secondToken, SecondChannelId);
            LobbyReadinessHelper.MarkReady(scenario, thirdToken, ThirdChannelId);
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller secondCaller = new LobbyCaller(secondToken, SecondChannelId);

            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    secondCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(3, actual.OperationResult.Room.Players.Count);
            Assert.All(actual.OperationResult.Room.Players, player => Assert.False(player.IsReady));
            Assert.False(actual.OperationResult.Room.CountdownEndsAtUtc.HasValue);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithSameValue_SucceedsWithoutResettingReady()
        {
            LobbyScenario scenario = new LobbyScenario();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, new LobbyCallbackStub());
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, DefaultCardsPerPlayer));

            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(DefaultCardsPerPlayer, actual.OperationResult.Room.CardsPerPlayerCount);
            Assert.Single(actual.OperationResult.Room.Players, player => player.IsReady);
            Assert.Empty(actual.Notifications);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(-4)]
        public void ChangeCardsPerPlayer_WithInvalidValue_ReturnsErrorAndKeepsRoomUnchanged(int cardsPerPlayerCount)
        {
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, cardsPerPlayerCount));
            LobbyMembershipChange roomAfter = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller,
                scenario.RoomId,
                DefaultCardsPerPlayer);

            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
            Assert.Equal(DefaultCardsPerPlayer, roomAfter.Room.Start.CardsPerPlayerCount);
            Assert.Empty(roomAfter.RecipientCallbacks);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangeCardsPerPlayer_WithInvalidSession_ReturnsInvalidSessionError(string token)
        {
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller invalidCaller = new LobbyCaller(token, LobbyReadinessHelper.CreatorChannelId);

            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    invalidCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithPlayerOutsideLobby_ReturnsNotInRoomError()
        {
            LobbyScenario scenario = new LobbyScenario();
            string outsiderToken = scenario.Registry.StartSession(9, "Outsider", out SessionEnd replaced);
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller outsiderCaller = new LobbyCaller(outsiderToken, "outsider");

            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    outsiderCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotInRoomKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithNullRequest_ReturnsInvalidSessionError()
        {
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(creatorCaller, null);

            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WhenRegistryFails_ThrowsInvalidOperationException()
        {
            LobbyService service = new LobbyService(new FailingSessionRegistryStub());
            LobbyCaller caller = new LobbyCaller("token", LobbyReadinessHelper.CreatorChannelId);

            Assert.Throws<InvalidOperationException>(() =>
                service.ChangeCardsPerPlayer(
                    caller,
                    CreateRequest(1, SixCardsPerPlayer)));
        }

        private static void RunNotifications(IReadOnlyList<Action> notifications)
        {
            foreach (Action notification in notifications)
            {
                notification();
            }
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