using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de SessionRegistry al reemplazar una sesión y al terminar un canal de amigos.</summary>
    public class SessionRegistryReplacedSessionTests
    {
        private const int PlayerUserId = 1;
        private const string PlayerNickname = "Player";
        private const string OldFriendsChannelId = "oldFriends";
        private const string NewFriendsChannelId = "newFriends";
        private const string UnknownFriendsChannelId = "unknown";

        /// <summary>Verifica StartSession_WithRepeatedAccount_ReturnsFriendsCallbackOfReplacedSession.</summary>
        [Fact]
        public void StartSession_WithRepeatedAccount_ReturnsFriendsCallbackOfReplacedSession()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            FriendsCallbackStub oldCallback = new FriendsCallbackStub();
            StartSubscribedSession(registry, oldCallback, OldFriendsChannelId);

            // Act
            registry.StartRegisteredSession(PlayerUserId, PlayerNickname, out SessionEnd replacedSession);

            // Assert
            Assert.Same(oldCallback, replacedSession.FriendsCallback);
        }

        /// <summary>Verifica StartSession_WithReplacedSessionWithoutFriendsChannel_ReturnsNullCallback.</summary>
        [Fact]
        public void StartSession_WithReplacedSessionWithoutFriendsChannel_ReturnsNullCallback()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            registry.StartRegisteredSession(PlayerUserId, PlayerNickname, out SessionEnd firstReplacedSession);

            // Act
            registry.StartRegisteredSession(PlayerUserId, PlayerNickname, out SessionEnd replacedSession);

            // Assert
            Assert.Null(replacedSession.FriendsCallback);
        }

        /// <summary>Verifica TryEndSessionByFriendsChannel_WithReplacedChannel_KeepsCurrentSession.</summary>
        [Fact]
        public void TryEndSessionByFriendsChannel_WithReplacedChannel_KeepsCurrentSession()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            StartSubscribedSession(registry, new FriendsCallbackStub(), OldFriendsChannelId);
            string newToken = StartSubscribedSession(registry, new FriendsCallbackStub(), NewFriendsChannelId);

            // Act
            bool wasEnded = registry.TryEndSessionByFriendsChannel(OldFriendsChannelId, out SessionEnd sessionEnd);

            // Assert
            Assert.False(wasEnded);
            Assert.True(registry.TryFindSessionByToken(newToken, out PlayerSession currentSession));
        }

        /// <summary>Verifica TryEndSessionByFriendsChannel_WithCurrentChannel_EndsSession.</summary>
        [Fact]
        public void TryEndSessionByFriendsChannel_WithCurrentChannel_EndsSession()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = StartSubscribedSession(registry, new FriendsCallbackStub(), NewFriendsChannelId);

            // Act
            bool wasEnded = registry.TryEndSessionByFriendsChannel(NewFriendsChannelId, out SessionEnd sessionEnd);

            // Assert
            Assert.True(wasEnded);
            Assert.False(registry.TryFindSessionByToken(token, out PlayerSession endedSession));
        }

        /// <summary>Verifica TryEndSessionByFriendsChannel_WithUnknownChannel_ReturnsFalse.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(UnknownFriendsChannelId)]
        public void TryEndSessionByFriendsChannel_WithUnknownChannel_ReturnsFalse(string channelId)
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            StartSubscribedSession(registry, new FriendsCallbackStub(), NewFriendsChannelId);

            // Act
            bool wasEnded = registry.TryEndSessionByFriendsChannel(channelId, out SessionEnd sessionEnd);

            // Assert
            Assert.False(wasEnded);
        }

        private static string StartSubscribedSession(
            SessionRegistry registry,
            FriendsCallbackStub callback,
            string channelId)
        {
            string token = registry.StartRegisteredSession(PlayerUserId, PlayerNickname, out SessionEnd replacedSession);
            registry.SubscribeFriendsChannel(token, channelId, callback, new int[0]);

            return token;
        }
    }
}
