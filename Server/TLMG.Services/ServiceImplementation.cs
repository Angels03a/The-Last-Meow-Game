using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Leaderboard;
using TLMG.Services.Lobby;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        ConcurrencyMode = ConcurrencyMode.Single)]
    public partial class ServiceImplementation
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ServiceImplementation));

        private static ISessionRegistry _sessionRegistry;
        private static PlayerAccountRegistrationService _playerAccountRegistrationService;
        private static PlayerLogInService _playerLogInService;
        private static ChatService _chatService;
        private static FriendsService _friendsService;
        private static LeaderboardService _leaderboardService;
        private static LobbyService _lobbyService;
        private static MatchService _matchService;

        /// <summary>
        /// Recibe el grafo de objetos ya construido desde el Composition Root.
        /// WCF se vuelve ciego respecto a la base de datos y la encriptación.
        /// </summary>
        public static void ConfigureDependencies(
            ISessionRegistry sessionRegistry,
            PlayerAccountRegistrationService registrationService,
            PlayerLogInService logInService,
            ChatService chatService,
            FriendsService friendsService,
            LeaderboardService leaderboardService,
            LobbyService lobbyService,
            MatchService matchService)
        {
            _sessionRegistry = sessionRegistry;
            _playerAccountRegistrationService = registrationService;
            _playerLogInService = logInService;
            _chatService = chatService;
            _friendsService = friendsService;
            _leaderboardService = leaderboardService;
            _lobbyService = lobbyService;
            _matchService = matchService;
        }

        private void SafeNotifyAll(IReadOnlyList<Action> notifications)
        {
            foreach (Action notification in notifications)
            {
                SafeNotify(notification);
            }
        }

        private void SafeNotify(Action notificationOfResult)
        {
            try
            {
                notificationOfResult();
            }
            catch (CommunicationException communicationException)
            {
                _log.Warn("Error de comunicación al notificar al cliente.", communicationException);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Warn("El cliente tardó demasiado en recibir la notificación.", timeoutException);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente en el servidor al enviar respuesta.", outOfMemoryException);
            }
            catch (ObjectDisposedException disposedException)
            {
                _log.Warn("El canal del cliente fue liberado antes de llegar la respuesta.", disposedException);
            }
        }
    }
}