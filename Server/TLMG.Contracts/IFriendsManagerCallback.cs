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

        /// <summary>Avisa que un amigo invitó al jugador a su sala.</summary>
        /// <param name="roomInvitation">La invitación recibida.</param>
        [OperationContract(IsOneWay = true)]
        void OnRoomInvitationReceived(RoomInvitation roomInvitation);
    }
}