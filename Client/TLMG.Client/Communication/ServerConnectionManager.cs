using System;
using System.ServiceModel;
using System.Windows.Threading;

using log4net;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Única pieza del cliente que conoce los canales de amigos y de lobby: guarda el sessionToken del jugador
    /// registrado, mantiene vivo su canal de amigos (estar en línea) y convierte los callbacks del servidor en
    /// eventos .NET que se disparan en el hilo de la interfaz. Los eventos llevan los DTO tal como llegan, con su
    /// ErrorKey y sin textos: la traducción ocurre en pantalla. Es una clase de instancia: App crea una sola y las
    /// pantallas la alcanzan con ((App)Application.Current).ServerConnection; como vive más que ellas, deben
    /// desuscribirse en OnScreenUnloaded (§14.2). Se usa solo desde el hilo de la interfaz y no crea hilos, tareas ni
    /// temporizadores (D-60, ADR-08).
    /// </summary>
    internal sealed class ServerConnectionManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ServerConnectionManager));

        private readonly Dispatcher _interfaceDispatcher;
        private FriendsManagerClient _friendsClient;
        private IClientChannel _friendsChannel;
        private LobbyManagerClient _lobbyClient;
        private IClientChannel _lobbyChannel;
        private string _sessionToken;

        /// <summary>Crea el manager sin conexión alguna.</summary>
        /// <param name="interfaceDispatcher">Dispatcher del hilo de la interfaz, donde se disparan los eventos.</param>
        public ServerConnectionManager(Dispatcher interfaceDispatcher)
        {
            _interfaceDispatcher = interfaceDispatcher;
        }

        /// <summary>Se dispara con la lista de amigos que responde el servidor a la suscripción.</summary>
        public event EventHandler<ServerEventArgs<OperationSubscribeToFriendsResult>> FriendListReceived;

        /// <summary>Se dispara cuando un amigo pasa a estar en línea o deja de estarlo.</summary>
        public event EventHandler<ServerEventArgs<Friend>> FriendPresenceChanged;

        /// <summary>Se dispara cuando un amigo invita al jugador a su sala.</summary>
        public event EventHandler<ServerEventArgs<RoomInvitation>> RoomInvitationReceived;

        /// <summary>Se dispara con el resultado de invitar a un amigo a la sala.</summary>
        public event EventHandler<ServerEventArgs<OperationInviteFriendToRoomResult>>
            InviteFriendResultReceived;

        /// <summary>Se dispara con el resultado de aceptar una invitación; trae el id de la sala.</summary>
        public event EventHandler<ServerEventArgs<OperationAcceptRoomInvitationResult>>
            AcceptInvitationResultReceived;

        /// <summary>Se dispara con el resultado de crear una sala.</summary>
        public event EventHandler<ServerEventArgs<OperationCreateRoomResult>> CreateRoomResultReceived;

        /// <summary>Se dispara con el resultado de entrar a una sala.</summary>
        public event EventHandler<ServerEventArgs<OperationEnterLobbyResult>> EnterLobbyResultReceived;

        /// <summary>Se dispara con el resultado de salir de una sala.</summary>
        public event EventHandler<ServerEventArgs<OperationLeaveLobbyResult>> LeaveLobbyResultReceived;

        /// <summary>Se dispara cuando cambian los jugadores de la sala del jugador.</summary>
        public event EventHandler<ServerEventArgs<LobbyRoom>> LobbyRoomChanged;

        /// <summary>
        /// Se dispara cuando un canal con el servidor termina sin que la aplicación lo haya pedido, o cuando falla un
        /// envío. El manager ya liberó todo; la aplicación debe regresar al inicio de sesión.
        /// </summary>
        public event EventHandler ConnectionLost;

        /// <summary>Obtiene si hay una sesión de jugador registrado con su canal de amigos abierto.</summary>
        public bool HasFriendsChannel => _friendsClient != null && !string.IsNullOrEmpty(_sessionToken);

        /// <summary>
        /// Obtiene la última foto de la sala del jugador que entregó el servidor, o null si no está en una sala. El
        /// manager la actualiza al recibir los callbacks de lobby aunque ninguna pantalla esté suscrita, para que la
        /// sala sobreviva a la pantalla que la muestra; se limpia al salir de la sala o al terminar la sesión.
        /// </summary>
        public LobbyRoom CurrentRoom { get; private set; }

        /// <summary>Obtiene si se pidió crear una sala y su resultado todavía no llega.</summary>
        public bool IsRoomCreationPending { get; private set; }

        /// <summary>
        /// Guarda el sessionToken del jugador registrado que entró, abre su canal de amigos y se suscribe a ellos. Un
        /// invitado no tiene token y no pasa por aquí.
        /// </summary>
        /// <param name="operationLogInResult">Resultado exitoso del inicio de sesión, con su sessionToken.</param>
        /// <returns>
        /// true si la sesión quedó guardada y el canal de amigos abierto; false si el resultado no trae sesión o el
        /// servidor no respondió, caso en el que no queda nada abierto y el detalle está en el log.
        /// </returns>
        public bool StartRegisteredSession(OperationLogInResult operationLogInResult)
        {
            bool wasStarted = false;
            Disconnect();

            if (IsRegisteredSessionResult(operationLogInResult))
            {
                _sessionToken = operationLogInResult.SessionToken;
                wasStarted = TryOpenFriendsChannel();

                if (!wasStarted)
                {
                    Disconnect();
                }
            }
            else
            {
                _log.Warn("Se ignoró un resultado de inicio de sesión que no trae una sesión registrada.");
            }

            return wasStarted;
        }

        /// <summary>
        /// Cierra los canales y limpia el estado; al cerrarse el canal de amigos el servidor termina la sesión del
        /// jugador. Es seguro llamarlo sin sesión, por ejemplo con un invitado.
        /// </summary>
        public void Disconnect()
        {
            UnwatchChannel(_friendsChannel);
            UnwatchChannel(_lobbyChannel);
            CloseClientSafely(_friendsClient);
            CloseClientSafely(_lobbyClient);
            _friendsClient = null;
            _friendsChannel = null;
            _lobbyClient = null;
            _lobbyChannel = null;
            _sessionToken = null;
            ClearCurrentRoom();
        }

        /// <summary>Pide de nuevo la lista de amigos; la respuesta llega por FriendListReceived.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        public bool RequestFriendList()
        {
            bool wasSent = false;

            if (HasFriendsChannel)
            {
                wasSent = TrySend(() => _friendsClient.SubscribeToFriends(_sessionToken), "pedir la lista de amigos");
            }

            return wasSent;
        }

        /// <summary>Invita a un amigo en línea a la sala; el resultado llega por InviteFriendResultReceived.</summary>
        /// <param name="friendUserId">Id de la cuenta del amigo, tal como llegó en la lista de amigos.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        public bool InviteFriendToRoom(int friendUserId)
        {
            bool wasSent = false;

            if (HasFriendsChannel)
            {
                wasSent = TrySend(
                    () => _friendsClient.InviteFriendToRoom(_sessionToken, friendUserId),
                    "invitar a un amigo a la sala");
            }

            return wasSent;
        }

        /// <summary>
        /// Acepta una invitación recibida; el resultado llega por AcceptInvitationResultReceived con el id de la sala,
        /// a la que se entra después con EnterLobby. Aceptar no mete al jugador en la sala.
        /// </summary>
        /// <param name="invitationId">Id de la invitación, tal como llegó en RoomInvitationReceived.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        public bool AcceptRoomInvitation(int invitationId)
        {
            bool wasSent = false;

            if (HasFriendsChannel)
            {
                wasSent = TrySend(
                    () => _friendsClient.AcceptRoomInvitation(_sessionToken, invitationId),
                    "aceptar una invitación a la sala");
            }

            return wasSent;
        }

        /// <summary>
        /// Abre el canal de lobby si todavía no está abierto. Las operaciones de sala lo abren solas; se expone para
        /// abrirlo antes de mostrar la sala.
        /// </summary>
        /// <returns>
        /// true si el canal de lobby está abierto; false si no hay sesión registrada o el servidor no respondió,
        /// caso en el que además se dispara ConnectionLost.
        /// </returns>
        public bool OpenLobbyChannel()
        {
            bool isOpen = _lobbyClient != null;

            if (!isOpen && HasFriendsChannel)
            {
                isOpen = TryOpenLobbyChannel();

                if (!isOpen)
                {
                    HandleConnectionFailure();
                }
            }

            return isOpen;
        }

        /// <summary>Crea una sala y deja al jugador dentro; el resultado llega por CreateRoomResultReceived.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        public bool CreateRoom()
        {
            bool wasSent = false;

            if (OpenLobbyChannel())
            {
                wasSent = TrySend(() => _lobbyClient.CreateRoom(_sessionToken), "crear una sala");
                IsRoomCreationPending = wasSent;
            }

            return wasSent;
        }

        /// <summary>
        /// Entra a una sala para la que el jugador aceptó una invitación; el resultado llega por
        /// EnterLobbyResultReceived.
        /// </summary>
        /// <param name="roomId">Id de la sala, tal como lo entregó AcceptInvitationResultReceived.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        public bool EnterLobby(int roomId)
        {
            bool wasSent = false;

            if (OpenLobbyChannel())
            {
                wasSent = TrySend(() => _lobbyClient.EnterLobby(_sessionToken, roomId), "entrar a una sala");
            }

            return wasSent;
        }

        /// <summary>
        /// Sale de la sala actual y la libera en el cliente sin esperar la respuesta, porque salir sin estar en una
        /// sala también es éxito en el servidor; el resultado llega por LeaveLobbyResultReceived.
        /// </summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        public bool LeaveLobby()
        {
            bool wasSent = false;
            ClearCurrentRoom();

            if (OpenLobbyChannel())
            {
                wasSent = TrySend(() => _lobbyClient.LeaveLobby(_sessionToken), "salir de una sala");
            }

            return wasSent;
        }

        /// <summary>Ejecuta una acción en el hilo de la interfaz, sin esperar a que termine.</summary>
        /// <param name="action">Acción que dispara un evento o toca controles; de una sola sentencia.</param>
        internal void RunOnInterface(Action action)
        {
            _interfaceDispatcher.BeginInvoke(action);
        }

        /// <summary>Dispara FriendListReceived.</summary>
        /// <param name="operationSubscribeToFriendsResult">Resultado que llegó del servidor.</param>
        internal void NotifyFriendListReceived(
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult)
        {
            FriendListReceived?.Invoke(
                this,
                new ServerEventArgs<OperationSubscribeToFriendsResult>(operationSubscribeToFriendsResult));
        }

        /// <summary>Dispara FriendPresenceChanged.</summary>
        /// <param name="friend">El amigo con su presencia actual.</param>
        internal void NotifyFriendPresenceChanged(Friend friend)
        {
            FriendPresenceChanged?.Invoke(this, new ServerEventArgs<Friend>(friend));
        }

        /// <summary>Dispara RoomInvitationReceived.</summary>
        /// <param name="roomInvitation">La invitación que llegó del servidor.</param>
        internal void NotifyRoomInvitationReceived(RoomInvitation roomInvitation)
        {
            RoomInvitationReceived?.Invoke(this, new ServerEventArgs<RoomInvitation>(roomInvitation));
        }

        /// <summary>Dispara InviteFriendResultReceived.</summary>
        /// <param name="operationInviteFriendToRoomResult">Resultado que llegó del servidor.</param>
        internal void NotifyInviteFriendResultReceived(
            OperationInviteFriendToRoomResult operationInviteFriendToRoomResult)
        {
            InviteFriendResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationInviteFriendToRoomResult>(operationInviteFriendToRoomResult));
        }

        /// <summary>Dispara AcceptInvitationResultReceived.</summary>
        /// <param name="operationAcceptRoomInvitationResult">Resultado que llegó del servidor.</param>
        internal void NotifyAcceptInvitationResultReceived(
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            AcceptInvitationResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationAcceptRoomInvitationResult>(operationAcceptRoomInvitationResult));
        }

        /// <summary>Dispara CreateRoomResultReceived.</summary>
        /// <param name="operationCreateRoomResult">Resultado que llegó del servidor.</param>
        /// <remarks>
        /// Guarda la sala solo si la creación seguía pendiente: si el jugador salió antes de que llegara el
        /// resultado, el servidor ya lo sacó de esa sala.
        /// </remarks>
        internal void NotifyCreateRoomResultReceived(OperationCreateRoomResult operationCreateRoomResult)
        {
            if (IsRoomCreationPending && operationCreateRoomResult.IsSuccess)
            {
                CurrentRoom = operationCreateRoomResult.Room;
            }

            IsRoomCreationPending = false;

            CreateRoomResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationCreateRoomResult>(operationCreateRoomResult));
        }

        /// <summary>Dispara EnterLobbyResultReceived.</summary>
        /// <param name="operationEnterLobbyResult">Resultado que llegó del servidor.</param>
        internal void NotifyEnterLobbyResultReceived(OperationEnterLobbyResult operationEnterLobbyResult)
        {
            if (operationEnterLobbyResult.IsSuccess)
            {
                CurrentRoom = operationEnterLobbyResult.Room;
            }

            EnterLobbyResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationEnterLobbyResult>(operationEnterLobbyResult));
        }

        /// <summary>Dispara LeaveLobbyResultReceived.</summary>
        /// <param name="operationLeaveLobbyResult">Resultado que llegó del servidor.</param>
        internal void NotifyLeaveLobbyResultReceived(OperationLeaveLobbyResult operationLeaveLobbyResult)
        {
            if (!operationLeaveLobbyResult.IsSuccess)
            {
                _log.WarnFormat(
                    "El servidor rechazó la salida de la sala. ErrorKey: {0}.",
                    operationLeaveLobbyResult.ErrorKey);
            }

            LeaveLobbyResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationLeaveLobbyResult>(operationLeaveLobbyResult));
        }

        /// <summary>
        /// Actualiza la sala actual y dispara LobbyRoomChanged. Ignora el aviso de una sala que ya no es la del
        /// jugador, como uno que venía en camino cuando salió.
        /// </summary>
        /// <param name="lobbyRoom">La sala con la lista actualizada de jugadores.</param>
        internal void NotifyLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            bool isCurrentRoom = CurrentRoom != null && lobbyRoom != null && lobbyRoom.RoomId == CurrentRoom.RoomId;

            if (isCurrentRoom)
            {
                CurrentRoom = lobbyRoom;
                LobbyRoomChanged?.Invoke(this, new ServerEventArgs<LobbyRoom>(lobbyRoom));
            }
            else
            {
                _log.Debug("Se ignoró el aviso de una sala que no es la sala actual del jugador.");
            }
        }

        private void ClearCurrentRoom()
        {
            CurrentRoom = null;
            IsRoomCreationPending = false;
        }

        private bool IsRegisteredSessionResult(OperationLogInResult operationLogInResult)
        {
            bool hasSuccess = operationLogInResult != null && operationLogInResult.IsSuccess;
            bool hasToken = hasSuccess && !string.IsNullOrEmpty(operationLogInResult.SessionToken);

            return hasToken;
        }

        private bool TryOpenFriendsChannel()
        {
            bool wasOpened = false;

            try
            {
                _friendsClient = new FriendsManagerClient(new InstanceContext(new ServerCallback(this)));
                _friendsChannel = _friendsClient.InnerChannel;
                WatchChannel(_friendsChannel);
                _friendsClient.Open();
                _friendsClient.SubscribeToFriends(_sessionToken);
                wasOpened = true;
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al abrir el canal de amigos.", ex);
            }
            catch (TimeoutException ex)
            {
                _log.Error("Se agotó el tiempo al abrir el canal de amigos.", ex);
            }
            catch (Exception ex)
            {
                // Último bloque de la cascada: por ejemplo, un endpoint ausente en App.config. Se registra y se
                // convierte en un resultado controlado para que el inicio de sesión no termine en un cierre.
                _log.Error("Fallo inesperado al abrir el canal de amigos.", ex);
            }

            return wasOpened;
        }

        private bool TryOpenLobbyChannel()
        {
            bool wasOpened = false;

            try
            {
                _lobbyClient = new LobbyManagerClient(new InstanceContext(new ServerCallback(this)));
                _lobbyChannel = _lobbyClient.InnerChannel;
                WatchChannel(_lobbyChannel);
                _lobbyClient.Open();
                wasOpened = true;
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al abrir el canal de lobby.", ex);
            }
            catch (TimeoutException ex)
            {
                _log.Error("Se agotó el tiempo al abrir el canal de lobby.", ex);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al abrir el canal de lobby.", ex);
            }

            return wasOpened;
        }

        private bool TrySend(Action sendOperation, string operationDescription)
        {
            bool wasSent = false;

            try
            {
                sendOperation();
                wasSent = true;
            }
            catch (CommunicationException ex)
            {
                _log.Error(string.Format("Error de comunicación al {0}.", operationDescription), ex);
                HandleConnectionFailure();
            }
            catch (TimeoutException ex)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0}.", operationDescription), ex);
                HandleConnectionFailure();
            }

            return wasSent;
        }

        private void WatchChannel(ICommunicationObject channel)
        {
            channel.Closed += OnServerChannelEnded;
            channel.Faulted += OnServerChannelEnded;
        }

        private void UnwatchChannel(ICommunicationObject channel)
        {
            if (channel != null)
            {
                channel.Closed -= OnServerChannelEnded;
                channel.Faulted -= OnServerChannelEnded;
            }
        }

        // Closed y Faulted llegan en un hilo de WCF: el estado solo se toca en el hilo de la interfaz.
        private void OnServerChannelEnded(object sender, EventArgs eventArguments)
        {
            RunOnInterface(() => HandleChannelEnded(sender));
        }

        // Registra el detalle técnico y no lo relanza: el jugador solo recibe ConnectionLost (hallazgo para §10.2).
        private void HandleChannelEnded(object channel)
        {
            bool isFriendsChannel = ReferenceEquals(channel, _friendsChannel);
            bool isCurrentChannel = isFriendsChannel || ReferenceEquals(channel, _lobbyChannel);

            if (isCurrentChannel)
            {
                CommunicationState channelState = ((ICommunicationObject)channel).State;
                _log.WarnFormat(
                    "Un canal con el servidor terminó sin que la aplicación lo pidiera. Estado: {0}.",
                    channelState);
                HandleConnectionFailure();
            }
        }

        private void HandleConnectionFailure()
        {
            Disconnect();
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }

        private void CloseClientSafely(ICommunicationObject client)
        {
            if (client != null)
            {
                try
                {
                    if (client.State == CommunicationState.Faulted)
                    {
                        client.Abort();
                    }
                    else
                    {
                        client.Close();
                    }
                }
                catch (CommunicationException ex)
                {
                    _log.Warn("No se pudo cerrar limpiamente un canal con el servidor.", ex);
                    client.Abort();
                }
                catch (TimeoutException ex)
                {
                    _log.Warn("Se agotó el tiempo al cerrar un canal con el servidor.", ex);
                    client.Abort();
                }
            }
        }
    }
}