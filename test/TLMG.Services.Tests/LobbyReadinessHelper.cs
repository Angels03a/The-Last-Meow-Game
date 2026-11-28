using Xunit;
using TLMG.Contracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    internal static class LobbyReadinessHelper
    {
        internal const string CreatorChannelId = "creator";

        internal static string AddPlayer(
            LobbyScenario scenario,
            int userId,
            string channelId,
            ILobbyManagerCallback callback)
        {
            string token = scenario.Authorize(userId);
            SessionChannelCaller caller = new SessionChannelCaller(token, channelId);
            LobbyMembershipChange entry = scenario.Registry.EnterRoom(caller, scenario.RoomId, callback);
            Assert.Equal(LobbyChangeStatus.Applied, entry.Status);

            return token;
        }

        internal static LobbyMembershipChange MarkReady(LobbyScenario scenario, string token, string channelId)
        {
            SessionChannelCaller caller = new SessionChannelCaller(token, channelId);
            LobbyMembershipChange change = scenario.Registry.ChangePlayerReadiness(
                caller, scenario.RoomId, true);

            return change;
        }
    }
}