using System;
using System.Collections.Generic;
using System.ServiceModel;

using log4net;

using TLMG.DataAccess.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Chat;
using TLMG.Services.Friends;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    /// <summary>
    /// Implementación de todos los contratos del servidor; cada contrato vive en su propio archivo partial.
    /// </summary>
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        ConcurrencyMode = ConcurrencyMode.Single)]
    public partial class ServiceImplementation
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ServiceImplementation));

        private static readonly ISessionRegistry _sessionRegistry;

        /// <summary>
        /// Construye en un solo lugar y en orden explícito los objetos que dependen del registro de sesiones.
        /// C# no define el orden de inicialización de los campos estáticos entre archivos parciales, pero todos
        /// sus inicializadores se ejecutan antes que este cuerpo; por eso aquí se pueden usar los campos con
        /// inicializador de los demás archivos. Cada contrato nuevo que dependa del registro se asigna aquí.
        /// </summary>
        static ServiceImplementation()
        {
            _sessionRegistry = new SessionRegistry();
            _playerLogInService = new PlayerLogInService(_playerRepository, _passwordHasher, _sessionRegistry);
            _lobbyService = new LobbyService(_sessionRegistry);
            _chatService = new ChatService(_sessionRegistry);
            _friendshipRepository = new FriendshipRepository();
            _friendsService = new FriendsService(_friendshipRepository, _sessionRegistry);
        }

        /// <summary>
        /// Entrega varias notificaciones, una por una, sin que un cliente caído impida avisar a los demás.
        /// </summary>
        /// <param name="notifications">Invocaciones de callbacks que ya no dependen de ningún candado.</param>
        private void SafeNotifyAll(IReadOnlyList<Action> notifications)
        {
            foreach (Action notification in notifications)
            {
                SafeNotify(notification);
            }
        }

        /// <summary>
        /// Envía una notificación al cliente sin que un cliente caído afecte al servidor.
        /// </summary>
        /// <param name="notificationOfResult">Invocación del callback.</param>
        private void SafeNotify(Action notificationOfResult)
        {
            try
            {
                notificationOfResult();
            }
            catch (CommunicationException communicationException)
            {
                _log.Warn("Error de comunicación al notificar al cliente; verifique la conexión por parte del cliente.",
                    communicationException);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Warn("El cliente tardó demasiado en recibir la notificación.", timeoutException);
            }
            catch (ObjectDisposedException disposedException)
            {
                _log.Warn("El canal del cliente fue liberado antes de llegar la respuesta del servidor. " +
                    "Se destruye la respuesta que iba a enviarse.", disposedException);
            }
        }
    }
}