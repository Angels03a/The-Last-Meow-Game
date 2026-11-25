using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Leaderboard;
using TLMG.Services.Lobby;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    /// <summary>
    /// Groups the object graph built by the composition root so WCF can receive it in one argument.
    /// </summary>
    public class ServiceDependencies
    {
        public ISessionRegistry SessionRegistry { get; set; }
        public PlayerAccountRegistrationService RegistrationService { get; set; }
        public PlayerLogInService LogInService { get; set; }
        public PlayerProfileService ProfileService { get; set; }
        public ChatService ChatService { get; set; }
        public FriendsService FriendsService { get; set; }
        public LeaderboardService LeaderboardService { get; set; }
        public LobbyService LobbyService { get; set; }
        public MatchService MatchService { get; set; }
        public PasswordChangeService PasswordChangeService { get; set; }
        public PasswordRecoveryService PasswordRecoveryService { get; set; }
        public GuestSessionService GuestSessionService { get; set; }
    }
}