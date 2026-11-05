using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Recibe los callbacks del servidor en un hilo de WCF y los pasa al hilo de la interfaz como eventos del
    /// ServerConnectionManager que lo creó. No decide nada ni toca controles. UseSynchronizationContext = false evita
    /// el interbloqueo cuando la interfaz llama al servidor mientras espera un callback.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ServerCallback : IFriendsManagerCallback, ILobbyManagerCallback
    {
        private readonly ServerConnectionManager _serverConnection;

        /// <summary>Crea el receptor de callbacks de un manager.</summary>
        /// <param name="serverConnection">Manager cuyos eventos se disparan con lo que llega del servidor.</param>
        internal ServerCallback(ServerConnectionManager serverConnection)
        {
            _serverConnection = serverConnection;
        }

        /// <summary>Entrega el resultado de suscribirse a los amigos.</summary>
        /// <param name="operationSubscribeToFriendsResult">Resultado con los amigos o la clave de error.</param>
        public void SubscribeToFriendsResult(OperationSubscribeToFriendsResult operationSubscribeToFriendsResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyFriendListReceived(operationSubscribeToFriendsResult));
        }

        /// <summary>Entrega el resultado de invitar a un amigo a la sala.</summary>
        /// <param name="operationInviteFriendToRoomResult">Resultado con éxito o clave de error.</param>
        public void InviteFriendToRoomResult(OperationInviteFriendToRoomResult operationInviteFriendToRoomResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyInviteFriendResultReceived(operationInviteFriendToRoomResult));
        }

        /// <summary>Entrega el resultado de aceptar una invitación.</summary>
        /// <param name="operationAcceptRoomInvitationResult">Resultado con el id de sala o la clave de error.</param>
        public void AcceptRoomInvitationResult(OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyAcceptInvitationResultReceived(
                    operationAcceptRoomInvitationResult));
        }

        /// <summary>Avisa que un amigo pasó a estar en línea o dejó de estarlo.</summary>
        /// <param name="friend">El amigo con su presencia actual.</param>
        public void OnFriendPresenceChanged(Friend friend)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyFriendPresenceChanged(friend));
        }

        /// <summary>Avisa que un amigo invitó al jugador a su sala.</summary>
        /// <param name="roomInvitation">La invitación recibida.</param>
        public void OnRoomInvitationReceived(RoomInvitation roomInvitation)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyRoomInvitationReceived(roomInvitation));
        }

        /// <summary>Entrega el resultado de crear una sala.</summary>
        /// <param name="operationCreateRoomResult">Resultado con la sala o la clave de error.</param>
        public void CreateRoomResult(OperationCreateRoomResult operationCreateRoomResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyCreateRoomResultReceived(operationCreateRoomResult));
        }

        /// <summary>Entrega el resultado de entrar a una sala.</summary>
        /// <param name="operationEnterLobbyResult">Resultado con la sala o la clave de error.</param>
        public void EnterLobbyResult(OperationEnterLobbyResult operationEnterLobbyResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyEnterLobbyResultReceived(operationEnterLobbyResult));
        }

        /// <summary>Entrega el resultado de salir de una sala.</summary>
        /// <param name="operationLeaveLobbyResult">Resultado con éxito o clave de error.</param>
        public void LeaveLobbyResult(OperationLeaveLobbyResult operationLeaveLobbyResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyLeaveLobbyResultReceived(operationLeaveLobbyResult));
        }

        /// <summary>Avisa que la sala del jugador cambió porque otro jugador entró o salió.</summary>
        /// <param name="lobbyRoom">La sala con la lista actualizada de jugadores.</param>
        public void OnLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyLobbyRoomChanged(lobbyRoom));
        }

        public void ChangePlayerReadinessResult(OperationChangePlayerReadinessResult result)
        {
            throw new System.NotImplementedException();
        }

        public void RequestMatchStartResult(OperationRequestMatchStartResult result)
        {
            throw new System.NotImplementedException();
        }
    }
}