using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Threading;
using log4net;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
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

        public ServerConnectionManager(Dispatcher interfaceDispatcher)
        {
            _interfaceDispatcher = interfaceDispatcher;
            Countdown.StartRequested += OnCountdownStartRequested;
        }

        public event EventHandler<ServerEventArgs<OperationSubscribeToFriendsResult>> FriendListReceived;

        public event EventHandler<ServerEventArgs<Friend>> FriendPresenceChanged;

        public event EventHandler<ServerEventArgs<FriendRequest>> FriendRequestReceived;

        public event EventHandler<ServerEventArgs<Friend>> FriendAdded;

        public event EventHandler<ServerEventArgs<OperationSendFriendRequestResult>>
            SendFriendRequestResultReceived;

        public event EventHandler<ServerEventArgs<OperationAcceptFriendRequestResult>>
            AcceptFriendRequestResultReceived;

        public event EventHandler<ServerEventArgs<OperationDeclineFriendRequestResult>>
            DeclineFriendRequestResultReceived;

        public event EventHandler<ServerEventArgs<OperationLobbyRoomResult>>
            ChangeCardsPerPlayerResultReceived;

        public event EventHandler<ServerEventArgs<RoomInvitation>> RoomInvitationReceived;

        public event EventHandler<ServerEventArgs<OperationResult>>
            InviteFriendResultReceived;

        public event EventHandler<ServerEventArgs<OperationAcceptRoomInvitationResult>>
            AcceptInvitationResultReceived;

        public event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> CreateRoomResultReceived;

        public event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> EnterLobbyResultReceived;

        public event EventHandler<ServerEventArgs<LobbyRoom>> LobbyRoomChanged;

        public event EventHandler ConnectionLost;

        public bool HasRegisteredSession => _friendsClient != null && !string.IsNullOrEmpty(_sessionToken);

        public LobbyRoom CurrentRoom { get; private set; }

        public bool IsRoomCreationPending { get; private set; }

        public IReadOnlyList<Friend> FriendList { get; private set; }

        public int PlayerUserId { get; private set; }

        public string PlayerNickname { get; private set; }

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

        internal void RunOnInterface(Action action)
        {
            _interfaceDispatcher.BeginInvoke(action);
        }

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

        internal void NotifyFriendPresenceChanged(Friend friend)
        {
            UpdateFriendPresence(friend);
            FriendPresenceChanged?.Invoke(this, new ServerEventArgs<Friend>(friend));
        }

        internal void NotifyFriendRequestReceived(FriendRequest friendRequest)
        {
            if (friendRequest != null)
            {
                AddPendingFriendRequest(friendRequest);
                FriendRequestReceived?.Invoke(this, new ServerEventArgs<FriendRequest>(friendRequest));
            }
        }

        internal void NotifyFriendAdded(Friend friend)
        {
            if (friend != null)
            {
                AddFriendToList(friend);
                FriendAdded?.Invoke(this, new ServerEventArgs<Friend>(friend));
            }
        }

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

        internal void NotifyChangeCardsPerPlayerResultReceived(OperationLobbyRoomResult result)
        {
            if (result != null)
            {
                if (result.Room != null)
                {
                    NotifyLobbyRoomChanged(result.Room);
                }

                ChangeCardsPerPlayerResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationLobbyRoomResult>(result));
            }
        }

        internal void NotifyRoomInvitationReceived(RoomInvitation roomInvitation)
        {
            RoomInvitationReceived?.Invoke(this, new ServerEventArgs<RoomInvitation>(roomInvitation));
        }

        internal void NotifyInviteFriendResultReceived(
            OperationResult operationInviteFriendToRoomResult)
        {
            InviteFriendResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationResult>(operationInviteFriendToRoomResult));
        }

        internal void NotifyAcceptInvitationResultReceived(
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            AcceptInvitationResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationAcceptRoomInvitationResult>(operationAcceptRoomInvitationResult));
        }

        internal void NotifyCreateRoomResultReceived(OperationLobbyRoomResult operationCreateRoomResult)
        {
            if (IsRoomCreationPending && operationCreateRoomResult.IsSuccess)
            {
                ApplyRoomSnapshot(operationCreateRoomResult.Room);
            }

            IsRoomCreationPending = false;

            CreateRoomResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationLobbyRoomResult>(operationCreateRoomResult));
        }

        internal void NotifyEnterLobbyResultReceived(OperationLobbyRoomResult operationEnterLobbyResult)
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
                new ServerEventArgs<OperationLobbyRoomResult>(operationEnterLobbyResult));
        }

        internal void NotifyLeaveLobbyResultReceived(OperationResult operationLeaveLobbyResult)
        {
            if (!operationLeaveLobbyResult.IsSuccess)
            {
                _log.WarnFormat(
                    "El servidor rechazó la salida de la sala. ErrorKey: {0}.",
                    operationLeaveLobbyResult.ErrorKey);
            }
        }

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