using System;
using Xunit;
using TLMG.Services.Sessions;
using TLMG.Services.Moderation;

namespace TLMG.Services.Tests
{
    public class SessionRegistryTests
    {
        private const string PlayerNickname = "Player";
        private const string LobbyChannelId = "lobby";
        private const string GuestChannelId = "guest";
        private const string SecondChannelId = "second";
        private const string SeventhChannelId = "seventh";
        private const string FriendsChannelId = "friends";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void TryFindSessionByToken_WithUnknownToken_RejectsRequest(string token)
        {
            SessionRegistry registry = new SessionRegistry();

            bool actual = registry.TryFindSessionByToken(token, out PlayerSession session);

            Assert.False(actual);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void EndSession_WithUnknownToken_RejectsRequest(string token)
        {
            SessionRegistry registry = new SessionRegistry();

            bool actual = registry.EndSession(token, out SessionEnd sessionEnd);

            Assert.False(actual);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void CreateRoom_WithUnknownToken_RejectsRequest(string token)
        {
            SessionRegistry registry = new SessionRegistry();
            SessionChannelCaller caller = new SessionChannelCaller(token, LobbyChannelId);

            LobbyMembershipChange actual = registry.CreateRoom(caller, null);

            Assert.Equal(LobbyChangeStatus.InvalidSession, actual.Status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void LeaveRoom_WithUnknownToken_RejectsRequest(string token)
        {
            SessionRegistry registry = new SessionRegistry();

            LobbyMembershipChange actual = registry.LeaveRoom(token, out _);

            Assert.Equal(LobbyChangeStatus.InvalidSession, actual.Status);
        }

        [Fact]
        public void StartSession_WithNewAccount_CreatesSession()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);

            bool found = registry.TryFindSessionByToken(token, out PlayerSession session);

            Assert.True(found);
        }

        [Fact]
        public void StartSession_WithRepeatedAccount_InvalidatesPreviousToken()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);
            registry.StartRegisteredSession(1, PlayerNickname, out replacedSession);

            bool found = registry.TryFindSessionByToken(token, out PlayerSession session);

            Assert.False(found);
        }

        [Fact]
        public void EndSession_WithCurrentToken_RemovesSession()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);
            registry.EndSession(token, out SessionEnd endedSession);

            bool found = registry.TryFindSessionByToken(token, out PlayerSession session);

            Assert.False(found);
        }

        [Fact]
        public void CreateRoom_WithSession_AddsCreator()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);
            SessionChannelCaller caller = new SessionChannelCaller(token, LobbyChannelId);

            LobbyMembershipChange change = registry.CreateRoom(caller, null);

            Assert.Single(change.Room.Players);
        }

        [Fact]
        public void CreateRoom_WithExistingMembership_RejectsSecondRoom()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);
            SessionChannelCaller firstCaller = new SessionChannelCaller(token, LobbyChannelId);
            SessionChannelCaller secondCaller = new SessionChannelCaller(token, SecondChannelId);
            registry.CreateRoom(firstCaller, null);

            LobbyMembershipChange change = registry.CreateRoom(secondCaller, null);

            Assert.Equal(LobbyChangeStatus.AlreadyInRoom, change.Status);
        }

        [Fact]
        public void LeaveRoom_WithLastPlayer_DeletesRoom()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);
            SessionChannelCaller caller = new SessionChannelCaller(token, LobbyChannelId);
            registry.CreateRoom(caller, null);

            LobbyMembershipChange change = registry.LeaveRoom(token, out _);

            Assert.Null(change.Room);
        }

        [Fact]
        public void LeaveRoom_WithNoMembership_ReturnsNotInRoom()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);

            LobbyMembershipChange change = registry.LeaveRoom(token, out _);

            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
        }

        [Fact]
        public void EnterRoom_WithoutAuthorization_RejectsEntry()
        {
            SessionRegistry registry = new SessionRegistry();
            registry.ConfigureBlockPolicy(new BlockPolicy(new NoBlocksRepository()));
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);
            SessionChannelCaller caller = new SessionChannelCaller(token, LobbyChannelId);

            LobbyMembershipChange change = registry.EnterRoom(caller, 1, null);

            Assert.Equal(LobbyChangeStatus.NotAuthorized, change.Status);
        }

        [Fact]
        public void SubscribeFriendsChannel_WithNullFriendList_ThrowsNullReference()
        {
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(1, PlayerNickname, out SessionEnd replacedSession);

            Action subscribe = () => registry.SubscribeFriendsChannel(token, FriendsChannelId, null, null);

            Assert.Throws<NullReferenceException>(subscribe);
        }

        [Fact]
        public void EnterRoom_WithAuthorization_AddsPlayer()
        {
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(2);
            SessionChannelCaller caller = new SessionChannelCaller(token, GuestChannelId);

            LobbyMembershipChange change = lobby.Registry.EnterRoom(caller, lobby.RoomId, null);

            Assert.Equal(2, change.Room.Players.Count);
        }

        [Fact]
        public void EnterRoom_AfterAuthorizationConsumed_RejectsReentry()
        {
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(2);
            SessionChannelCaller caller = new SessionChannelCaller(token, GuestChannelId);
            lobby.Registry.EnterRoom(caller, lobby.RoomId, null);
            lobby.Registry.LeaveRoom(token, out _);

            LobbyMembershipChange change = lobby.Registry.EnterRoom(caller, lobby.RoomId, null);

            Assert.Equal(LobbyChangeStatus.NotAuthorized, change.Status);
        }

        [Fact]
        public void EnterRoom_WithSixPlayers_RejectsSeventh()
        {
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(7);
            SessionChannelCaller caller = new SessionChannelCaller(token, SeventhChannelId);
            lobby.FillRoom();

            LobbyMembershipChange change = lobby.Registry.EnterRoom(caller, lobby.RoomId, null);

            Assert.Equal(LobbyChangeStatus.RoomFull, change.Status);
        }

        [Fact]
        public void LeaveRoom_WhenCreatorLeaves_KeepsOtherPlayer()
        {
            LobbyScenario lobby = new LobbyScenario();
            string token = lobby.Authorize(2);
            SessionChannelCaller caller = new SessionChannelCaller(token, GuestChannelId);
            lobby.Registry.EnterRoom(caller, lobby.RoomId, null);

            LobbyMembershipChange change = lobby.Registry.LeaveRoom(lobby.CreatorToken, out _);

            Assert.Single(change.Room.Players);
        }
    }
}