using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Registro en memoria de las sesiones iniciadas y de las salas, compartido entre todas las sesiones de WCF.
    /// </summary>
    public interface ISessionRegistry
    {
        /// <summary>
        /// Inicia una sesión y reemplaza la anterior de la misma cuenta, si existe; la sesión anterior
        /// pierde también su sala, su callback y sus autorizaciones de entrada.
        /// </summary>
        /// <param name="userId">Identificador de la cuenta.</param>
        /// <param name="nickname">Nickname de la cuenta.</param>
        /// <param name="replacedSession">
        /// Lo que hay que avisar por la sesión anterior que se reemplazó; null si la cuenta no tenía otra.
        /// </param>
        /// <returns>El token de la sesión nueva; nunca es null.</returns>
        string StartSession(int userId, string nickname, out SessionEnd replacedSession);

        /// <summary>Busca la sesión de un token.</summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="playerSession">La sesión encontrada; null si no existe.</param>
        /// <returns>true si el token pertenece a una sesión vigente; en caso contrario, false.</returns>
        bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession);

        /// <summary>
        /// Cierra la sesión de un token; el jugador pierde también su sala, su callback y sus autorizaciones.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="sessionEnd">Lo que hay que avisar por la sesión cerrada; null si no había sesión.</param>
        /// <returns>true si había una sesión vigente y se cerró; en caso contrario, false.</returns>
        bool EndSession(string sessionToken, out SessionEnd sessionEnd);

        /// <summary>
        /// Termina la sesión del jugador cuyo canal de amigos se cerró o falló; si el canal ya no es el registrado
        /// (porque se reemplazó) no hace nada.
        /// </summary>
        /// <param name="channelId">Identificador del canal de amigos que terminó.</param>
        /// <param name="sessionEnd">Lo que hay que avisar por la sesión cerrada; null si no se cerró ninguna.</param>
        /// <returns>true si el canal era el de amigos de una sesión y esta se cerró; si no, false.</returns>
        bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd);

        /// <summary>Crea una sala nueva y deja dentro al jugador de la sesión.</summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="channelId">Identificador del canal de lobby desde el que llama el jugador.</param>
        /// <param name="lobbyCallback">Callback del canal de lobby del jugador.</param>
        /// <returns>Applied con la sala, InvalidSession o AlreadyInRoom; nunca es null.</returns>
        LobbyMembershipChange CreateRoom(string sessionToken, string channelId, ILobbyManagerCallback lobbyCallback);

        /// <summary>
        /// Mete al jugador en una sala si tiene autorización de entrada y hay cupo; consume la autorización.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="roomId">Identificador de la sala a la que se quiere entrar.</param>
        /// <param name="channelId">Identificador del canal de lobby desde el que llama el jugador.</param>
        /// <param name="lobbyCallback">Callback del canal de lobby del jugador.</param>
        /// <returns>
        /// Applied con la sala y los demás jugadores, InvalidSession, AlreadyInRoom, NotAuthorized o RoomFull;
        /// NotAuthorized cubre tanto una sala inexistente como la falta de autorización. Nunca es null.
        /// </returns>
        LobbyMembershipChange EnterRoom(
            string sessionToken,
            int roomId,
            string channelId,
            ILobbyManagerCallback lobbyCallback);

        /// <summary>Saca al jugador de su sala; la elimina si queda vacía.</summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <returns>Applied, InvalidSession o NotInRoom; nunca es null.</returns>
        LobbyMembershipChange LeaveRoom(string sessionToken);

        /// <summary>Saca de su sala al jugador cuyo canal de lobby terminó; la elimina si queda vacía.</summary>
        /// <param name="channelId">Identificador del canal de lobby que terminó.</param>
        /// <returns>Applied o NotInRoom; nunca es null.</returns>
        LobbyMembershipChange LeaveRoomByChannel(string channelId);

        /// <summary>
        /// Registra el canal de amigos de la sesión, lo que deja al jugador en línea; si la sesión ya tenía uno,
        /// gana el último. Devuelve también a quién avisar que el jugador pasó a estar en línea.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="channelId">Identificador del canal de amigos desde el que llama el jugador.</param>
        /// <param name="friendsCallback">Callback del canal de amigos del jugador.</param>
        /// <param name="friendUserIds">Ids de los amigos aceptados del jugador, ya consultados en la base.</param>
        /// <returns>Applied con cuáles de esos amigos están en línea, o InvalidSession; nunca es null.</returns>
        FriendsSubscription SubscribeFriendsChannel(
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback,
            IReadOnlyList<int> friendUserIds);

        /// <summary>
        /// Crea la invitación del jugador de la sesión a su sala para un amigo en línea que no está en una sala. La
        /// amistad ya debe estar comprobada en la base de datos; una invitación repetida no crea otra.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="friendUserId">Id de la cuenta del amigo invitado.</param>
        /// <returns>
        /// Applied con la invitación y el callback del invitado, InvalidSession, NotInRoom, RoomFull o CannotInvite;
        /// CannotInvite cubre que el invitado no esté en línea o ya esté en una sala. Nunca es null.
        /// </returns>
        RoomInvitationCreation CreateRoomInvitation(string sessionToken, int friendUserId);

        /// <summary>
        /// Acepta una invitación del jugador de la sesión: la consume y emite la autorización de entrada a su sala.
        /// No mete al jugador en la sala.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="invitationId">Id de la invitación recibida.</param>
        /// <returns>
        /// Applied con el id de la sala, InvalidSession o InvitationNotValid; InvitationNotValid cubre que la
        /// invitación no exista, no sea del jugador o su sala ya no exista. Nunca es null.
        /// </returns>
        RoomInvitationAcceptance AcceptRoomInvitation(string sessionToken, int invitationId);
    }
}