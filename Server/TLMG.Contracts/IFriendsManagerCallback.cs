using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Callback con el que el servidor responde las operaciones de amigos y avisa de invitaciones; lo implementa
    /// el cliente.
    /// </summary>
    [ServiceContract]
    public interface IFriendsManagerCallback
    {
        /// <summary>Entrega el resultado de suscribirse a los amigos al cliente.</summary>
        /// <param name="operationSubscribeToFriendsResult">Resultado con los amigos o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToFriendsResult(OperationSubscribeToFriendsResult operationSubscribeToFriendsResult);

        /// <summary>Entrega el resultado de invitar a un amigo a la sala al cliente.</summary>
        /// <param name="operationInviteFriendToRoomResult">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void InviteFriendToRoomResult(OperationInviteFriendToRoomResult operationInviteFriendToRoomResult);

        /// <summary>Entrega el resultado de aceptar una invitación al cliente.</summary>
        /// <param name="operationAcceptRoomInvitationResult">Resultado con el id de sala o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptRoomInvitationResult(OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult);

        /// <summary>Avisa que un amigo aceptado pasó a estar en línea o dejó de estarlo.</summary>
        /// <param name="friend">El amigo con su presencia actual.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendPresenceChanged(Friend friend);

        /// <summary>Avisa que un amigo invitó al jugador a su sala.</summary>
        /// <param name="roomInvitation">La invitación recibida.</param>
        [OperationContract(IsOneWay = true)]
        void OnRoomInvitationReceived(RoomInvitation roomInvitation);

        /// <summary>Entrega el resultado de SendFriendRequest al solicitante.</summary>
        /// <param name="operationSendFriendRequestResult">Resultado con la solicitud, el amigo o el error.</param>
        [OperationContract(IsOneWay = true)]
        void SendFriendRequestResult(OperationSendFriendRequestResult operationSendFriendRequestResult);

        /// <summary>Entrega el resultado de AcceptFriendRequest a quien aceptó.</summary>
        /// <param name="operationAcceptFriendRequestResult">Resultado con el amigo nuevo o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequestResult(OperationAcceptFriendRequestResult operationAcceptFriendRequestResult);

        /// <summary>Entrega el resultado de DeclineFriendRequest a quien rechazó.</summary>
        /// <param name="operationDeclineFriendRequestResult">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void DeclineFriendRequestResult(OperationDeclineFriendRequestResult operationDeclineFriendRequestResult);

        /// <summary>Avisa al destinatario en línea que otro jugador le envió una solicitud de amistad.</summary>
        /// <param name="friendRequest">La solicitud recibida, con IsIncoming en true.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendRequestReceived(FriendRequest friendRequest);

        /// <summary>Avisa a quien envió la solicitud, si está en línea, que quedó aceptada.</summary>
        /// <param name="friend">El amigo nuevo con su presencia actual.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendAdded(Friend friend);
    }
}