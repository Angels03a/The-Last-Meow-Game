using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Leaderboard;
using TLMG.Services.Lobby;
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
        public static void ConfigureDependencies(ServiceDependencies serviceDependencies)
        {
            _sessionRegistry = serviceDependencies.SessionRegistry;
            _playerAccountRegistrationService = serviceDependencies.RegistrationService;
            _playerLogInService = serviceDependencies.LogInService;
            _chatService = serviceDependencies.ChatService;
            _friendsService = serviceDependencies.FriendsService;
            _leaderboardService = serviceDependencies.LeaderboardService;
            _lobbyService = serviceDependencies.LobbyService;
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