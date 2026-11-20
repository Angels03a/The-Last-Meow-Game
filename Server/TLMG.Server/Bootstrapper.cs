using TLMG.DataAccess.Repositories;
using TLMG.Domain.Repositories;
using TLMG.Services;
using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Leaderboard;
using TLMG.Services.Lobby;
using TLMG.Services.Match;
using TLMG.Services.Sessions;
using TLMG.Services.Utilities;

namespace TLMG.Server
{
    public static class Bootstrapper
    {
        public static void Initialize()
        {
            IPlayerRepository playerRepository = new PlayerRepository();
            IPlayerProfileRepository playerProfileRepository = new PlayerProfileRepository();
            IFriendshipRepository friendshipRepository = new FriendshipRepository();
            IVerificationCodeRepository verificationCodeRepository = new VerificationCodeRepository();

            IPasswordHasher passwordHasher = new PasswordHasher();
            SessionRegistry sessionRegistry = new SessionRegistry();

            VerificationCodeStore verificationCodeStore = new VerificationCodeStore(verificationCodeRepository);

            EmailUtility emailUtility = new EmailUtility();

            AccountVerificationDispatcher verificationDispatcher =
                new AccountVerificationDispatcher(verificationCodeStore, emailUtility);

            PlayerAccountRegistrationService registrationService =
                new PlayerAccountRegistrationService(playerRepository, passwordHasher, verificationDispatcher);

            PlayerLogInService logInService =
                new PlayerLogInService(playerRepository, passwordHasher, sessionRegistry);

            PlayerProfileService profileService =
                new PlayerProfileService(playerProfileRepository, sessionRegistry);

            ChatService chatService = new ChatService(sessionRegistry, new ChatMessageRepository());

            FriendsService friendsService = new FriendsService(friendshipRepository, sessionRegistry);

            LeaderboardService leaderboardService = new LeaderboardService(playerRepository);

            LobbyService lobbyService = new LobbyService(sessionRegistry);

            MatchService matchService = new MatchService(sessionRegistry);
            IPasswordRepository passwordRepository = new PasswordRepository();

            PasswordResetDispatcher resetDispatcher =
                new PasswordResetDispatcher(verificationCodeStore, emailUtility, playerRepository);

            PasswordChangeService passwordChangeService =
                new PasswordChangeService(passwordRepository, passwordHasher, sessionRegistry);

            PasswordRecoveryService passwordRecoveryService =
                new PasswordRecoveryService(passwordRepository, passwordHasher, resetDispatcher);

            ServiceDependencies dependencies = new ServiceDependencies
            {
                SessionRegistry = sessionRegistry,
                RegistrationService = registrationService,
                LogInService = logInService,
                ProfileService = profileService,
                ChatService = chatService,
                FriendsService = friendsService,
                LeaderboardService = leaderboardService,
                LobbyService = lobbyService,
                MatchService = matchService,
                PasswordChangeService = passwordChangeService,
                PasswordRecoveryService = passwordRecoveryService
            };

            ServiceImplementation.ConfigureDependencies(dependencies);
        }
    }
}