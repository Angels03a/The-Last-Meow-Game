using System;
using System.Collections.Generic;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Área de amigos de la conexión con el servidor: lista de amigos, solicitudes de amistad pendientes e
    /// invitaciones a la sala. FriendsViewModel depende solo de esta interfaz (D-70); la implementa
    /// ServerConnectionManager, que conserva el estado aunque ninguna pantalla esté abierta (D-60). Los eventos se
    /// disparan en el hilo de la interfaz y llevan la ErrorKey tal como la manda el servidor, sin textos. Las
    /// operaciones solo envían: el resultado llega por el evento correspondiente.
    /// </summary>
    public interface IFriendsConnection
    {
        /// <summary>Se dispara con la lista de amigos y las solicitudes pendientes que responde el servidor.</summary>
        event EventHandler<ServerEventArgs<OperationSubscribeToFriendsResult>> FriendListReceived;

        /// <summary>Se dispara cuando un amigo pasa a estar en línea o deja de estarlo.</summary>
        event EventHandler<ServerEventArgs<Friend>> FriendPresenceChanged;

        /// <summary>Se dispara cuando otro jugador envía una solicitud de amistad al jugador.</summary>
        event EventHandler<ServerEventArgs<FriendRequest>> FriendRequestReceived;

        /// <summary>Se dispara cuando una solicitud se aceptó y el nuevo amigo entró a la lista.</summary>
        event EventHandler<ServerEventArgs<Friend>> FriendAdded;

        /// <summary>Se dispara con el resultado de invitar a un amigo a la sala.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> InviteFriendResultReceived;

        /// <summary>Se dispara con el resultado de enviar una solicitud de amistad.</summary>
        event EventHandler<ServerEventArgs<OperationSendFriendRequestResult>> SendFriendRequestResultReceived;

        /// <summary>Se dispara con el resultado de aceptar una solicitud de amistad.</summary>
        event EventHandler<ServerEventArgs<OperationAcceptFriendRequestResult>>
            AcceptFriendRequestResultReceived;

        /// <summary>Se dispara con el resultado de rechazar una solicitud de amistad.</summary>
        event EventHandler<ServerEventArgs<OperationDeclineFriendRequestResult>>
            DeclineFriendRequestResultReceived;

        /// <summary>Obtiene si hay una sesión de jugador registrado; un invitado no la tiene.</summary>
        bool HasRegisteredSession { get; }

        /// <summary>Obtiene si el jugador está en una sala, requisito para invitar a un amigo.</summary>
        bool HasCurrentRoom { get; }

        /// <summary>Obtiene la última lista de amigos del servidor, o null si todavía no llega.</summary>
        IReadOnlyList<Friend> FriendList { get; }

        /// <summary>
        /// Obtiene las solicitudes pendientes, recibidas y enviadas, al día con los avisos del servidor, o null si la
        /// lista todavía no llega.
        /// </summary>
        IReadOnlyList<FriendRequest> PendingFriendRequests { get; }

        /// <summary>Pide de nuevo la lista de amigos; la respuesta llega por FriendListReceived.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        bool RequestFriendList();

        /// <summary>Invita a un amigo en línea a la sala; el resultado llega por InviteFriendResultReceived.</summary>
        /// <param name="friendUserId">Id de la cuenta del amigo, tal como llegó en la lista de amigos.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        bool InviteFriendToRoom(int friendUserId);

        /// <summary>
        /// Envía una solicitud de amistad por nickname; el resultado llega por SendFriendRequestResultReceived.
        /// </summary>
        /// <param name="friendNickname">Nickname capturado por el jugador; el servidor lo valida.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        bool SendFriendRequest(string friendNickname);

        /// <summary>Acepta una solicitud recibida; el resultado llega por AcceptFriendRequestResultReceived.</summary>
        /// <param name="friendRequestId">Id de la solicitud, tal como llegó en las pendientes.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        bool AcceptFriendRequest(int friendRequestId);

        /// <summary>
        /// Rechaza una solicitud recibida; el resultado llega por DeclineFriendRequestResultReceived.
        /// </summary>
        /// <param name="friendRequestId">Id de la solicitud, tal como llegó en las pendientes.</param>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el envío.</returns>
        bool DeclineFriendRequest(int friendRequestId);
    }
}
