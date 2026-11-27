using TLMG.DataAccess.Repositories;
using TLMG.Domain.Repositories;
using TLMG.Services;
using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Leaderboard;
using TLMG.Services.Lobby;
using TLMG.Services.Match;
using TLMG.Services.Moderation;
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
            IGameHistoryRepository gameHistoryRepository = new GameHistoryRepository();
            IBlockRepository blockRepository = new BlockRepository();
            BlockPolicy blockPolicy = new BlockPolicy(blockRepository);

            IPasswordHasher passwordHasher = new PasswordHasher();
            SessionRegistry sessionRegistry = new SessionRegistry();
            sessionRegistry.ConfigureMatchHistory(gameHistoryRepository);
            sessionRegistry.ConfigureBlockPolicy(blockPolicy);
            sessionRegistry.ConfigureKickHistory(new KickVotingRepository());

            VerificationCodeStore verificationCodeStore = new VerificationCodeStore(verificationCodeRepository);
            EmailUtility emailUtility = new EmailUtility();

            AccountVerificationDispatcher verificationDispatcher =
                new AccountVerificationDispatcher(verificationCodeStore, emailUtility);

            PlayerAccountRegistrationService registrationService =
                new PlayerAccountRegistrationService(playerRepository, passwordHasher, verificationDispatcher);

            GuestSessionService guestSessionService = new GuestSessionService(sessionRegistry);

            PlayerProfileService profileService =
                new PlayerProfileService(playerProfileRepository, sessionRegistry, sessionRegistry);

            ChatService chatService =
                new ChatService(sessionRegistry, new ChatMessageRepository(), sessionRegistry);

            FriendsService friendsService =
                new FriendsService(friendshipRepository, sessionRegistry, sessionRegistry, blockPolicy);

            LeaderboardService leaderboardService = new LeaderboardService(playerRepository, sessionRegistry);

            MatchStartCoordinator matchStartCoordinator =
                new MatchStartCoordinator(sessionRegistry, gameHistoryRepository);
            matchStartCoordinator.RecoverInterruptedMatches();

            IReportRepository reportRepository = new ReportRepository();
            ReportService reportService = new ReportService(reportRepository, sessionRegistry, matchStartCoordinator);
            PlayerLogInService logInService =
                new PlayerLogInService(playerRepository, passwordHasher, sessionRegistry, reportService);

            LobbyService lobbyService = new LobbyService(sessionRegistry, sessionRegistry, matchStartCoordinator);

            MatchService matchService = new MatchService(sessionRegistry);
            IPasswordRepository passwordRepository = new PasswordRepository();

            PasswordResetDispatcher resetDispatcher =
                new PasswordResetDispatcher(verificationCodeStore, emailUtility, playerRepository);

            PasswordChangeService passwordChangeService =
                new PasswordChangeService(passwordRepository, passwordHasher, sessionRegistry);

            PasswordRecoveryService passwordRecoveryService =
                new PasswordRecoveryService(passwordRepository, passwordHasher, resetDispatcher);

            ModerationService moderationService = new ModerationService(blockRepository, sessionRegistry, blockPolicy);

            KickVotingService kickVotingService = new KickVotingService(sessionRegistry, matchStartCoordinator);

            ServiceDependencies dependencies = new ServiceDependencies
            {
                SessionRegistry = sessionRegistry,
                RegistrationService = registrationService,
                LogInService = logInService,
                GuestSessionService = guestSessionService,
                ProfileService = profileService,
                ChatService = chatService,
                FriendsService = friendsService,
                LeaderboardService = leaderboardService,
                LobbyService = lobbyService,
                MatchService = matchService,
                PasswordChangeService = passwordChangeService,
                PasswordRecoveryService = passwordRecoveryService,
                ModerationService = moderationService,
                ReportService = reportService,
                KickVotingService = kickVotingService
            };

            ServiceImplementation.ConfigureDependencies(dependencies);
        }
    }
}