using System;

using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de SessionRegistry.</summary>
    public class SessionRegistryTests
    {
        private const string PlayerNickname = "Player";

        private const string LobbyChannelId = "lobby";

        private const string GuestChannelId = "guest";

        private const string SecondChannelId = "second";

        private const string SeventhChannelId = "seventh";

        private const string FriendsChannelId = "friends";

        /// <summary>Verifica TryFindSessionByToken_WithUnknownToken_RejectsRequest.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void TryFindSessionByToken_WithUnknownToken_RejectsRequest(string token)
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();

            // Act
            bool actual = registry.TryFindSessionByToken(token, out PlayerSession session);

            // Assert
            Assert.False(actual);
        }

        /// <summary>Verifica EndSession_WithUnknownToken_RejectsRequest.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void EndSession_WithUnknownToken_RejectsRequest(string token)
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();

            // Act
            bool actual = registry.EndSession(token, out SessionEnd sessionEnd);

            // Assert
            Assert.False(actual);
        }

        /// <summary>Verifica CreateRoom_WithUnknownToken_RejectsRequest.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void CreateRoom_WithUnknownToken_RejectsRequest(string token)
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();

            // Act
            LobbyMembershipChange actual = registry.CreateRoom(token, LobbyChannelId, null);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, actual.Status);
        }

        /// <summary>Verifica LeaveRoom_WithUnknownToken_RejectsRequest.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void LeaveRoom_WithUnknownToken_RejectsRequest(string token)
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();

            // Act
            LobbyMembershipChange actual = registry.LeaveRoom(token);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, actual.Status);
        }

        /// <summary>Verifica StartSession_WithNewAccount_CreatesSession.</summary>
        [Fact]
        public void StartSession_WithNewAccount_CreatesSession()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);

            // Act
            bool found = registry.TryFindSessionByToken(token, out PlayerSession session);

            // Assert
            Assert.True(found);
        }

        /// <summary>Verifica StartSession_WithRepeatedAccount_InvalidatesPreviousToken.</summary>
        [Fact]
        public void StartSession_WithRepeatedAccount_InvalidatesPreviousToken()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);
            registry.StartSession(1, PlayerNickname, out replacedSession);

            // Act
            bool found = registry.TryFindSessionByToken(token, out PlayerSession session);

            // Assert
            Assert.False(found);
        }

        /// <summary>Verifica EndSession_WithCurrentToken_RemovesSession.</summary>
        [Fact]
        public void EndSession_WithCurrentToken_RemovesSession()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);
            registry.EndSession(token, out SessionEnd endedSession);

            // Act
            bool found = registry.TryFindSessionByToken(token, out PlayerSession session);

            // Assert
            Assert.False(found);
        }

        /// <summary>Verifica CreateRoom_WithSession_AddsCreator.</summary>
        [Fact]
        public void CreateRoom_WithSession_AddsCreator()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);

            // Act
            LobbyMembershipChange change = registry.CreateRoom(token, LobbyChannelId, null);

            // Assert
            Assert.Single(change.Room.Players);
        }

        /// <summary>Verifica CreateRoom_WithExistingMembership_RejectsSecondRoom.</summary>
        [Fact]
        public void CreateRoom_WithExistingMembership_RejectsSecondRoom()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);
            registry.CreateRoom(token, LobbyChannelId, null);

            // Act
            LobbyMembershipChange change = registry.CreateRoom(token, SecondChannelId, null);

            // Assert
            Assert.Equal(LobbyChangeStatus.AlreadyInRoom, change.Status);
        }

        /// <summary>Verifica LeaveRoom_WithLastPlayer_DeletesRoom.</summary>
        [Fact]
        public void LeaveRoom_WithLastPlayer_DeletesRoom()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);
            registry.CreateRoom(token, LobbyChannelId, null);

            // Act
            LobbyMembershipChange change = registry.LeaveRoom(token);

            // Assert
            Assert.Null(change.Room);
        }

        /// <summary>Verifica LeaveRoom_WithNoMembership_ReturnsNotInRoom.</summary>
        [Fact]
        public void LeaveRoom_WithNoMembership_ReturnsNotInRoom()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);

            // Act
            LobbyMembershipChange change = registry.LeaveRoom(token);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
        }

        /// <summary>Verifica EnterRoom_WithoutAuthorization_RejectsEntry.</summary>
        [Fact]
        public void EnterRoom_WithoutAuthorization_RejectsEntry()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);

            // Act
            LobbyMembershipChange change = registry.EnterRoom(token, 1, LobbyChannelId, null);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotAuthorized, change.Status);
        }

        /// <summary>Verifica SubscribeFriendsChannel_WithNullFriendList_ThrowsNullReference.</summary>
        [Fact]
        public void SubscribeFriendsChannel_WithNullFriendList_ThrowsNullReference()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartSession(1, PlayerNickname, out SessionEnd replacedSession);

            // Act
            Action subscribe = () => registry.SubscribeFriendsChannel(token, FriendsChannelId, null, null);

            // Assert
            Assert.Throws<NullReferenceException>(subscribe);
        }

        /// <summary>Verifica EnterRoom_WithAuthorization_AddsPlayer.</summary>
        [Fact]
        public void EnterRoom_WithAuthorization_AddsPlayer()
        {
            // Arrange
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(2);

            // Act
            LobbyMembershipChange change = lobby.Registry.EnterRoom(token, lobby.RoomId, GuestChannelId, null);

            // Assert
            Assert.Equal(2, change.Room.Players.Count);
        }

        /// <summary>Verifica EnterRoom_AfterAuthorizationConsumed_RejectsReentry.</summary>
        [Fact]
        public void EnterRoom_AfterAuthorizationConsumed_RejectsReentry()
        {
            // Arrange
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(2);
            lobby.Registry.EnterRoom(token, lobby.RoomId, GuestChannelId, null);
            lobby.Registry.LeaveRoom(token);

            // Act
            LobbyMembershipChange change = lobby.Registry.EnterRoom(token, lobby.RoomId, GuestChannelId, null);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotAuthorized, change.Status);
        }

        /// <summary>Verifica EnterRoom_WithSixPlayers_RejectsSeventh.</summary>
        [Fact]
        public void EnterRoom_WithSixPlayers_RejectsSeventh()
        {
            // Arrange
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(7);
            lobby.FillRoom();

            // Act
            LobbyMembershipChange change = lobby.Registry.EnterRoom(token, lobby.RoomId, SeventhChannelId, null);

            // Assert
            Assert.Equal(LobbyChangeStatus.RoomFull, change.Status);
        }

        /// <summary>Verifica LeaveRoom_WhenCreatorLeaves_KeepsOtherPlayer.</summary>
        [Fact]
        public void LeaveRoom_WhenCreatorLeaves_KeepsOtherPlayer()
        {
            // Arrange
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(2);
            lobby.Registry.EnterRoom(token, lobby.RoomId, GuestChannelId, null);

            // Act
            LobbyMembershipChange change = lobby.Registry.LeaveRoom(lobby.CreatorToken);

            // Assert
            Assert.Single(change.Room.Players);
        }

    }
}
