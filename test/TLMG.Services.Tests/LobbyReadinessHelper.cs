using TLMG.Contracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Ayudantes de prueba para sumar jugadores a la sala de un LobbyScenario y marcarlos Listo.</summary>
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
            scenario.Registry.EnterRoom(token, scenario.RoomId, channelId, callback);

            return token;
        }

        internal static LobbyMembershipChange MarkReady(LobbyScenario scenario, string token, string channelId)
        {
            LobbyMembershipChange change = scenario.Registry.ChangePlayerReadiness(
                token, channelId, scenario.RoomId, true);

            return change;
        }
    }
}
