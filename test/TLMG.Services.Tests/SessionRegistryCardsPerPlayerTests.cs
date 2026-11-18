using Xunit;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    public class SessionRegistryCardsPerPlayerTests
    {
        private const string SecondChannelId = "lobby2";
        private const string ThirdChannelId = "lobby3";
        private const int SecondUserId = 2;
        private const int ThirdUserId = 3;
        private const int CreatorUserId = 1;
        private const int DefaultCardsPerPlayer = 4;
        private const int EightCardsPerPlayer = 8;

        [Fact]
        public void ChangeCardsPerPlayer_WithDifferentValue_SavesValueAndResetsEveryoneToNotReady()
        {
            LobbyScenario scenario = new LobbyScenario();
            string secondToken = LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, null);
            string thirdToken = LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, null);
            LobbyReadinessHelper.MarkReady(scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyReadinessHelper.MarkReady(scenario, secondToken, SecondChannelId);
            LobbyMembershipChange allReady = LobbyReadinessHelper.MarkReady(scenario, thirdToken, ThirdChannelId);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller,
                scenario.RoomId,
                EightCardsPerPlayer);

            Assert.Equal(3, allReady.Room.Start.ReadyUserIds.Count);
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
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, ThirdUserId, ThirdChannelId, thirdCallback);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller,
                scenario.RoomId,
                EightCardsPerPlayer);

            Assert.Equal(2, change.RecipientCallbacks.Count);
            Assert.Contains(secondCallback, change.RecipientCallbacks);
            Assert.Contains(thirdCallback, change.RecipientCallbacks);
            Assert.Equal(0, secondCallback.CallCount);
            Assert.Equal(0, thirdCallback.CallCount);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithSameValue_KeepsReadyAndSendsNoNotifications()
        {
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, SecondUserId, SecondChannelId, secondCallback);
            LobbyMembershipChange ready = LobbyReadinessHelper.MarkReady(
                scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);
            LobbyCaller creatorCaller = new LobbyCaller(
                scenario.CreatorToken,
                LobbyReadinessHelper.CreatorChannelId);

            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                creatorCaller,
                scenario.RoomId,
                DefaultCardsPerPlayer);

            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(DefaultCardsPerPlayer, change.Room.Start.CardsPerPlayerCount);
            Assert.Contains(CreatorUserId, change.Room.Start.ReadyUserIds);
            Assert.Equal(ready.Room.Start.Version, change.Room.Start.Version);
            Assert.Empty(change.RecipientCallbacks);
            Assert.Equal(0, secondCallback.CallCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangeCardsPerPlayer_WithInvalidSession_RejectsRequest(string token)
        {
            LobbyScenario scenario = new LobbyScenario();
            LobbyCaller invalidCaller = new LobbyCaller(token, LobbyReadinessHelper.CreatorChannelId);

            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                invalidCaller,
                scenario.RoomId,
                EightCardsPerPlayer);

            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Null(change.Room);
        }

        [Fact]
        public void ChangeCardsPerPlayer_WithPlayerOutsideLobby_RejectsRequest()
        {
            LobbyScenario scenario = new LobbyScenario();
            string outsiderToken = scenario.Registry.StartRegisteredSession(9, "Outsider", out SessionEnd replaced);
            LobbyCaller outsiderCaller = new LobbyCaller(outsiderToken, "outsider");

            LobbyMembershipChange change = scenario.Registry.ChangeCardsPerPlayer(
                outsiderCaller,
                scenario.RoomId,
                EightCardsPerPlayer);

            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
            Assert.Null(change.Room);
        }
    }
}