using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de SessionRegistry.ChangeCardsPerPlayer.</summary>
    public class SessionRegistryCardsPerPlayerTests
    {
        private const string SecondChannelId = "lobby2";

        private const string ThirdChannelId = "lobby3";

        private const int SecondUserId = 2;

        private const int ThirdUserId = 3;

        private const int CreatorUserId = 1;

        private const int DefaultCardsPerPlayer = 4;

        private const int EightCardsPerPlayer = 8;

        /// <summary>Verifica ChangeCardsPerPlayer_WithDifferentValue_SavesValueAndResetsEveryoneToNotReady.</summary>
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

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId,
                scenario.RoomId,
                EightCardsPerPlayer);

            // Assert
            Assert.Equal(3, allReady.Room.Start.ReadyUserIds.Count);
            Assert.True(allReady.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(EightCardsPerPlayer, change.Room.Start.CardsPerPlayerCount);
            Assert.Empty(change.Room.Start.ReadyUserIds);
            Assert.False(change.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.True(change.Room.Start.Version > allReady.Room.Start.Version);
        }

        /// <summary>
        /// Verifica ChangeCardsPerPlayer_WithDifferentValue_ReturnsOtherPlayersCallbacksWithoutInvokingThem.
        /// </summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValue_ReturnsOtherPlayersCallbacksWithoutInvokingThem()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId,
                scenario.RoomId,
                EightCardsPerPlayer);

            // Assert
            Assert.Equal(2, change.RecipientCallbacks.Count);
            Assert.Contains(secondCallback, change.RecipientCallbacks);
            Assert.Contains(thirdCallback, change.RecipientCallbacks);
            Assert.Equal(0, secondCallback.CallCount);
            Assert.Equal(0, thirdCallback.CallCount);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithSameValue_KeepsReadyAndSendsNoNotifications.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithSameValue_KeepsReadyAndSendsNoNotifications()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyMembershipChange ready = LobbyReadinessHelper.MarkReady(
                scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId,
                scenario.RoomId,
                DefaultCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(DefaultCardsPerPlayer, change.Room.Start.CardsPerPlayerCount);
            Assert.Contains(CreatorUserId, change.Room.Start.ReadyUserIds);
            Assert.Equal(ready.Room.Start.Version, change.Room.Start.Version);
            Assert.Empty(change.RecipientCallbacks);
            Assert.Equal(0, secondCallback.CallCount);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithInvalidSession_RejectsRequest.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangeCardsPerPlayer_WithInvalidSession_RejectsRequest(string token)
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                token,
                LobbyReadinessHelper.CreatorChannelId,
                scenario.RoomId,
                EightCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Null(change.Room);
        }

        /// <summary>Verifica ChangeCardsPerPlayer_WithPlayerOutsideLobby_RejectsRequest.</summary>
        [Fact]
        public void ChangeCardsPerPlayer_WithPlayerOutsideLobby_RejectsRequest()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            string outsiderToken = scenario.Registry.StartSession(9, "Outsider", out SessionEnd replaced);

            // Act
            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                outsiderToken, "outsider", scenario.RoomId, EightCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
            Assert.Null(change.Room);
        }
    }
}
