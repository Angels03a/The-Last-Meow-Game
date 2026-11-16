using System;
using System.Collections.Generic;

using Xunit;

using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de SessionNotificationHelper para las sesiones reemplazadas por un nuevo inicio.</summary>
    public class SessionNotificationHelperTests
    {
        private const int PlayerUserId = 1;
        private const string PlayerNickname = "Player";
        private const string FriendsChannelId = "friends";

        /// <summary>
        /// Verifica CreateReplacedSessionNotifications_WithNonChannelCallback_AddsNothingAndDoesNotFail.
        /// </summary>
        [Fact]
        public void CreateReplacedSessionNotifications_WithNonChannelCallback_AddsNothingAndDoesNotFail()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(PlayerUserId, PlayerNickname, out SessionEnd firstReplacedSession);
            registry.SubscribeFriendsChannel(token, FriendsChannelId, new FriendsCallbackStub(), new int[0]);
            registry.StartSession(PlayerUserId, PlayerNickname, out SessionEnd replacedSession);
            IReadOnlyList<Action> notifications = null;

            // Act
            Exception thrown = Record.Exception(
                () => notifications = SessionNotificationHelper.CreateReplacedSessionNotifications(replacedSession));

            // Assert
            Assert.Null(thrown);
            Assert.Empty(notifications);
        }

        /// <summary>Verifica CreateReplacedSessionNotifications_WithNullSession_ReturnsNoNotifications.</summary>
        [Fact]
        public void CreateReplacedSessionNotifications_WithNullSession_ReturnsNoNotifications()
        {
            // Act
            IReadOnlyList<Action> notifications = SessionNotificationHelper.CreateReplacedSessionNotifications(null);

            // Assert
            Assert.Empty(notifications);
        }
    }
}
