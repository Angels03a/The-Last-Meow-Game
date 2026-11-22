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
        private const int CountdownSeconds = 3;
        private const string InvalidSessionKey = "Common_InvalidSessionError";
        private const string NotInRoomKey = "Lobby_NotInRoomError";
        private const string UnexpectedKey = "Common_UnexpectedError";
        private const string NotValidKey = "Lobby_CardsPerPlayerNotValidError";

        [Fact]
        public void LeaveLobby_WhenAlreadyOutsideRoom_ReturnsSuccessWithEmptyKey()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, "Creator", out SessionEnd replaced);
            LobbyService service = new LobbyService(registry);

            // Act
            OperationResult result = service.LeaveLobby(token).OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
        }

        [Fact]
        public void CreateRoom_WithValidSession_ReturnsSuccessWithEmptyKey()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, "Creator", out SessionEnd replaced);
            LobbyService service = new LobbyService(registry);

            // Act
            OperationLobbyRoomResult result = service.CreateRoom(new SessionChannelCaller(token, "creator"), null)
                .OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.NotNull(result.Room);
        }

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
            SessionChannelCaller creatorCaller = new SessionChannelCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(string.Empty, actual.OperationResult.ErrorKey);
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
            SessionChannelCaller creatorCaller = new SessionChannelCaller(
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
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string secondToken = LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, null);
            string thirdToken = LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, null);
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyReadinessHelper.MarkReady(scenario, secondToken, SecondChannelId);
            LobbyReadinessHelper.MarkReady(scenario, thirdToken, ThirdChannelId);
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller secondCaller = new SessionChannelCaller(secondToken, SecondChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    secondCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.Equal(3, actual.OperationResult.Room.Players.Count);
            Assert.All(actual.OperationResult.Room.Players, player => Assert.False(player.IsReady));
            Assert.False(actual.OperationResult.Room.CountdownEndsAtUtc.HasValue);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithSameValue_SucceedsWithoutResettingReady()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, new LobbyCallbackStub());
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, DefaultCardsPerPlayer));

            // Assert
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
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);
            LobbyMembershipChange roomBefore = scenario.Registry.ChangePlayerReadiness(
                creatorCaller, scenario.RoomId, false);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    creatorCaller,
                    CreateRequest(scenario.RoomId, cardsPerPlayerCount));

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotValidKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
            // "Not ready" again for a player who is not ready changes nothing; it only shows the room kept its state.
            LobbyMembershipChange roomAfter = scenario.Registry.ChangePlayerReadiness(
                creatorCaller, scenario.RoomId, false);
            Assert.Equal(roomBefore.Room.Start.Version, roomAfter.Room.Start.Version);
            Assert.Equal(DefaultCardsPerPlayer, roomAfter.Room.Start.CardsPerPlayerCount);
            Assert.Empty(roomAfter.RecipientCallbacks);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangeCardsPerPlayer_WithInvalidSession_ReturnsInvalidSessionError(string token)
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller invalidCaller = new SessionChannelCaller(token, LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    invalidCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithPlayerOutsideLobby_ReturnsNotInRoomError()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string outsiderToken = scenario.Registry.StartRegisteredSession(9, "Outsider", out SessionEnd replaced);
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller outsiderCaller = new SessionChannelCaller(outsiderToken, "outsider");

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(
                    outsiderCaller,
                    CreateRequest(scenario.RoomId, SixCardsPerPlayer));

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(NotInRoomKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithNullRequest_ReturnsInvalidSessionError()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangeCardsPerPlayer(creatorCaller, null);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WhenRegistryFails_ThrowsInvalidOperationException()
        {
            // Arrange
            LobbyService service = new LobbyService(new FailingSessionRegistryStub());
            SessionChannelCaller caller = new SessionChannelCaller("token", LobbyReadinessHelper.CreatorChannelId);

            // Act
            Action change = () => service.ChangeCardsPerPlayer(caller, CreateRequest(1, SixCardsPerPlayer));

            // Assert
            Assert.Throws<InvalidOperationException>(change);
        }

        [Fact]
        public void ChangePlayerReadiness_WithNullRequest_RejectsWithInvalidSessionWithoutNotifications()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual =
                service.ChangePlayerReadiness(creatorCaller, null);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void ChangePlayerReadiness_WhenTheLastPlayerBecomesReady_ReturnsTheCountdownAndNotifiesTheRoom()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            string secondToken = LobbyReadinessHelper.AddPlayer(
                scenario, SecondUserId, SecondChannelId, secondCallback);
            string thirdToken = LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyReadinessHelper.MarkReady(scenario, secondToken, SecondChannelId);
            LobbyService service = new LobbyService(scenario.Registry);

            // Act
            ResultWithNotification<OperationLobbyRoomResult> actual = service.ChangePlayerReadiness(
                new SessionChannelCaller(thirdToken, ThirdChannelId),
                new OperationChangePlayerReadinessRequest { RoomId = scenario.RoomId, IsReady = true });
            RunNotifications(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.True(actual.OperationResult.Room.CountdownEndsAtUtc.HasValue);
            Assert.All(actual.OperationResult.Room.Players, player => Assert.True(player.IsReady));
            Assert.Equal(OneCall, secondCallback.CallCount);
            Assert.Equal(OneCall, thirdCallback.CallCount);
            Assert.Equal(actual.OperationResult.Room.CountdownId, secondCallback.LastLobbyRoom.CountdownId);
            Assert.Equal(actual.OperationResult.Room.CountdownId, thirdCallback.LastLobbyRoom.CountdownId);
        }

        [Fact]
        public void RequestMatchStart_WithNullRequest_RejectsWithInvalidSessionWithoutNotifications()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationRequestMatchStartResult> actual =
                service.RequestMatchStart(creatorCaller, null);

            // Assert
            Assert.False(actual.OperationResult.IsSuccess);
            Assert.Equal(InvalidSessionKey, actual.OperationResult.ErrorKey);
            Assert.Null(actual.OperationResult.Room);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void RequestMatchStart_BeforeTheCountdownEnds_CreatesNoMatchAndAsksToRetryInOneSecond()
        {
            // Arrange
            ManualUtcClock clock = new ManualUtcClock();
            RoomWithThreeReadyPlayers room = new RoomWithThreeReadyPlayers(clock);
            LobbyService service = new LobbyService(room.Scenario.Registry);
            clock.Advance(TimeSpan.FromSeconds(CountdownSeconds - 1));

            // Act
            ResultWithNotification<OperationRequestMatchStartResult> actual = service.RequestMatchStart(
                room.CreatorCaller,
                new OperationMatchStartRequest { RoomId = room.Scenario.RoomId, CountdownId = room.CountdownId });

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.False(actual.OperationResult.Room.MatchId.HasValue);
            Assert.Equal(1, actual.OperationResult.RetryAfterSeconds);
            Assert.Empty(actual.Notifications);
        }

        [Fact]
        public void RequestMatchStart_AfterTheCountdownEnds_AuthorizesTheMatchAndNotifiesTheOtherPlayers()
        {
            // Arrange
            ManualUtcClock clock = new ManualUtcClock();
            RoomWithThreeReadyPlayers room = new RoomWithThreeReadyPlayers(clock);
            LobbyService service = new LobbyService(room.Scenario.Registry);
            clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            ResultWithNotification<OperationRequestMatchStartResult> actual = service.RequestMatchStart(
                room.CreatorCaller,
                new OperationMatchStartRequest { RoomId = room.Scenario.RoomId, CountdownId = room.CountdownId });
            RunNotifications(actual.Notifications);

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Guid matchId = actual.OperationResult.Room.MatchId.Value;
            Assert.True(actual.OperationResult.Room.InGame);
            Assert.Equal(0, actual.OperationResult.RetryAfterSeconds);
            Assert.Equal(matchId, room.SecondCallback.LastLobbyRoom.MatchId);
            Assert.Equal(matchId, room.ThirdCallback.LastLobbyRoom.MatchId);
        }

        [Fact]
        public void RequestMatchStart_WithPlayersNotReady_CreatesNoMatchAndDoesNotAskToRetry()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, new LobbyCallbackStub());
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, new LobbyCallbackStub());
            LobbyService service = new LobbyService(scenario.Registry);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            ResultWithNotification<OperationRequestMatchStartResult> actual = service.RequestMatchStart(
                creatorCaller, new OperationMatchStartRequest { RoomId = scenario.RoomId, CountdownId = 0 });

            // Assert
            Assert.True(actual.OperationResult.IsSuccess);
            Assert.False(actual.OperationResult.Room.MatchId.HasValue);
            Assert.Equal(0, actual.OperationResult.RetryAfterSeconds);
            Assert.Empty(actual.Notifications);
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

        /// <summary>A room of three players who are all ready, with a manual clock and a known countdown.</summary>
        private sealed class RoomWithThreeReadyPlayers
        {
            internal RoomWithThreeReadyPlayers(ManualUtcClock clock)
            {
                Scenario = new LobbyScenario(new SessionRegistry(clock.ReadUtcNow));
                SecondCallback = new LobbyCallbackStub();
                ThirdCallback = new LobbyCallbackStub();
                string secondToken = LobbyReadinessHelper.AddPlayer(
                    Scenario, SecondUserId, SecondChannelId, SecondCallback);
                string thirdToken = LobbyReadinessHelper.AddPlayer(
                    Scenario, ThirdUserId, ThirdChannelId, ThirdCallback);
                LobbyReadinessHelper.MarkReady(Scenario, Scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
                LobbyReadinessHelper.MarkReady(Scenario, secondToken, SecondChannelId);
                CountdownId = LobbyReadinessHelper.MarkReady(Scenario, thirdToken, ThirdChannelId)
                    .Room.Start.CountdownId;
                CreatorCaller = new SessionChannelCaller(Scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            }

            internal LobbyScenario Scenario { get; }

            internal LobbyCallbackStub SecondCallback { get; }

            internal LobbyCallbackStub ThirdCallback { get; }

            internal long CountdownId { get; }

            internal SessionChannelCaller CreatorCaller { get; }
        }
    }
}
