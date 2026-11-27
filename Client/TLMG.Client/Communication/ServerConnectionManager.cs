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
        internal const int NoUserId = 0;
        private const int RoomOperationConfirmationMilliseconds = 10000;
        private const int NoInvitationId = 0;
        private const int NoRoomId = 0;
        private const string RoomOperationErrorKey = "Common_UnexpectedError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(ServerConnectionManager));

        private readonly Dispatcher _interfaceDispatcher;
        private readonly ModerationConnection _moderationConnection;
        private readonly ModerationChannel _moderationChannel;
        private FriendsManagerClient _friendsClient;
        private IClientChannel _friendsChannel;
        private ServerCallback _friendsCallback;
        private LobbyManagerClient _lobbyClient;
        private IClientChannel _lobbyChannel;
        private ServerCallback _lobbyCallback;
        private string _sessionToken;
        private int? _enteringRoomId;
        private long? _invitationAcceptanceUntil;
        private long? _roomEntryUntil;
        private int _pendingLeaveRequests;
        private int? _finishedLeaveRoomId;
        private Guid? _finishedLeaveMatchId;

        public ServerConnectionManager(Dispatcher interfaceDispatcher)
        {
            _interfaceDispatcher = interfaceDispatcher;
            _moderationChannel = new ModerationChannel(interfaceDispatcher, _heartbeatClock);
            _moderationConnection = new ModerationConnection(this, _moderationChannel);
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

        internal IModerationConnection Moderation { get { return _moderationConnection; } }

        internal string SessionToken { get { return _sessionToken; } }
        public bool HasSession { get { return !string.IsNullOrEmpty(_sessionToken); } }
        public bool IsGuestSession { get; private set; }
        public bool HasRegisteredSession { get { return HasSession && !IsGuestSession; } }

        public LobbyRoom CurrentRoom { get; private set; }

        public bool IsRoomCreationPending { get; private set; }

        internal bool CanReceiveRoomInvitation => HasSession && _friendsClient != null
            && CurrentRoom == null && !IsRoomCreationPending && _pendingLeaveRequests == 0
            && !_invitationAcceptanceUntil.HasValue && !_enteringRoomId.HasValue
            && !IsMatchRecoveryPending && !IsMatchRecoveryExhausted;

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
                RequestRecovery();
                DiscoverRecoveryContext(notifyMatchContextEnded: false);
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
            _invitationAcceptanceUntil = null;
            _roomEntryUntil = null;
            _moderationChannel.Close();
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
            void SendFriendList()
            {
                _friendsClient.SubscribeToFriends(_sessionToken);
            }

            bool wasRequestFriendListSent = false;

            if (_friendsClient == null && HasSession)
            {
                _friendsRecoveryAttempts = 0;
                wasRequestFriendListSent = true;
            }

            if (_friendsClient != null && HasSession)
            {
                wasRequestFriendListSent = TrySend(SendFriendList,
                    "pedir la lista de amigos", CloseFriendsChannel);
            }

            return wasRequestFriendListSent;
        }

        public bool InviteFriendToRoom(int friendUserId)
        {
            void SendInvitation()
            {
                _friendsClient.InviteFriendToRoom(_sessionToken, friendUserId);
            }

            bool wasInviteFriendToRoomSent = false;

            if (_friendsClient != null && HasSession)
            {
                wasInviteFriendToRoomSent = TrySend(
                    SendInvitation,
                    "invitar a un amigo a la sala", CloseFriendsChannel);
            }

            return wasInviteFriendToRoomSent;
        }

        public bool AcceptRoomInvitation(int invitationId)
        {
            void AcceptInvitation()
            {
                _friendsClient.AcceptRoomInvitation(_sessionToken, invitationId);
            }

            bool wasAcceptRoomInvitationSent = false;

            if (CanReceiveRoomInvitation && invitationId > NoInvitationId)
            {
                wasAcceptRoomInvitationSent = TrySend(
                    AcceptInvitation,
                    "aceptar una invitación a la sala", CloseFriendsChannel);

                if (wasAcceptRoomInvitationSent)
                {
                    _invitationAcceptanceUntil = _heartbeatClock.ElapsedMilliseconds
                        + RoomOperationConfirmationMilliseconds;
                }
            }

            return wasAcceptRoomInvitationSent;
        }

        public bool CreateRoom()
        {
            void SendRoomCreation()
            {
                _lobbyClient.CreateRoom(_sessionToken);
            }

            bool wasCreateRoomSent = false;

            if (HasRegisteredSession && CurrentRoom == null && !IsRoomCreationPending
                && !_invitationAcceptanceUntil.HasValue && !_enteringRoomId.HasValue
                && _pendingLeaveRequests == 0 && OpenLobbyChannel())
            {
                wasCreateRoomSent = TrySend(SendRoomCreation,
                    "crear una sala", CloseLobbyChannel);
                IsRoomCreationPending = wasCreateRoomSent;
            }

            return wasCreateRoomSent;
        }

        public bool EnterLobby(int roomId)
        {
            void SendRoomEntry()
            {
                _lobbyClient.EnterLobby(_sessionToken, roomId);
            }

            bool wasRequestEnterLobbySent = false;

            if (HasSession && roomId > NoRoomId && CurrentRoom == null && !IsRoomCreationPending
                && _pendingLeaveRequests == 0 && !_invitationAcceptanceUntil.HasValue
                && !_recoveryLobbyUntil.HasValue && !_enteringRoomId.HasValue && OpenLobbyChannel())
            {
                wasRequestEnterLobbySent = TrySend(SendRoomEntry,
                    "entrar a una sala", CloseLobbyChannel);

                if (wasRequestEnterLobbySent)
                {
                    _enteringRoomId = roomId;
                    _roomEntryUntil = _heartbeatClock.ElapsedMilliseconds + RoomOperationConfirmationMilliseconds;
                }
            }

            return wasRequestEnterLobbySent;
        }

        public bool LeaveLobby()
        {
            void SendRoomDeparture()
            {
                _lobbyClient.LeaveLobby(_sessionToken);
            }

            bool wasRequestLeaveLobbySent = false;
            if (!_finishedLeaveRoomId.HasValue)
            {
                ClearCurrentRoom();

                if (OpenLobbyChannel())
                {
                    _pendingLeaveRequests++;
                    wasRequestLeaveLobbySent = TrySend(SendRoomDeparture,
                        "salir de una sala", CloseLobbyChannel);
                }
            }

            return wasRequestLeaveLobbySent;
        }

        public bool LeaveFinishedMatch(Guid matchId)
        {
            void SendMatchDeparture()
            {
                _lobbyClient.LeaveLobby(_sessionToken);
            }

            bool wasSent = false;
            if (CurrentRoom != null && CurrentRoom.MatchId == matchId && _pendingLeaveRequests == 0
                && OpenLobbyChannel())
            {
                _finishedLeaveRoomId = CurrentRoom.RoomId;
                _finishedLeaveMatchId = matchId;
                _pendingLeaveRequests++;
                wasSent = TrySend(SendMatchDeparture, "salir de la partida terminada", CloseLobbyChannel);
            }

            return wasSent;
        }

        internal void RunOnInterface(Action action)
        {
            _interfaceDispatcher.BeginInvoke(action);
        }
        internal void RunOnInterfaceIfCurrent(ServerCallback callback, Action notification)
        {
            void DeliverCurrentNotification()
            {
                RunIfCurrentCallback(callback, notification);
            }

            RunOnInterface(DeliverCurrentNotification);
        }

        internal void NotifyPlayerExpelled(ServerCallback callback, int roomId)
        {
            bool isCurrentRoom = callback != null && ReferenceEquals(callback, _lobbyCallback)
                && CurrentRoom != null && CurrentRoom.RoomId == roomId && HasSession;

            if (isCurrentRoom)
            {
                bool wasInMatch = CurrentRoom.MatchId.HasValue;
                ClearCurrentRoom();
                CloseLobbyChannel();
                bool hasExpulsionSubscribers = _moderationConnection.NotifyPlayerExpelled(roomId);

                if (wasInMatch && !hasExpulsionSubscribers)
                {
                    MatchContextEnded?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _log.WarnFormat("Ignored expulsion notification for room {0}: no matching current lobby context.", roomId);
            }
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
            if (_invitationAcceptanceUntil.HasValue && HasSession)
            {
                if (_heartbeatClock.ElapsedMilliseconds >= _invitationAcceptanceUntil.Value)
                {
                    CloseFriendsChannel();
                }
                else
                {
                    _invitationAcceptanceUntil = null;
                    operationAcceptRoomInvitationResult = NormalizeOperationResult(operationAcceptRoomInvitationResult);
                    AcceptInvitationResultReceived?.Invoke(this,
                        new ServerEventArgs<OperationAcceptRoomInvitationResult>(operationAcceptRoomInvitationResult));
                }
            }
            else
            {
                _log.Warn("Ignored an invitation confirmation without a pending acceptance.");
            }
        }

        internal static ResultType NormalizeOperationResult<ResultType>(ResultType result)
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
            else if (_enteringRoomId.HasValue && _roomEntryUntil.HasValue)
            {
                if (_heartbeatClock.ElapsedMilliseconds >= _roomEntryUntil.Value)
                {
                    CloseLobbyChannel();
                }
                else
                {
                    LobbyRoom enteredRoom = operationEnterLobbyResult.Room;
                    bool isExpectedEntry = operationEnterLobbyResult.IsSuccess && IsEnteringRoom(enteredRoom);
                    _enteringRoomId = null;
                    _roomEntryUntil = null;

                    if (isExpectedEntry)
                    {
                        _lobbyBoundRoomId = enteredRoom.RoomId;
                        ApplyRoomSnapshot(enteredRoom);
                    }
                    else if (operationEnterLobbyResult.IsSuccess)
                    {
                        operationEnterLobbyResult = CreateFailedResult<OperationLobbyRoomResult>(RoomOperationErrorKey);
                        CloseLobbyChannel();
                    }

                    EnterLobbyResultReceived?.Invoke(this,
                        new ServerEventArgs<OperationLobbyRoomResult>(operationEnterLobbyResult));
                }
            }
            else
            {
                _log.Warn("Ignored a room entry confirmation without a pending entry.");
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
            _roomEntryUntil = null;
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
            void SubscribeToFriends()
            {
                _friendsClient.SubscribeToFriends(_sessionToken);
            }

            bool wasFriendsChannelOpened = false;

            try
            {
                _friendsCallback = new ServerCallback(this);
                _friendsClient = new FriendsManagerClient(new InstanceContext(_friendsCallback));
                _friendsChannel = _friendsClient.InnerChannel;
                WatchChannel(_friendsChannel);
                OpenConnection(_friendsClient);
                _friendsSubscriptionUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryReplyMilliseconds;
                SendConnectionRequest(_friendsChannel, SubscribeToFriends);
                wasFriendsChannelOpened = true;
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al abrir el canal de amigos.", communicationException);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("Se agotó el tiempo al abrir el canal de amigos.", timeoutException);
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
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al abrir el canal de lobby.", communicationException);
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("Se agotó el tiempo al abrir el canal de lobby.", timeoutException);
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
            catch (CommunicationException communicationException)
            {
                _log.Error(string.Format("Error de comunicación al {0}.", operationDescription), communicationException);
                onFailure();
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0}.", operationDescription), timeoutException);
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
            void HandleEndedChannel()
            {
                DisconnectIfCurrentChannelEnded(sender);
            }

            RunOnInterface(HandleEndedChannel);
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
            bool wasEntryPending = _roomEntryUntil.HasValue;
            _roomEntryUntil = null;
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

            if (wasEntryPending && HasSession)
            {
                EnterLobbyResultReceived?.Invoke(this, new ServerEventArgs<OperationLobbyRoomResult>(
                    CreateFailedResult<OperationLobbyRoomResult>(RoomOperationErrorKey)));
            }
        }

        private void CloseFriendsChannel()
        {
            FriendsManagerClient client = _friendsClient;
            bool wasAcceptancePending = _invitationAcceptanceUntil.HasValue;
            _invitationAcceptanceUntil = null;
            UnwatchChannel(_friendsChannel);
            _friendsClient = null;
            _friendsChannel = null;
            _friendsCallback = null;
            _friendsSubscriptionUntil = null;
            FriendList = null;
            client?.Abort();

            if (wasAcceptancePending && HasSession)
            {
                AcceptInvitationResultReceived?.Invoke(this, new ServerEventArgs<OperationAcceptRoomInvitationResult>(
                    CreateFailedResult<OperationAcceptRoomInvitationResult>(RoomOperationErrorKey)));
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
                catch (CommunicationException communicationException)
                {
                    _log.Warn("No se pudo cerrar limpiamente un canal con el servidor.", communicationException);
                    client.Abort();
                }
                catch (TimeoutException timeoutException)
                {
                    _log.Warn("Se agotó el tiempo al cerrar un canal con el servidor.", timeoutException);
                    client.Abort();
                }
            }
        }
    }
}