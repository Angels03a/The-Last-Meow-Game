using TLMG.Domain.Repositories;

namespace TLMG.Services.Sessions
{
    public sealed class MatchStartPreparation
    {
        internal MatchStartPreparation(LobbyMembershipChange lobbyMembershipChange, GameRegistration gameRegistration)
        {
            LobbyMembershipChange = lobbyMembershipChange;
            GameRegistration = gameRegistration;
        }

        public LobbyMembershipChange LobbyMembershipChange { get; }
        public GameRegistration GameRegistration { get; }
    }
}
