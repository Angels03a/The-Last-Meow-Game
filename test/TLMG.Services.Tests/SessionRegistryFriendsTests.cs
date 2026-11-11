using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de los métodos de amigos de SessionRegistry usados por las solicitudes de amistad.</summary>
    public class SessionRegistryFriendsTests
    {
        /// <summary>Verifica FindFriendsCallbackOfUser_WithOnlineUser_ReturnsItsCallback.</summary>
        [Fact]
        public void FindFriendsCallbackOfUser_WithOnlineUser_ReturnsItsCallback()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            object actual = scenario.Registry.FindFriendsCallbackOfUser(FriendsScenario.AlphaUserId);

            // Assert
            Assert.Same(scenario.AlphaCallback, actual);
        }

        /// <summary>Verifica FindFriendsCallbackOfUser_WithOfflineOrUnknownUser_ReturnsNull.</summary>
        [Theory]
        [InlineData(FriendsScenario.CharlieUserId)]
        [InlineData(99)]
        public void FindFriendsCallbackOfUser_WithOfflineOrUnknownUser_ReturnsNull(int userId)
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            object actual = scenario.Registry.FindFriendsCallbackOfUser(userId);

            // Assert
            Assert.Null(actual);
        }

        /// <summary>Verifica AddFriendshipToOnlineChannels_WithBothOnline_ReturnsBothCallbacks.</summary>
        [Fact]
        public void AddFriendshipToOnlineChannels_WithBothOnline_ReturnsBothCallbacks()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            FriendsAddition actual = scenario.Registry.AddFriendshipToOnlineChannels(
                FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId);

            // Assert
            Assert.True(actual.IsFirstOnline);
            Assert.True(actual.IsSecondOnline);
            Assert.Same(scenario.AlphaCallback, actual.FirstCallback);
            Assert.Same(scenario.BravoCallback, actual.SecondCallback);
            Assert.Equal(0, scenario.AlphaCallback.OtherCallCount);
            Assert.Equal(0, scenario.BravoCallback.OtherCallCount);
        }

        /// <summary>Verifica AddFriendshipToOnlineChannels_WithOneOffline_ReturnsNullCallbackForIt.</summary>
        [Fact]
        public void AddFriendshipToOnlineChannels_WithOneOffline_ReturnsNullCallbackForIt()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();

            // Act
            FriendsAddition actual = scenario.Registry.AddFriendshipToOnlineChannels(
                FriendsScenario.AlphaUserId, FriendsScenario.CharlieUserId);

            // Assert
            Assert.True(actual.IsFirstOnline);
            Assert.False(actual.IsSecondOnline);
            Assert.Null(actual.SecondCallback);
        }

        /// <summary>Verifica AddFriendshipToOnlineChannels_AfterAdding_RefreshesPresenceRecipients.</summary>
        [Fact]
        public void AddFriendshipToOnlineChannels_AfterAdding_RefreshesPresenceRecipients()
        {
            // Arrange
            FriendsScenario scenario = new FriendsScenario();
            scenario.Registry.AddFriendshipToOnlineChannels(FriendsScenario.AlphaUserId, FriendsScenario.BravoUserId);

            // Act
            bool wasEnded = scenario.Registry.EndSession(scenario.BravoToken, out SessionEnd sessionEnd);

            // Assert
            Assert.True(wasEnded);
            Assert.Contains(scenario.AlphaCallback, sessionEnd.PresenceRecipientCallbacks);
        }
    }
}
