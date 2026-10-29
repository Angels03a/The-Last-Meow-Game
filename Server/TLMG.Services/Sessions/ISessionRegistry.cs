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
        /// <returns>El token de la sesión nueva; nunca es null.</returns>
        string StartSession(int userId, string nickname);

        /// <summary>Busca la sesión de un token.</summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="playerSession">La sesión encontrada; null si no existe.</param>
        /// <returns>true si el token pertenece a una sesión vigente; en caso contrario, false.</returns>
        bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession);

        /// <summary>
        /// Cierra la sesión de un token; el jugador pierde también su sala, su callback y sus autorizaciones.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <returns>true si había una sesión vigente y se cerró; en caso contrario, false.</returns>
        bool EndSession(string sessionToken);

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

        /// <summary>Emite la autorización para que un jugador entre a una sala existente.</summary>
        /// <param name="userId">Identificador de la cuenta autorizada; debe tener sesión vigente.</param>
        /// <param name="roomId">Identificador de la sala a la que se autoriza.</param>
        /// <returns>true si quedó emitida; false si el jugador no tiene sesión o la sala no existe.</returns>
        bool IssueRoomEntryAuthorization(int userId, int roomId);
    }
}