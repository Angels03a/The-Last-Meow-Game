using System.ServiceModel;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Recibe los callbacks del servidor en un hilo de WCF y los pasa al hilo de la interfaz como eventos del
    /// ServerConnectionManager que lo creó. No decide nada ni toca controles. UseSynchronizationContext = false evita
    /// el interbloqueo cuando la interfaz llama al servidor mientras espera un callback. Cada canal (amigos, lobby,
    /// chat, partida y consulta de la tabla de posiciones) crea su propia instancia, y el manager la compara por
    /// referencia para ignorar lo que llegue de un canal anterior.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ServerCallback : IFriendsManagerCallback, ILobbyManagerCallback, IChatManagerCallback,
        IMatchManagerCallback, ILeaderboardManagerCallback
    {
        private readonly ServerConnectionManager _serverConnection;

        /// <summary>Crea el receptor de callbacks de un manager.</summary>
        /// <param name="serverConnection">Manager cuyos eventos se disparan con lo que llega del servidor.</param>
        internal ServerCallback(ServerConnectionManager serverConnection)
        {
            _serverConnection = serverConnection;
        }

        /// <summary>Entrega la tabla de posiciones mediante el dispatcher de la interfaz.</summary>
        /// <param name="operationGetLeaderboardResult">Resultado con los jugadores o la clave de error.</param>
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

        /// <summary>Entrega el resultado de suscribirse a la partida mediante el dispatcher de la interfaz.</summary>
        /// <param name="operationSubscribeToMatchResult">Aceptación o clave de error.</param>
        public void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyMatchSubscriptionResult(this, operationSubscribeToMatchResult));
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

        /// <summary>Entrega la vista privada de la partida mediante el dispatcher de la interfaz.</summary>
        /// <param name="matchView">Vista del jugador, con su versión.</param>
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

        /// <summary>Entrega el resultado de invitar a un amigo a la sala.</summary>
        /// <param name="operationInviteFriendToRoomResult">Resultado con éxito o clave de error.</param>
        public void InviteFriendToRoomResult(OperationResult operationInviteFriendToRoomResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyInviteFriendResultReceived(operationInviteFriendToRoomResult));
        }

        /// <summary>Entrega el resultado de aceptar una invitación.</summary>
        /// <param name="operationAcceptRoomInvitationResult">Resultado con el id de sala o la clave de error.</param>
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

        /// <summary>Avisa que un amigo invitó al jugador a su sala.</summary>
        /// <param name="roomInvitation">La invitación recibida.</param>
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

        /// <summary>Entrega el resultado de cambiar las cartas por jugador de la sala.</summary>
        /// <param name="result">Resultado con la sala actualizada o la clave de error.</param>
        public void ChangeCardsPerPlayerResult(OperationLobbyRoomResult result)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyChangeCardsPerPlayerResultReceived(result));
        }

        /// <summary>Entrega el resultado de crear una sala.</summary>
        /// <param name="operationCreateRoomResult">Resultado con la sala o la clave de error.</param>
        public void CreateRoomResult(OperationLobbyRoomResult operationCreateRoomResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyCreateRoomResultReceived(operationCreateRoomResult));
        }

        /// <summary>Entrega el resultado de entrar a una sala.</summary>
        /// <param name="operationEnterLobbyResult">Resultado con la sala o la clave de error.</param>
        public void EnterLobbyResult(OperationLobbyRoomResult operationEnterLobbyResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyEnterLobbyResultReceived(operationEnterLobbyResult));
        }

        /// <summary>Entrega el resultado de salir de una sala.</summary>
        /// <param name="operationLeaveLobbyResult">Resultado con éxito o clave de error.</param>
        public void LeaveLobbyResult(OperationResult operationLeaveLobbyResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyLeaveLobbyResultReceived(operationLeaveLobbyResult));
        }

        /// <summary>Avisa que la sala del jugador cambió porque otro jugador entró o salió.</summary>
        /// <param name="lobbyRoom">La sala con la lista actualizada de jugadores.</param>
        public void OnLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyLobbyRoomChanged(lobbyRoom));
        }

        /// <summary>Entrega el resultado de ChangePlayerReadiness por el dispatcher de la interfaz.</summary>
        /// <param name="changePlayerReadinessResult">Resultado recibido del servidor.</param>
        public void ChangePlayerReadinessResult(OperationLobbyRoomResult changePlayerReadinessResult)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyReadinessResultReceived(changePlayerReadinessResult));
        }

        /// <summary>Entrega el resultado de RequestMatchStart por el dispatcher de la interfaz.</summary>
        /// <param name="result">Resultado recibido del servidor.</param>
        public void RequestMatchStartResult(OperationRequestMatchStartResult result)
        {
            _serverConnection.RunOnInterfaceIfCurrent(
                this,
                () => _serverConnection.NotifyMatchStartResultReceived(result));
        }
    }
}