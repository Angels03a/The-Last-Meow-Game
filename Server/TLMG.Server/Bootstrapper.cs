using TLMG.DataAccess.Repositories;
using TLMG.Domain.Repositories;
using TLMG.Services;
using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Leaderboard;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;
using TLMG.Services.Utilities;

namespace TLMG.Server
{
    public static class Bootstrapper
    {
        public static void Initialize()
        {
            IPlayerRepository playerRepository = new PlayerRepository();
            IFriendshipRepository friendshipRepository = new FriendshipRepository();
            IVerificationCodeRepository verificationCodeRepository = new VerificationCodeRepository();

            IPasswordHasher passwordHasher = new PasswordHasher();
            ISessionRegistry sessionRegistry = new SessionRegistry();
            
            VerificationCodeStore verificationCodeStore = new VerificationCodeStore(verificationCodeRepository);
            
            EmailUtility emailUtility = new EmailUtility();
            
            AccountVerificationDispatcher verificationDispatcher =
                new AccountVerificationDispatcher(verificationCodeStore, emailUtility);

            PlayerAccountRegistrationService registrationService =
                new PlayerAccountRegistrationService(playerRepository, passwordHasher, verificationDispatcher);

            PlayerLogInService logInService =
                new PlayerLogInService(playerRepository, passwordHasher, sessionRegistry);

            ChatService chatService = new ChatService(sessionRegistry, new ChatMessageRepository());

            FriendsService friendsService = new FriendsService(friendshipRepository, sessionRegistry);

            LeaderboardService leaderboardService = new LeaderboardService(playerRepository);

            LobbyService lobbyService = new LobbyService(sessionRegistry);

            ServiceDependencies dependencies = new ServiceDependencies
            {
                SessionRegistry = sessionRegistry,
                RegistrationService = registrationService,
                LogInService = logInService,
                ChatService = chatService,
                FriendsService = friendsService,
                LeaderboardService = leaderboardService,
                LobbyService = lobbyService
            };

            ServiceImplementation.ConfigureDependencies(dependencies);
        }
    }
}