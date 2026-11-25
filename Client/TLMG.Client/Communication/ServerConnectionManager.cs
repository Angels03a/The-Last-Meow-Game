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
        private ServerCallback _friendsCallback;
        private LobbyManagerClient _lobbyClient;
        private IClientChannel _lobbyChannel;
        private ServerCallback _lobbyCallback;
        private string _sessionToken;
        private int? _enteringRoomId;
        private int _pendingLeaveRequests;
        private int? _finishedLeaveRoomId;
        private Guid? _finishedLeaveMatchId;

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

        public event EventHandler<ServerEventArgs<OperationResult>> FinishedMatchLeaveResultReceived;

        public event EventHandler ConnectionLost;

        /// <summary>Gets whether there is any session, registered or guest.</summary>
        public bool HasSession => !string.IsNullOrEmpty(_sessionToken);

        /// <summary>Gets whether the session is a guest session, as decided by the server at log in.</summary>
        public bool IsGuestSession { get; private set; }

        /// <summary>Gets whether the session belongs to a registered account.</summary>
        public bool HasRegisteredSession => HasSession && !IsGuestSession;

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
                IsGuestSession = operationLogInResult.IsGuest;
                PlayerUserId = operationLogInResult.UserId;
                PlayerNickname = operationLogInResult.Nickname;
                wasRegisterSessionStarted = true;
                _startingSession = true;
                try
                {
                    RequestRecovery();
                    DiscoverRecoveryContext();
                }
                finally
                {
                    _startingSession = false;
                }
                if (HasSession)
                {
                    StartHeartbeat();
                }
                else
                {
                    wasRegisterSessionStarted = false;
                }
            }
            else
            {
                _log.Warn("Se ignoró un resultado de inicio de sesión que no trae una sesión.");
            }

            return wasRegisterSessionStarted;
        }

        public void Disconnect()
        {
            StopHeartbeat();
            ResetRecovery();
            UnwatchChannel(_friendsChannel);
            UnwatchChannel(_lobbyChannel);
            CloseClientSafely(_friendsClient);
            CloseClientSafely(_lobbyClient);
            _friendsClient = null;
            _friendsChannel = null;
            _friendsCallback = null;
            _friendsSubscriptionUntil = null;
            _lobbyClient = null;
            _lobbyChannel = null;
            _lobbyBoundRoomId = null;
            _lobbyCallback = null;
            _pendingLeaveRequests = 0;
            _finishedLeaveRoomId = null;
            _finishedLeaveMatchId = null;
            _sessionToken = null;
            IsGuestSession = false;
            PlayerUserId = NoUserId;
            PlayerNickname = null;
            FriendList = null;
            _pendingFriendRequests = null;
            ClearCurrentRoom();
        }

        public bool RequestFriendList()
        {
            bool wasRequestFriendListSent = false;

            if (_friendsClient == null && HasSession)
            {
                _friendsRecoveryAttempts = 0;
                wasRequestFriendListSent = true;
            }

            if (_friendsClient != null && HasSession)
            {
                wasRequestFriendListSent = TrySend(() => _friendsClient.SubscribeToFriends(_sessionToken),
                    "pedir la lista de amigos", CloseFriendsChannel);
            }

            return wasRequestFriendListSent;
        }

        public bool InviteFriendToRoom(int friendUserId)
        {
            bool wasInviteFriendToRoomSent = false;

            if (_friendsClient != null && HasSession)
            {
                wasInviteFriendToRoomSent = TrySend(
                    () => _friendsClient.InviteFriendToRoom(_sessionToken, friendUserId),
                    "invitar a un amigo a la sala", CloseFriendsChannel);
            }

            return wasInviteFriendToRoomSent;
        }

        public bool AcceptRoomInvitation(int invitationId)
        {
            bool wasAcceptRoomInvitationSent = false;

            if (_friendsClient != null && HasSession)
            {
                wasAcceptRoomInvitationSent = TrySend(
                    () => _friendsClient.AcceptRoomInvitation(_sessionToken, invitationId),
                    "aceptar una invitación a la sala", CloseFriendsChannel);
            }

            return wasAcceptRoomInvitationSent;
        }

        public bool CreateRoom()
        {
            bool wasCreateRoomSent = false;

            if (HasRegisteredSession && OpenLobbyChannel())
            {
                wasCreateRoomSent = TrySend(() => _lobbyClient.CreateRoom(_sessionToken),
                    "crear una sala", CloseLobbyChannel);
                IsRoomCreationPending = wasCreateRoomSent;
            }

            return wasCreateRoomSent;
        }

        public bool EnterLobby(int roomId)
        {
            bool wasRequestEnterLobbySent = false;

            if (!_recoveryLobbyUntil.HasValue && !_enteringRoomId.HasValue && OpenLobbyChannel())
            {
                wasRequestEnterLobbySent = TrySend(() => _lobbyClient.EnterLobby(_sessionToken, roomId),
                    "entrar a una sala", CloseLobbyChannel);

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
            if (!_finishedLeaveRoomId.HasValue)
            {
                ClearCurrentRoom();

                if (OpenLobbyChannel())
                {
                    _pendingLeaveRequests++;
                    wasRequestLeaveLobbySent = TrySend(() => _lobbyClient.LeaveLobby(_sessionToken),
                        "salir de una sala", CloseLobbyChannel);
                }
            }

            return wasRequestLeaveLobbySent;
        }

        public bool LeaveFinishedMatch(Guid matchId)
        {
            bool wasSent = false;
            if (CurrentRoom != null && CurrentRoom.MatchId == matchId && _pendingLeaveRequests == 0
                && OpenLobbyChannel())
            {
                _finishedLeaveRoomId = CurrentRoom.RoomId;
                _finishedLeaveMatchId = matchId;
                _pendingLeaveRequests++;
                wasSent = TrySend(() => _lobbyClient.LeaveLobby(_sessionToken), "salir de la partida terminada", CloseLobbyChannel);
            }

            return wasSent;
        }

        internal void RunOnInterface(Action action)
        {
            _interfaceDispatcher.BeginInvoke(action);
        }

        /// <summary>
        /// Passes a friends or lobby notification to the UI thread. It checks the channel when the notification runs
        /// because the channel may close after the notification arrives.
        /// </summary>
        /// <param name="callback">Receiver that delivered the notification.</param>
        /// <param name="notification">Notification to apply only if the receiver is still current.</param>
        internal void RunOnInterfaceIfCurrent(ServerCallback callback, Action notification)
        {
            RunOnInterface(() => RunIfCurrentCallback(callback, notification));
        }

        internal void NotifyFriendListReceived(
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult)
        {
            operationSubscribeToFriendsResult = NormalizeOperationResult(operationSubscribeToFriendsResult);
            if (operationSubscribeToFriendsResult.IsSuccess)
            {
                _friendsSubscriptionUntil = null;
                _friendsRecoveryAttempts = 0;
                FriendList = operationSubscribeToFriendsResult.Friends ?? Array.Empty<Friend>();
                StorePendingFriendRequests(operationSubscribeToFriendsResult.PendingFriendRequests);
            }

            FriendListReceived?.Invoke(
                this,
                new ServerEventArgs<OperationSubscribeToFriendsResult>(operationSubscribeToFriendsResult));
        }

        internal void NotifyFriendPresenceChanged(Friend friend)
        {
            bool isGuestGoneOffline = friend != null && friend.UserId < NoUserId && !friend.IsOnline;

            if (isGuestGoneOffline)
            {
                RemoveGuestFriend(friend.UserId);
            }
            else
            {
                UpdateFriendPresence(friend);
            }

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
            result = NormalizeOperationResult(result);
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
            result = NormalizeOperationResult(result);
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
            result = NormalizeOperationResult(result);
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
            result = NormalizeOperationResult(result);
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
            operationInviteFriendToRoomResult = NormalizeOperationResult(operationInviteFriendToRoomResult);
            InviteFriendResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationResult>(operationInviteFriendToRoomResult));
        }

        internal void NotifyAcceptInvitationResultReceived(
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            operationAcceptRoomInvitationResult = NormalizeOperationResult(operationAcceptRoomInvitationResult);
            AcceptInvitationResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationAcceptRoomInvitationResult>(operationAcceptRoomInvitationResult));
        }

        private static ResultType NormalizeOperationResult<ResultType>(ResultType result)
            where ResultType : OperationResult, new()
        {
            const string unexpectedErrorKey = "Common_UnexpectedError";

            if (result == null)
            {
                result = CreateFailedResult<ResultType>(unexpectedErrorKey);
            }
            else
            {
                NormalizeAccountResult(result, unexpectedErrorKey);
            }

            return result;
        }

        internal void NotifyCreateRoomResultReceived(OperationLobbyRoomResult operationCreateRoomResult)
        {
            operationCreateRoomResult = NormalizeOperationResult(operationCreateRoomResult);
            if (IsRoomCreationPending && operationCreateRoomResult.IsSuccess)
            {
                _lobbyBoundRoomId = operationCreateRoomResult.Room?.RoomId;
                ApplyRoomSnapshot(operationCreateRoomResult.Room);
            }

            IsRoomCreationPending = false;

            CreateRoomResultReceived?.Invoke(
                this,
                new ServerEventArgs<OperationLobbyRoomResult>(operationCreateRoomResult));
        }

        internal void NotifyEnterLobbyResultReceived(OperationLobbyRoomResult operationEnterLobbyResult)
        {
            operationEnterLobbyResult = NormalizeOperationResult(operationEnterLobbyResult);
            if (_recoveryLobbyUntil.HasValue)
            {
                CompleteRecoveryLobby(operationEnterLobbyResult);
            }
            else
            {
                LobbyRoom enteredRoom = operationEnterLobbyResult.Room;
                bool isExpectedEntry = operationEnterLobbyResult.IsSuccess && IsEnteringRoom(enteredRoom);
                bool isAlreadyShowingRoom = IsSameRoom(CurrentRoom, enteredRoom);

                if (isExpectedEntry || isAlreadyShowingRoom)
                {
                    _lobbyBoundRoomId = enteredRoom.RoomId;
                    ApplyRoomSnapshot(enteredRoom);
                }

                _enteringRoomId = null;

                EnterLobbyResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationLobbyRoomResult>(operationEnterLobbyResult));
            }
        }

        internal void NotifyLeaveLobbyResultReceived(OperationResult operationLeaveLobbyResult)
        {
            operationLeaveLobbyResult = NormalizeOperationResult(operationLeaveLobbyResult);
            bool isFinishedLeave = _finishedLeaveRoomId.HasValue && _pendingLeaveRequests == 1;
            if (_pendingLeaveRequests > 0)
            {
                _pendingLeaveRequests--;
            }

            if (isFinishedLeave)
            {
                bool isSameRoom = CurrentRoom != null && CurrentRoom.RoomId == _finishedLeaveRoomId.Value
                    && CurrentRoom.MatchId == _finishedLeaveMatchId;
                _finishedLeaveRoomId = null;
                _finishedLeaveMatchId = null;
                if (operationLeaveLobbyResult != null && operationLeaveLobbyResult.IsSuccess && isSameRoom)
                {
                    ClearCurrentRoom();
                }
                else if (operationLeaveLobbyResult == null || !isSameRoom)
                {
                    operationLeaveLobbyResult = new OperationResult
                    {
                        IsSuccess = false,
                        ErrorKey = "Common_UnexpectedError"
                    };
                }

                FinishedMatchLeaveResultReceived?.Invoke(
                    this, new ServerEventArgs<OperationResult>(operationLeaveLobbyResult));
            }
            else if (operationLeaveLobbyResult != null && !operationLeaveLobbyResult.IsSuccess)
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

        /// <summary>
        /// Applies notifications only from the current friends or lobby receiver. Each new channel has its own
        /// receiver, so old callbacks leave the current state unchanged.
        /// </summary>
        private void RunIfCurrentCallback(ServerCallback callback, Action notification)
        {
            bool isCurrentCallback = callback != null
                && (ReferenceEquals(callback, _friendsCallback) || ReferenceEquals(callback, _lobbyCallback));

            if (isCurrentCallback)
            {
                notification();
            }
            else
            {
                _log.Debug("Se ignoró un aviso de un canal con el servidor que ya no está vigente.");
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

        private void RemoveGuestFriend(int guestUserId)
        {
            if (FriendList != null)
            {
                List<Friend> remainingFriends = new List<Friend>();

                foreach (Friend listedFriend in FriendList)
                {
                    if (listedFriend.UserId != guestUserId)
                    {
                        remainingFriends.Add(listedFriend);
                    }
                }

                FriendList = remainingFriends;
            }

            RemovePendingFriendRequestsOfUser(guestUserId);
        }

        private void ClearCurrentRoom()
        {
            bool wasRecoveringLobby = _recoveryLobbyUntil.HasValue;
            CancelMatchRecovery();
            _lobbyBoundRoomId = null;
            CurrentRoom = null;
            if (wasRecoveringLobby)
            {
                CloseLobbyChannel();
            }
            CurrentMatchView = null;
            _matchViewAge.Reset();
            CloseChatChannel();
            CloseMatchChannel();
            Countdown.ResetTimerForMatchStart();
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
                _friendsCallback = new ServerCallback(this);
                _friendsClient = new FriendsManagerClient(new InstanceContext(_friendsCallback));
                _friendsChannel = _friendsClient.InnerChannel;
                WatchChannel(_friendsChannel);
                OpenConnection(_friendsClient);
                _friendsSubscriptionUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryReplyMilliseconds;
                SendConnectionRequest(_friendsChannel, () => _friendsClient.SubscribeToFriends(_sessionToken));
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

            if (!wasFriendsChannelOpened)
            {
                CloseFriendsChannel();
            }
            return wasFriendsChannelOpened;
        }

        private bool OpenLobbyChannel()
        {
            bool isLobbyChannelOpen = _lobbyClient != null;

            if (!isLobbyChannelOpen && HasSession)
            {
                isLobbyChannelOpen = TryOpenLobbyChannel();

                if (!isLobbyChannelOpen)
                {
                    CloseLobbyChannel();
                }
            }

            return isLobbyChannelOpen;
        }

        private bool TryOpenLobbyChannel()
        {
            bool wasLobbyChannelOpened = false;

            try
            {
                _lobbyCallback = new ServerCallback(this);
                _lobbyClient = new LobbyManagerClient(new InstanceContext(_lobbyCallback));
                _lobbyChannel = _lobbyClient.InnerChannel;
                WatchChannel(_lobbyChannel);
                OpenConnection(_lobbyClient);
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

        private bool TrySend(Action sendOperation, string operationDescription, Action onFailure)
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
                onFailure();
            }
            catch (TimeoutException ex)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0}.", operationDescription), ex);
                onFailure();
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

            if (isChatChannel)
            {
                CloseChatChannel();
            }
            else if (isFriendsChannel)
            {
                CloseFriendsChannel();
            }
            else if (isCurrentChannel)
            {
                CloseLobbyChannel();
            }
        }

        private void CloseLobbyChannel()
        {
            LobbyManagerClient client = _lobbyClient;
            UnwatchChannel(_lobbyChannel);
            _lobbyClient = null;
            _lobbyCallback = null;
            _lobbyChannel = null;
            _lobbyBoundRoomId = null;
            IsRoomCreationPending = false;
            _enteringRoomId = null;
            _recoveryLobbyUntil = null;
            _pendingLeaveRequests = 0;
            _finishedLeaveRoomId = null;
            _finishedLeaveMatchId = null;
            client?.Abort();
            if (CurrentRoom?.MatchId.HasValue == true)
            {
                RequestRecovery();
            }
            else if (CurrentRoom != null)
            {
                ClearCurrentRoom();
            }
        }

        private void CloseFriendsChannel()
        {
            FriendsManagerClient client = _friendsClient;
            UnwatchChannel(_friendsChannel);
            _friendsClient = null;
            _friendsChannel = null;
            _friendsCallback = null;
            _friendsSubscriptionUntil = null;
            FriendList = null;
            client?.Abort();
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
