using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para la lista de amigos con presencia y las invitaciones a sala; las respuestas y los avisos
    /// llegan por IFriendsManagerCallback.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IFriendsManagerCallback))]
    public interface IFriendsManager
    {
        /// <summary>
        /// Registra este canal como el canal de amigos de la sesión y deja al jugador en línea; la respuesta llega
        /// por SubscribeToFriendsResult.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToFriends(string sessionToken);

        /// <summary>
        /// Invita a un amigo en línea a la sala del jugador; la respuesta llega por InviteFriendToRoomResult.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador que invita.</param>
        /// <param name="friendUserId">Identificador de la cuenta del amigo al que se invita.</param>
        [OperationContract(IsOneWay = true)]
        void InviteFriendToRoom(string sessionToken, int friendUserId);

        /// <summary>
        /// Acepta una invitación recibida y autoriza al jugador a entrar a la sala; no lo mete en ella. La respuesta
        /// llega por AcceptRoomInvitationResult.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador que acepta.</param>
        /// <param name="invitationId">Identificador de la invitación recibida.</param>
        [OperationContract(IsOneWay = true)]
        void AcceptRoomInvitation(string sessionToken, int invitationId);

        /// <summary>
        /// Solicita amistad al jugador registrado con ese nickname; la respuesta llega por SendFriendRequestResult y,
        /// si el destinatario está en línea, él recibe OnFriendRequestReceived. Una solicitud cruzada se acepta sola y
        /// una rechazada se puede reactivar sin espera. Sin sesión de registrado (invitado) responde
        /// Common_InvalidSessionError.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador que solicita.</param>
        /// <param name="friendNickname">Nickname buscado; de 3 a 12 caracteres y no un correo.</param>
        [OperationContract(IsOneWay = true)]
        void SendFriendRequest(string sessionToken, string friendNickname);

        /// <summary>
        /// Acepta una solicitud de amistad recibida; la respuesta llega por AcceptFriendRequestResult y quien la envió,
        /// si está en línea, recibe OnFriendAdded. Solo la acepta el destinatario de una solicitud pendiente.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador que acepta.</param>
        /// <param name="friendRequestId">Identificador de la solicitud (FriendRequest.FriendRequestId).</param>
        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequest(string sessionToken, int friendRequestId);

        /// <summary>
        /// Rechaza una solicitud de amistad recibida; la respuesta llega por DeclineFriendRequestResult. Rechazar no se
        /// avisa a quien la envió.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador que rechaza.</param>
        /// <param name="friendRequestId">Identificador de la solicitud (FriendRequest.FriendRequestId).</param>
        [OperationContract(IsOneWay = true)]
        void DeclineFriendRequest(string sessionToken, int friendRequestId);
    }
}