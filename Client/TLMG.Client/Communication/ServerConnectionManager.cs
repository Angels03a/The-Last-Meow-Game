using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Threading;

using log4net;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Administra los canales de amigos, lobby y chat, y las cuentas con UsersManagerClient por solicitud.
    /// Guarda el sessionToken del jugador
    /// registrado, mantiene vivo su canal de amigos (estar en línea) y convierte los callbacks del servidor en
    /// eventos .NET que se disparan en el hilo de la interfaz. Los eventos llevan los DTO tal como llegan, con su
    /// ErrorKey y sin textos: la traducción ocurre en pantalla. Es una clase de instancia: App crea una sola y las
    /// pantallas la alcanzan con ((App)Application.Current).ServerConnection; como vive más que ellas, deben
    /// desuscribirse en OnScreenUnloaded (§14.2). Se usa solo desde el hilo de la interfaz y no crea hilos, tareas ni
    /// temporizadores propios; delega la cuenta regresiva a LobbyCountdown (D-60, ADR-08).
    /// </summary>
    internal sealed partial class ServerConnectionManager
    {
        private const int NoUserId = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(ServerConnectionManager));

        private readonly Dispatcher _interfaceDispatcher;
        private FriendsManagerClient _friendsClient;
        private IClientChannel _friendsChannel;
        private LobbyManagerClient _lobbyClient;
        private IClientChannel _lobbyChannel;
        private string _sessionToken;
        private int? _enteringRoomId;

        /// <summary>Crea el manager sin conexión alguna.</summary>
        /// <param name="interfaceDispatcher">Dispatcher del hilo de la interfaz, donde se disparan los eventos.</param>
        public ServerConnectionManager(Dispatcher interfaceDispatcher)
        {
            _interfaceDispatcher = interfaceDispatcher;
            Countdown.StartRequested += OnCountdownStartRequested;
        }

        /// <summary>Se dispara con la lista de amigos que responde el servidor a la suscripción.</summary>
        public event EventHandler<ServerEventArgs<OperationSubscribeToFriendsResult>> FriendListReceived;

        /// <summary>Se dispara cuando un amigo pasa a estar en línea o deja de estarlo.</summary>
        public event EventHandler<ServerEventArgs<Friend>> FriendPresenceChanged;

        /// <summary>Se dispara cuando otro jugador envía una solicitud de amistad al jugador.</summary>
        public event EventHandler<ServerEventArgs<FriendRequest>> FriendRequestReceived;

        /// <summary>Se dispara cuando una solicitud se aceptó y el nuevo amigo entra a la lista.</summary>
        public event EventHandler<ServerEventArgs<Friend>> FriendAdded;

        /// <summary>Se dispara con el resultado de enviar una solicitud de amistad.</summary>
        public event EventHandler<ServerEventArgs<OperationSendFriendRequestResult>>
            SendFriendRequestResultReceived;

        /// <summary>Se dispara con el resultado de aceptar una solicitud de amistad.</summary>
        public event EventHandler<ServerEventArgs<OperationAcceptFriendRequestResult>>
            AcceptFriendRequestResultReceived;

        /// <summary>Se dispara con el resultado de rechazar una solicitud de amistad.</summary>
        public event EventHandler<ServerEventArgs<OperationDeclineFriendRequestResult>>
            DeclineFriendRequestResultReceived;

        /// <summary>Se dispara con el resultado de cambiar las cartas por jugador de la sala.</summary>
        public event EventHandler<ServerEventArgs<OperationChangeCardsPerPlayerResult>>
            ChangeCardsPerPlayerResultReceived;

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

        /// <summary>Se dispara cuando cambian los jugadores de la sala del jugador.</summary>
        public event EventHandler<ServerEventArgs<LobbyRoom>> LobbyRoomChanged;

        /// <summary>
        /// Se dispara cuando un canal con el servidor termina sin que la aplicación lo haya pedido, o cuando falla un
        /// envío. El manager ya liberó todo; la aplicación debe regresar al inicio de sesión.
        /// </summary>
        public event EventHandler ConnectionLost;

        /// <summary>Obtiene si hay una sesión de jugador registrado con su canal de amigos abierto.</summary>
        public bool HasRegisteredSession => _friendsClient != null && !string.IsNullOrEmpty(_sessionToken);

        /// <summary>
        /// Obtiene la última foto de la sala del jugador que entregó el servidor, o null si no está en una sala. El
        /// manager la actualiza al recibir los callbacks de lobby aunque ninguna pantalla esté suscrita, para que la
        /// sala sobreviva a la pantalla que la muestra; se limpia al salir de la sala o al terminar la sesión.
        /// </summary>
        public LobbyRoom CurrentRoom { get; private set; }

        /// <summary>Obtiene si se pidió crear una sala y su resultado todavía no llega.</summary>
        public bool IsRoomCreationPending { get; private set; }

        /// <summary>
        /// Obtiene la última lista de amigos que entregó el servidor, con su presencia al día, o null si todavía no
        /// llega. Como la sala, la conserva el manager aunque ninguna pantalla esté suscrita: la lista llega justo
        /// después del login, antes de que exista FriendsScreen. Se limpia al terminar la sesión.
        /// </summary>
        public IReadOnlyList<Friend> FriendList { get; private set; }

        /// <summary>
        /// Obtiene el id de la cuenta del jugador en sesión, tal como lo entregó el inicio de sesión, o 0 si no hay
        /// sesión registrada. Solo sirve para mostrar: el servidor identifica al jugador por su token, nunca por este
        /// id. Se limpia al terminar la sesión.
        /// </summary>
        public int PlayerUserId { get; private set; }

        /// <summary>
        /// Obtiene el nickname del jugador en sesión, o null si no hay sesión registrada (por ejemplo, un invitado).
        /// Solo sirve para mostrar. Se limpia al terminar la sesión.
        /// </summary>
        public string PlayerNickname { get; private set; }

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
            bool wasRegisterSessionStarted = false;
            Disconnect();

            if (IsRegisteredSessionResult(operationLogInResult))
            {
                _sessionToken = operationLogInResult.SessionToken;
                PlayerUserId = operationLogInResult.UserId;
                PlayerNickname = operationLogInResult.Nickname;
                wasRegisterSessionStarted = TryOpenFriendsChannel();

                if (!wasRegisterSessionStarted)
                {
                    Disconnect();
                }
            }
            else
            {
                _log.Warn("Se ignoró un resultado de inicio de sesión que no trae una sesión registrada.");
            }

            return wasRegisterSessionStarted;
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
            PlayerUserId = NoUserId;
            PlayerNickname = null;
            FriendList = null;
            _pendingFriendRequests = null;
            ClearCurrentRoom();
        }

        /// <summary>Pide de nuevo la lista de amigos; la respuesta llega por FriendListReceived.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        public bool RequestFriendList()
        {
            bool wasRequestFriendListSent = false;

            if (HasRegisteredSession)
            {
                wasRequestFriendListSent = TrySend(() => _friendsClient.SubscribeToFriends(_sessionToken), 
                    "pedir la lista de amigos");
            }

            return wasRequestFriendListSent;
        }

        /// <summary>Invita a un amigo en línea a la sala; el resultado llega por InviteFriendResultReceived.</summary>
        /// <param name="friendUserId">Id de la cuenta del amigo, tal como llegó en la lista de amigos.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        public bool InviteFriendToRoom(int friendUserId)
        {
            bool wasInviteFriendToRoomSent = false;

            if (HasRegisteredSession)
            {
                wasInviteFriendToRoomSent = TrySend(
                    () => _friendsClient.InviteFriendToRoom(_sessionToken, friendUserId),
                    "invitar a un amigo a la sala");
            }

            return wasInviteFriendToRoomSent;
        }

        /// <summary>
        /// Acepta una invitación recibida; el resultado llega por AcceptInvitationResultReceived con el id de la sala,
        /// a la que se entra después con EnterLobby. Aceptar no mete al jugador en la sala.
        /// </summary>
        /// <param name="invitationId">Id de la invitación, tal como llegó en RoomInvitationReceived.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        public bool AcceptRoomInvitation(int invitationId)
        {
            bool wasAcceptRoomInvitationSent = false;

            if (HasRegisteredSession)
            {
                wasAcceptRoomInvitationSent = TrySend(
                    () => _friendsClient.AcceptRoomInvitation(_sessionToken, invitationId),
                    "aceptar una invitación a la sala");
            }

            return wasAcceptRoomInvitationSent;
        }

        /// <summary>Crea una sala y deja al jugador dentro; el resultado llega por CreateRoomResultReceived.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        public bool CreateRoom()
        {
            bool wasCreateRoomSent = false;

            if (OpenLobbyChannel())
            {
                wasCreateRoomSent = TrySend(() => _lobbyClient.CreateRoom(_sessionToken), 
                    "crear una sala");
                IsRoomCreationPending = wasCreateRoomSent;
            }

            return wasCreateRoomSent;
        }

        /// <summary>
        /// Entra a una sala para la que el jugador aceptó una invitación; el resultado llega por
        /// EnterLobbyResultReceived.
        /// </summary>
        /// <param name="roomId">Id de la sala, tal como lo entregó AcceptInvitationResultReceived.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        public bool EnterLobby(int roomId)
        {
            bool wasRequestEnterLobbySent = false;

            if (OpenLobbyChannel())
            {
                wasRequestEnterLobbySent = TrySend(() => _lobbyClient.EnterLobby(_sessionToken, roomId), 
                    "entrar a una sala");

                if (wasRequestEnterLobbySent)
                {
                    _enteringRoomId = roomId;
                }
            }

            return wasRequestEnterLobbySent;
        }

        /// <summary>
        /// Sale de la sala actual y la libera en el cliente sin esperar la respuesta, porque salir sin estar en una
        /// sala también es éxito en el servidor; si el servidor la rechaza, el manager lo registra en el log.
        /// </summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        public bool LeaveLobby()
        {
            bool wasRequestLeaveLobbySent = false;
            ClearCurrentRoom();

            if (OpenLobbyChannel())
            {
                wasRequestLeaveLobbySent = TrySend(() => _lobbyClient.LeaveLobby(_sessionToken), 
                    "salir de una sala");
            }

            return wasRequestLeaveLobbySent;
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
            if (operationSubscribeToFriendsResult.IsSuccess)
            {
                FriendList = operationSubscribeToFriendsResult.Friends ?? Array.Empty<Friend>();
                StorePendingFriendRequests(operationSubscribeToFriendsResult.PendingFriendRequests);
            }

            FriendListReceived?.Invoke(
                this,
                new ServerEventArgs<OperationSubscribeToFriendsResult>(operationSubscribeToFriendsResult));
        }

        /// <summary>Actualiza la presencia del amigo en la lista guardada y dispara FriendPresenceChanged.</summary>
        /// <param name="friend">El amigo con su presencia actual.</param>
        internal void NotifyFriendPresenceChanged(Friend friend)
        {
            UpdateFriendPresence(friend);
            FriendPresenceChanged?.Invoke(this, new ServerEventArgs<Friend>(friend));
        }

        /// <summary>Dispara FriendRequestReceived.</summary>
        /// <param name="friendRequest">La solicitud que llegó del servidor.</param>
        internal void NotifyFriendRequestReceived(FriendRequest friendRequest)
        {
            if (friendRequest != null)
            {
                AddPendingFriendRequest(friendRequest);
                FriendRequestReceived?.Invoke(this, new ServerEventArgs<FriendRequest>(friendRequest));
            }
        }

        /// <summary>Dispara FriendAdded.</summary>
        /// <param name="friend">El nuevo amigo que llegó del servidor.</param>
        internal void NotifyFriendAdded(Friend friend)
        {
            if (friend != null)
            {
                AddFriendToList(friend);
                FriendAdded?.Invoke(this, new ServerEventArgs<Friend>(friend));
            }
        }

        /// <summary>Dispara SendFriendRequestResultReceived.</summary>
        /// <param name="result">Resultado que llegó del servidor.</param>
        internal void NotifySendFriendRequestResultReceived(OperationSendFriendRequestResult result)
        {
            if (result != null)
            {
                if (result.IsSuccess && result.SentFriendRequest != null)
                {
                    AddPendingFriendRequest(result.SentFriendRequest);
                }

                if (result.IsSuccess && result.NewFriend != null)
                {
                    AddFriendToList(result.NewFriend);
                }

                SendFriendRequestResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationSendFriendRequestResult>(result));
            }
        }

        /// <summary>Dispara AcceptFriendRequestResultReceived.</summary>
        /// <param name="result">Resultado que llegó del servidor.</param>
        internal void NotifyAcceptFriendRequestResultReceived(OperationAcceptFriendRequestResult result)
        {
            if (result != null)
            {
                if (result.IsSuccess)
                {
                    RemovePendingFriendRequest(result.FriendRequestId);
                }

                if (result.IsSuccess && result.NewFriend != null)
                {
                    AddFriendToList(result.NewFriend);
                }

                AcceptFriendRequestResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationAcceptFriendRequestResult>(result));
            }
        }

        /// <summary>Dispara DeclineFriendRequestResultReceived.</summary>
        /// <param name="result">Resultado que llegó del servidor.</param>
        internal void NotifyDeclineFriendRequestResultReceived(OperationDeclineFriendRequestResult result)
        {
            if (result != null)
            {
                if (result.IsSuccess)
                {
                    RemovePendingFriendRequest(result.FriendRequestId);
                }

                DeclineFriendRequestResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationDeclineFriendRequestResult>(result));
            }
        }

        /// <summary>
        /// Aplica la sala que trae el resultado, con el estado Listo ya reiniciado por el servidor, y dispara
        /// ChangeCardsPerPlayerResultReceived.
        /// </summary>
        /// <param name="result">Resultado que llegó del servidor.</param>
        internal void NotifyChangeCardsPerPlayerResultReceived(OperationChangeCardsPerPlayerResult result)
        {
            if (result != null)
            {
                if (result.Room != null)
                {
                    NotifyLobbyRoomChanged(result.Room);
                }

                ChangeCardsPerPlayerResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationChangeCardsPerPlayerResult>(result));
            }
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
                ApplyRoomSnapshot(operationCreateRoomResult.LobbyRoom);
            }

            IsRoomCreationPending = false;

            CreateRoomResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationCreateRoomResult>(operationCreateRoomResult));
        }

        /// <summary>Dispara EnterLobbyResultReceived.</summary>
        /// <param name="operationEnterLobbyResult">Resultado que llegó del servidor.</param>
        /// <remarks>
        /// Guarda la sala solo si el jugador seguía entrando a ella: si salió antes de que llegara el resultado, el
        /// servidor ya lo sacó. Si un aviso de la misma sala llegó antes, se conserva la foto con mayor Version;
        /// las respuestas atrasadas nunca reemplazan una foto más reciente.
        /// </remarks>
        internal void NotifyEnterLobbyResultReceived(OperationEnterLobbyResult operationEnterLobbyResult)
        {
            LobbyRoom enteredRoom = operationEnterLobbyResult.Room;
            bool isExpectedEntry = operationEnterLobbyResult.IsSuccess && IsEnteringRoom(enteredRoom);
            bool isAlreadyShowingRoom = IsSameRoom(CurrentRoom, enteredRoom);

            if (isExpectedEntry || isAlreadyShowingRoom)
            {
                ApplyRoomSnapshot(enteredRoom);
            }

            _enteringRoomId = null;

            EnterLobbyResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationEnterLobbyResult>(operationEnterLobbyResult));
        }

        /// <summary>
        /// Registra en el log el rechazo de la salida de la sala. La sala ya se liberó en el cliente al enviar la
        /// salida, así que ninguna pantalla espera este resultado.
        /// </summary>
        /// <param name="operationLeaveLobbyResult">Resultado que llegó del servidor.</param>
        internal void NotifyLeaveLobbyResultReceived(OperationLeaveLobbyResult operationLeaveLobbyResult)
        {
            if (!operationLeaveLobbyResult.IsSuccess)
            {
                _log.WarnFormat(
                    "El servidor rechazó la salida de la sala. ErrorKey: {0}.",
                    operationLeaveLobbyResult.ErrorKey);
            }
        }

        /// <summary>
        /// Actualiza la sala actual y dispara LobbyRoomChanged. Acepta también el aviso de la sala a la que el jugador
        /// está entrando, que puede llegar antes que EnterLobbyResult; ignora el de una sala que ya no es la suya,
        /// como uno que venía en camino cuando salió.
        /// </summary>
        /// <param name="lobbyRoom">La sala con la lista actualizada de jugadores.</param>
        internal void NotifyLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            bool isCurrentRoom = IsSameRoom(CurrentRoom, lobbyRoom);
            bool isEnteringRoom = IsEnteringRoom(lobbyRoom);

            if (isCurrentRoom || isEnteringRoom)
            {
                ApplyRoomSnapshot(lobbyRoom);
            }
            else
            {
                _log.Debug("Se ignoró el aviso de una sala que no es la sala actual del jugador.");
            }
        }

        private void UpdateFriendPresence(Friend friend)
        {
            if (FriendList != null && friend != null)
            {
                foreach (Friend listedFriend in FriendList)
                {
                    if (listedFriend.UserId == friend.UserId)
                    {
                        listedFriend.IsOnline = friend.IsOnline;
                    }
                }
            }
        }

        private void ClearCurrentRoom()
        {
            CloseChatChannel();
            Countdown.ResetTimerForMatchStart();
            CurrentRoom = null;
            IsRoomCreationPending = false;
            _enteringRoomId = null;
        }

        private static bool IsSameRoom(LobbyRoom firstRoom, LobbyRoom secondRoom)
        {
            bool areBothRooms = firstRoom != null && secondRoom != null;
            bool isSameRoom = areBothRooms && firstRoom.RoomId == secondRoom.RoomId;

            return isSameRoom;
        }

        private bool IsEnteringRoom(LobbyRoom lobbyRoom)
        {
            bool isEnteringRoom = lobbyRoom != null && lobbyRoom.RoomId == _enteringRoomId;

            return isEnteringRoom;
        }

        private bool IsRegisteredSessionResult(OperationLogInResult operationLogInResult)
        {
            bool hasSuccess = operationLogInResult != null && operationLogInResult.IsSuccess;
            bool hasToken = hasSuccess && !string.IsNullOrEmpty(operationLogInResult.SessionToken);

            return hasToken;
        }

        private bool TryOpenFriendsChannel()
        {
            bool wasFriendsChannelOpened = false;

            try
            {
                _friendsClient = new FriendsManagerClient(new InstanceContext(new ServerCallback(this)));
                _friendsChannel = _friendsClient.InnerChannel;
                WatchChannel(_friendsChannel);
                _friendsClient.Open();
                _friendsClient.SubscribeToFriends(_sessionToken);
                wasFriendsChannelOpened = true;
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al abrir el canal de amigos.", ex);
            }
            catch (TimeoutException ex)
            {
                _log.Error("Se agotó el tiempo al abrir el canal de amigos.", ex);
            }

            return wasFriendsChannelOpened;
        }

        private bool OpenLobbyChannel()
        {
            bool isLobbyChannelOpen = _lobbyClient != null;

            if (!isLobbyChannelOpen && HasRegisteredSession)
            {
                isLobbyChannelOpen = TryOpenLobbyChannel();

                if (!isLobbyChannelOpen)
                {
                    DisconnectAndNotifyConnectionLost();
                }
            }

            return isLobbyChannelOpen;
        }

        private bool TryOpenLobbyChannel()
        {
            bool wasLobbyChannelOpened = false;

            try
            {
                _lobbyClient = new LobbyManagerClient(new InstanceContext(new ServerCallback(this)));
                _lobbyChannel = _lobbyClient.InnerChannel;
                WatchChannel(_lobbyChannel);
                _lobbyClient.Open();
                wasLobbyChannelOpened = true;
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al abrir el canal de lobby.", ex);
            }
            catch (TimeoutException ex)
            {
                _log.Error("Se agotó el tiempo al abrir el canal de lobby.", ex);
            }

            return wasLobbyChannelOpened;
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
                DisconnectAndNotifyConnectionLost();
            }
            catch (TimeoutException ex)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0}.", operationDescription), ex);
                DisconnectAndNotifyConnectionLost();
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

        private void OnServerChannelEnded(object sender, EventArgs eventArguments)
        {
            RunOnInterface(() => DisconnectIfCurrentChannelEnded(sender));
        }

        private void DisconnectIfCurrentChannelEnded(object channel)
        {
            bool isFriendsChannel = ReferenceEquals(channel, _friendsChannel);
            bool isCurrentChannel = isFriendsChannel || ReferenceEquals(channel, _lobbyChannel);

            bool isChatChannel = _chatClient != null && ReferenceEquals(channel, _chatClient.InnerChannel);

            if (isCurrentChannel || isChatChannel)
            {
                CommunicationState channelState = ((ICommunicationObject)channel).State;
                _log.WarnFormat(
                    "Un canal con el servidor terminó sin que la aplicación lo pidiera. Estado: {0}.",
                    channelState);
                DisconnectAndNotifyConnectionLost();
            }
        }

        private void DisconnectAndNotifyConnectionLost()
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