using System.ServiceModel;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Receives server callbacks on a WCF thread and raises events on the UI thread through its
    /// ServerConnectionManager. It makes no game decisions and does not touch controls. UseSynchronizationContext =
    /// false prevents a deadlock when the UI calls the server while waiting for a callback. Each friends, lobby, chat,
    /// match or leaderboard channel creates its own receiver. The manager compares receiver references to ignore
    /// callbacks from old channels.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ServerCallback : IFriendsManagerCallback, ILobbyManagerCallback, IChatManagerCallback,
        IMatchManagerCallback, ILeaderboardManagerCallback
    {
        private readonly ServerConnectionManager _serverConnection;

        internal long? HeartbeatDue { get; set; }
        internal long LastHeartbeatEchoAt { get; set; }
        internal int HeartbeatSequence { get; set; }
        internal int PendingHeartbeat { get; set; }
        internal int ConfirmedHeartbeat { get; set; }

        public void FriendsHeartbeatEcho(int sequence, string errorKey) => HeartbeatEcho(sequence, errorKey);
        public void LobbyHeartbeatEcho(int sequence, string errorKey) => HeartbeatEcho(sequence, errorKey);
        public void ChatHeartbeatEcho(int sequence, string errorKey) => HeartbeatEcho(sequence, errorKey);
        public void MatchHeartbeatEcho(int sequence, string errorKey) => HeartbeatEcho(sequence, errorKey);

        private void HeartbeatEcho(int sequence, string errorKey)
        {
            _serverConnection.RunOnInterface(() => _serverConnection.NotifyHeartbeatEcho(this, sequence, errorKey));
        }

        /// <summary>Creates a callback receiver for a connection manager.</summary>
        /// <param name="serverConnection">Manager that raises events when server callbacks arrive.</param>
        internal ServerCallback(ServerConnectionManager serverConnection)
        {
            _serverConnection = serverConnection;
        }

        /// <summary>Delivers the leaderboard through the UI dispatcher.</summary>
        /// <param name="operationGetLeaderboardResult">Players returned by the server, or an error key.</param>
        public void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyLeaderboardResultReceived(this, operationGetLeaderboardResult));
        }

        /// <summary>Entrega el aviso de chat mediante el dispatcher de la interfaz.</summary>
        /// <param name="chatResult">Aviso recibido del servidor.</param>
        public void SubscribeToChatResult(OperationResult chatResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyChatSubscriptionResult(this, chatResult));
        }

        /// <summary>Entrega el aviso de chat mediante el dispatcher de la interfaz.</summary>
        /// <param name="chatResult">Aviso recibido del servidor.</param>
        public void SendChatMessageResult(OperationSendChatMessageResult chatResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyChatSendResult(this, chatResult));
        }

        /// <summary>Entrega el aviso de chat mediante el dispatcher de la interfaz.</summary>
        /// <param name="chatResult">Aviso recibido del servidor.</param>
        public void OnChatMessageReceived(ChatMessage chatResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyChatMessageReceived(this, chatResult));
        }

        /// <summary>Delivers the match subscription result through the UI dispatcher.</summary>
        /// <param name="operationSubscribeToMatchResult">Subscription confirmation or error key.</param>
        public void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyMatchSubscriptionResult(this, operationSubscribeToMatchResult));
        }

        /// <summary>Delivers the player's catch attempt result.</summary>
        public void TryCatchFugitiveKittenResult(OperationResult result)
        {
            _serverConnection.RunOnInterface(() => _serverConnection.NotifyFugitiveKittenResult(this, result, true));
        }

        /// <summary>Delivers the player's deadline check result.</summary>
        public void CheckFugitiveKittenDeadlineResult(OperationResult result)
        {
            _serverConnection.RunOnInterface(() => _serverConnection.NotifyFugitiveKittenResult(this, result, false));
        }

        public void PlaceCardResult(OperationResult operationPlaceCardResult)
        {
            DispatchMatchActionResult(operationPlaceCardResult);
        }

        public void PlaceBetResult(OperationResult operationPlaceBetResult)
        {
            DispatchMatchActionResult(operationPlaceBetResult);
        }

        public void WithdrawFromBetResult(OperationResult operationWithdrawFromBetResult)
        {
            DispatchMatchActionResult(operationWithdrawFromBetResult);
        }

        public void RevealCardResult(OperationResult operationRevealCardResult)
        {
            DispatchMatchActionResult(operationRevealCardResult);
        }

        public void ChoosePunishmentCardResult(OperationResult operationChoosePunishmentCardResult)
        {
            DispatchMatchActionResult(operationChoosePunishmentCardResult);
        }

        private void DispatchMatchActionResult(OperationResult result)
        {
            _serverConnection.RunOnInterface(() => _serverConnection.NotifyMatchActionResult(this, result));
        }

        /// <summary>Delivers the private match view through the UI dispatcher.</summary>
        /// <param name="matchView">Player's view and its version.</param>
        public void OnMatchViewChanged(MatchView matchView)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyMatchViewReceived(this, matchView));
        }

        /// <summary>Entrega el resultado de suscribirse a los amigos.</summary>
        /// <param name="operationSubscribeToFriendsResult">Resultado con los amigos o la clave de error.</param>
        public void SubscribeToFriendsResult(OperationSubscribeToFriendsResult operationSubscribeToFriendsResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyFriendListReceived(operationSubscribeToFriendsResult));
        }

        /// <summary>Delivers the result of inviting a friend to the room.</summary>
        /// <param name="operationInviteFriendToRoomResult">Success result or error key.</param>
        public void InviteFriendToRoomResult(OperationResult operationInviteFriendToRoomResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyInviteFriendResultReceived(operationInviteFriendToRoomResult));
        }

        /// <summary>Delivers the result of accepting a room invitation.</summary>
        /// <param name="operationAcceptRoomInvitationResult">Room ID or error key.</param>
        public void AcceptRoomInvitationResult(OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyAcceptInvitationResultReceived(
                    operationAcceptRoomInvitationResult));
        }

        /// <summary>Avisa que un amigo pasó a estar en línea o dejó de estarlo.</summary>
        /// <param name="friend">El amigo con su presencia actual.</param>
        public void OnFriendPresenceChanged(Friend friend)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyFriendPresenceChanged(friend));
        }

        /// <summary>Reports that a friend invited the player to a room.</summary>
        /// <param name="roomInvitation">Room invitation received.</param>
        public void OnRoomInvitationReceived(RoomInvitation roomInvitation)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyRoomInvitationReceived(roomInvitation));
        }

        /// <summary>Entrega la solicitud de amistad recibida mediante el dispatcher de la interfaz.</summary>
        /// <param name="friendRequest">La solicitud recibida del servidor.</param>
        public void OnFriendRequestReceived(FriendRequest friendRequest)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyFriendRequestReceived(friendRequest));
        }

        /// <summary>Avisa que un nuevo amigo entró a la lista del jugador.</summary>
        /// <param name="friend">El nuevo amigo con su presencia actual.</param>
        public void OnFriendAdded(Friend friend)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyFriendAdded(friend));
        }

        /// <summary>Entrega el resultado de enviar una solicitud de amistad.</summary>
        /// <param name="operationSendFriendRequestResult">Resultado con éxito o clave de error.</param>
        public void SendFriendRequestResult(
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifySendFriendRequestResultReceived(
                    operationSendFriendRequestResult));
        }

        /// <summary>Entrega el resultado de aceptar una solicitud de amistad.</summary>
        /// <param name="operationAcceptFriendRequestResult">Resultado con éxito o clave de error.</param>
        public void AcceptFriendRequestResult(
            OperationAcceptFriendRequestResult operationAcceptFriendRequestResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyAcceptFriendRequestResultReceived(
                    operationAcceptFriendRequestResult));
        }

        /// <summary>Entrega el resultado de rechazar una solicitud de amistad.</summary>
        /// <param name="operationDeclineFriendRequestResult">Resultado con éxito o clave de error.</param>
        public void DeclineFriendRequestResult(
            OperationDeclineFriendRequestResult operationDeclineFriendRequestResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyDeclineFriendRequestResultReceived(
                    operationDeclineFriendRequestResult));
        }

        /// <summary>Delivers the result of changing the number of cards per player.</summary>
        /// <param name="result">Updated room or error key.</param>
        public void ChangeCardsPerPlayerResult(OperationLobbyRoomResult result)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyChangeCardsPerPlayerResultReceived(result));
        }

        /// <summary>Delivers the room creation result.</summary>
        /// <param name="operationCreateRoomResult">Room or error key.</param>
        public void CreateRoomResult(OperationLobbyRoomResult operationCreateRoomResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyCreateRoomResultReceived(operationCreateRoomResult));
        }

        /// <summary>Delivers the result of joining a room.</summary>
        /// <param name="operationEnterLobbyResult">Room or error key.</param>
        public void EnterLobbyResult(OperationLobbyRoomResult operationEnterLobbyResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyEnterLobbyResultReceived(operationEnterLobbyResult));
        }

        /// <summary>Delivers the result of leaving a room.</summary>
        /// <param name="operationLeaveLobbyResult">Success result or error key.</param>
        public void LeaveLobbyResult(OperationResult operationLeaveLobbyResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyLeaveLobbyResultReceived(operationLeaveLobbyResult));
        }

        /// <summary>Reports a room change when another player joins or leaves.</summary>
        /// <param name="lobbyRoom">Room with its updated player list.</param>
        public void OnLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyLobbyRoomChanged(lobbyRoom));
        }

        /// <summary>Delivers the ChangePlayerReadiness result through the UI dispatcher.</summary>
        /// <param name="changePlayerReadinessResult">Result received from the server.</param>
        public void ChangePlayerReadinessResult(OperationLobbyRoomResult changePlayerReadinessResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyReadinessResultReceived(changePlayerReadinessResult));
        }

        /// <summary>Delivers the RequestMatchStart result through the UI dispatcher.</summary>
        /// <param name="result">Result received from the server.</param>
        public void RequestMatchStartResult(OperationRequestMatchStartResult result)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyMatchStartResultReceived(result));
        }
    }
}