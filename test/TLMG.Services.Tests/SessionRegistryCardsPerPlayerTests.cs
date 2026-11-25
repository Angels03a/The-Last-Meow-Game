using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of SessionRegistry.ChangeCardsPerPlayer. The registry does not validate the value (the lobby service
    /// does, and its tests live in LobbyServiceCardsPerPlayerTests); it applies a real change, resets readiness,
    /// tells the other players and rejects callers who are not in the room.
    /// </summary>
    public class SessionRegistryCardsPerPlayerTests
    {
        private const string SecondChannelId = "lobby2";
        private const string ThirdChannelId = "lobby3";
        private const int SecondUserId = 2;
        private const int ThirdUserId = 3;
        private const int OutsiderUserId = 9;
        private const int CreatorUserId = 1;
        private const int DefaultCardsPerPlayer = 4;
        private const int EightCardsPerPlayer = 8;
        private const int ThreePlayers = 3;
        private const int OtherPlayers = 2;
        private const int NoCalls = 0;

        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValue_SavesValueAndResetsEveryoneToNotReady()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string secondToken = LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, null);
            string thirdToken = LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, null);
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyReadinessHelper.MarkReady(scenario, secondToken, SecondChannelId);
            LobbyMembershipChange allReady = LobbyReadinessHelper.MarkReady(scenario, thirdToken, ThirdChannelId);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller, scenario.RoomId, EightCardsPerPlayer);

            // Assert
            Assert.Equal(ThreePlayers, allReady.Room.Start.ReadyUserIds.Count);
            Assert.True(allReady.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(EightCardsPerPlayer, change.Room.Start.CardsPerPlayerCount);
            Assert.Empty(change.Room.Start.ReadyUserIds);
            Assert.False(change.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.True(change.Room.Start.Version > allReady.Room.Start.Version);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValue_ReturnsOtherPlayersCallbacksWithoutInvokingThem()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller, scenario.RoomId, EightCardsPerPlayer);

            // Assert
            Assert.Equal(OtherPlayers, change.RecipientCallbacks.Count);
            Assert.Contains(secondCallback, change.RecipientCallbacks);
            Assert.Contains(thirdCallback, change.RecipientCallbacks);
            Assert.Equal(NoCalls, secondCallback.CallCount);
            Assert.Equal(NoCalls, thirdCallback.CallCount);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithSameValue_KeepsReadyAndSendsNoNotifications()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyMembershipChange ready = LobbyReadinessHelper.MarkReady(
                scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller, scenario.RoomId, DefaultCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(DefaultCardsPerPlayer, change.Room.Start.CardsPerPlayerCount);
            Assert.Contains(CreatorUserId, change.Room.Start.ReadyUserIds);
            Assert.Equal(ready.Room.Start.Version, change.Room.Start.Version);
            Assert.Empty(change.RecipientCallbacks);
            Assert.Equal(NoCalls, secondCallback.CallCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangeCardsPerPlayer_WithInvalidSession_RejectsWithoutChangingTheRoom(string token)
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            SessionChannelCaller invalidCaller = new SessionChannelCaller(token, LobbyReadinessHelper.CreatorChannelId);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyMembershipChange before = RepeatNotReady(scenario, creatorCaller);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                invalidCaller, scenario.RoomId, EightCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Null(change.Room);
            AssertRoomUnchanged(before, RepeatNotReady(scenario, creatorCaller));
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithPlayerOutsideLobby_RejectsWithoutChangingTheRoom()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string outsiderToken = scenario.Registry.StartRegisteredSession(OutsiderUserId, "Outsider", out _);
            SessionChannelCaller outsiderCaller = new SessionChannelCaller(outsiderToken, "outsider");
            SessionChannelCaller creatorCaller = new SessionChannelCaller(scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyMembershipChange before = RepeatNotReady(scenario, creatorCaller);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                outsiderCaller, scenario.RoomId, EightCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
            Assert.Null(change.Room);
            AssertRoomUnchanged(before, RepeatNotReady(scenario, creatorCaller));
        }

        /// <summary>
        /// A "not ready" request from a player who is already not ready. It is a repeat without changes, not a
        /// read: AssertRoomUnchanged checks that it kept the version and notified nobody.
        /// </summary>
        private static LobbyMembershipChange RepeatNotReady(LobbyScenario scenario, SessionChannelCaller notReadyMemberCaller)
        {
            return scenario.Registry.ChangePlayerReadiness(notReadyMemberCaller, scenario.RoomId, false);
        }

        private static void AssertRoomUnchanged(LobbyMembershipChange before, LobbyMembershipChange repeated)
        {
            Assert.Equal(before.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Equal(before.Room.Start.CardsPerPlayerCount, repeated.Room.Start.CardsPerPlayerCount);
            Assert.Equal(DefaultCardsPerPlayer, repeated.Room.Start.CardsPerPlayerCount);
            Assert.Empty(repeated.RecipientCallbacks);
        }
    }
}
