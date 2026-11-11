namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Acceso a la persistencia de las amistades entre cuentas; considera ambos sentidos de Friendship.
    /// </summary>
    public interface IFriendshipRepository
    {
        /// <summary>Obtiene los amigos aceptados de una cuenta, sin repetir al de fila en ambos sentidos.</summary>
        /// <param name="userId">Identificador de la cuenta (id de User).</param>
        /// <returns>Los amigos y su estado de consulta; nunca es null.</returns>
        FriendListLookup FindAcceptedFriends(int userId);

        /// <summary>Comprueba si dos cuentas son amigas aceptadas, sin importar quién envió la solicitud.</summary>
        /// <param name="userId">Identificador de la primera cuenta (id de User).</param>
        /// <param name="otherUserId">Identificador de la segunda cuenta (id de User).</param>
        /// <returns>
        /// Accepted si son amigas, NotAccepted si no lo son y DataFailure si la consulta falló; un fallo nunca
        /// debe tratarse como "no son amigos" ni como "son amigos".
        /// </returns>
        FriendshipLookupStatus FindFriendshipStatus(int userId, int otherUserId);

        /// <summary>Busca por nickname al jugador al que se quiere solicitar amistad.</summary>
        /// <param name="nickname">Nickname ya validado y recortado.</param>
        /// <returns>La cuenta encontrada, que no existe o el fallo de la base de datos.</returns>
        FriendCandidateLookup FindFriendCandidateByNickname(string nickname);

        /// <summary>Busca la fila que une a dos jugadores, sin importar quién envió la solicitud.</summary>
        /// <param name="userId">Id de uno de los jugadores.</param>
        /// <param name="otherUserId">Id del otro jugador.</param>
        /// <returns>La fila de la pareja, que no existe o el fallo de la base de datos.</returns>
        FriendshipRecordLookup FindFriendshipRecord(int userId, int otherUserId);

        /// <summary>Busca una fila de Friendship por su id, que es el id de la solicitud.</summary>
        /// <param name="friendshipId">Id de la solicitud.</param>
        /// <returns>La fila, que no existe o el fallo de la base de datos.</returns>
        FriendshipRecordLookup FindFriendshipRecordById(int friendshipId);

        /// <summary>Consulta las solicitudes pendientes que el jugador recibió o envió.</summary>
        /// <param name="userId">Id del jugador.</param>
        /// <returns>Las solicitudes pendientes o el fallo de la base de datos.</returns>
        PendingFriendRequestsLookup FindPendingFriendRequests(int userId);

        /// <summary>
        /// Inserta una solicitud pendiente. La unicidad por pareja de la base de datos evita dos filas si dos
        /// solicitudes llegan a la vez.
        /// </summary>
        /// <param name="senderUserId">Id de quien envía.</param>
        /// <param name="receiverUserId">Id de quien recibe.</param>
        /// <returns>Applied con el id creado, DuplicatePair si la pareja ya tenía fila o DataFailure.</returns>
        FriendshipWriteResult CreateFriendRequest(int senderUserId, int receiverUserId);

        /// <summary>
        /// Reactiva una solicitud rechazada como pendiente nueva con el emisor actual, sin período de espera.
        /// </summary>
        /// <param name="friendshipId">Id de la fila rechazada.</param>
        /// <param name="senderUserId">Id de quien envía ahora.</param>
        /// <param name="receiverUserId">Id de quien recibe ahora.</param>
        /// <returns>Applied, StateChanged si la fila ya no estaba rechazada, o DataFailure.</returns>
        FriendshipWriteStatus ReactivateFriendRequest(int friendshipId, int senderUserId, int receiverUserId);

        /// <summary>
        /// Acepta o rechaza una solicitud pendiente. Solo cambia la fila si sigue pendiente y es del receptor.
        /// </summary>
        /// <param name="friendshipId">Id de la solicitud.</param>
        /// <param name="receiverUserId">Id de quien responde, tomado de su sesión.</param>
        /// <param name="newState">Accepted o Rejected.</param>
        /// <returns>Applied, StateChanged si otro cambio ganó o no es del receptor, o DataFailure.</returns>
        FriendshipWriteStatus AnswerFriendRequest(int friendshipId, int receiverUserId, FriendshipState newState);
    }
}