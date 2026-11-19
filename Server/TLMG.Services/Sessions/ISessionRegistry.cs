using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

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
        string StartRegisteredSession(int userId, string nickname, out SessionEnd replacedSession);

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

        /// <summary>Revoca el token solo si el jugador no tiene participación activa.</summary>
        bool TryLogOut(string sessionToken, out SessionEnd sessionEnd, out bool hasActiveParticipation);

        IReadOnlyList<Action> EvaluateConnectionDeadlines(bool force = true);

        ResultWithNotification<OperationRecoveryContextResult> GetRecoveryContext(string sessionToken);

        IReadOnlyList<Action> ProcessHeartbeat(string token, string channelId, HeartbeatChannel channel,
            int sequence, int confirmedSequence, Action<int, string> echo);

        /// <summary>
        /// Termina la sesión del jugador cuyo canal de amigos se cerró o falló; si el canal ya no es el registrado
        /// (porque se reemplazó) no hace nada.
        /// </summary>
        /// <param name="channelId">Identificador del canal de amigos que terminó.</param>
        /// <param name="sessionEnd">Lo que hay que avisar por la sesión cerrada; null si no se cerró ninguna.</param>
        /// <returns>true si el canal era el de amigos de una sesión y esta se cerró; si no, false.</returns>
        bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd);

        /// <summary>Crea una sala nueva y deja dentro al jugador de la sesión.</summary>
        /// <param name="caller">Token de sesión y canal de lobby desde el que llama el jugador.</param>
        /// <param name="lobbyCallback">Callback del canal de lobby del jugador.</param>
        /// <returns>Applied con la sala, InvalidSession o AlreadyInRoom; nunca es null.</returns>
        LobbyMembershipChange CreateRoom(LobbyCaller caller, ILobbyManagerCallback lobbyCallback);

        /// <summary>
        /// Mete al jugador en una sala si tiene autorización de entrada y hay cupo; consume la autorización.
        /// </summary>
        /// <param name="caller">Token de sesión y canal de lobby desde el que llama el jugador.</param>
        /// <param name="roomId">Identificador de la sala a la que se quiere entrar.</param>
        /// <param name="lobbyCallback">Callback del canal de lobby del jugador.</param>
        /// <returns>
        /// Applied con la sala y los demás jugadores, InvalidSession, AlreadyInRoom, NotAuthorized o RoomFull;
        /// NotAuthorized cubre sala inexistente, falta de autorización o partida ya iniciada. Nunca es null.
        /// </returns>
        LobbyMembershipChange EnterRoom(LobbyCaller caller, int roomId, ILobbyManagerCallback lobbyCallback);

        /// <summary>
        /// Saca al jugador de su sala por su propia decisión (Salir); la elimina si queda vacía. Si la sala tenía una
        /// partida, también lo saca del motor de partida bajo el mismo candado.
        /// </summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="matchViewDeliveries">
        /// Vistas que hay que entregar a los demás suscritos de la partida; vacía si la partida no cambió.
        /// </param>
        /// <returns>Applied, InvalidSession o NotInRoom; nunca es null.</returns>
        LobbyMembershipChange LeaveRoom(string sessionToken, out IReadOnlyList<MatchViewDelivery> matchViewDeliveries);

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
        FriendsSubscription SubscribeFriendsChannel(string sessionToken, string channelId, 
            IFriendsManagerCallback friendsCallback, IReadOnlyList<int> friendUserIds);

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
        /// Busca el callback del canal de amigos de un jugador por su id, para avisarle fuera del candado.
        /// </summary>
        /// <param name="userId">Id de la cuenta del jugador.</param>
        /// <returns>El callback del canal de amigos, o null si el jugador está desconectado.</returns>
        IFriendsManagerCallback FindFriendsCallbackOfUser(int userId);

        /// <summary>
        /// Suma la amistad nueva a la lista de amigos guardada en el canal de cada jugador en línea, para que su
        /// presencia se avise sin volver a suscribirse. Bajo _sessionsLock; no invoca ningún callback.
        /// </summary>
        /// <param name="firstUserId">Id de uno de los dos jugadores.</param>
        /// <param name="secondUserId">Id del otro jugador.</param>
        /// <returns>La presencia y el callback de cada jugador; el callback es null si está desconectado.</returns>
        FriendsAddition AddFriendshipToOnlineChannels(int firstUserId, int secondUserId);

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
        /// <summary>Cambia el estado Listo del propio jugador. Valida token y canal antes de copiar el estado.</summary>
        /// <param name="caller">Token de sesión y canal de lobby vinculado al jugador.</param>
        /// <param name="roomId">Sala indicada por la petición.</param>
        /// <param name="isReady">Estado Listo deseado del propio jugador; repetirlo no renueva la cuenta.</param>
        /// <returns>Estado actual y destinatarios, o rechazo sin datos de la sala.</returns>
        LobbyMembershipChange ChangePlayerReadiness(LobbyCaller caller, int roomId, bool isReady);

        /// <summary>
        /// Cambia las cartas por jugador de la sala. Valida token y canal; un valor distinto se guarda y deja a todos
        /// los jugadores en No Listo bajo el mismo _sessionsLock, y el mismo valor no cambia nada.
        /// </summary>
        /// <param name="caller">Token de sesión y canal de lobby vinculado al jugador.</param>
        /// <param name="roomId">Sala indicada por la petición.</param>
        /// <param name="cardsPerPlayerCount">Cantidad ya validada por el servicio: 4, 6 u 8.</param>
        /// <returns>
        /// Estado actual y como destinatarios los demás jugadores, o rechazo sin datos de la sala. Con el mismo valor
        /// no hay destinatarios.
        /// </returns>
        LobbyMembershipChange ChangeCardsPerPlayer(LobbyCaller caller, int roomId, int cardsPerPlayerCount);

        /// <summary>Valida la generación y el plazo UTC para iniciar. Valida token y canal antes de copiar el estado.</summary>
        /// <param name="caller">Token de sesión y canal de lobby vinculado al jugador.</param>
        /// <param name="roomId">Sala indicada por la petición.</param>
        /// <param name="countdownId">Generación de cuenta observada; una generación vieja solo devuelve el estado.</param>
        /// <returns>Estado actual y destinatarios, o rechazo sin datos de la sala.</returns>
        LobbyMembershipChange RequestMatchStart(LobbyCaller caller, int roomId, long countdownId);

        /// <summary>Suscribe el canal a la sala actual; la última suscripción válida de la sesión gana.</summary>
        /// <param name="caller">Token de cuenta registrada y canal de chat.</param>
        /// <param name="callback">Callback del canal de chat.</param>
        /// <returns>null si se suscribió; clave de error si se rechazó.</returns>
        string SubscribeToChat(LobbyCaller caller, IChatManagerCallback callback);

        /// <summary>Valida identidad, canal y sala y reserva atómicamente el intervalo del envío.</summary>
        /// <param name="caller">Token vigente y canal suscrito del emisor.</param>
        /// <param name="submission">Sala, correlación y texto ya validado por ChatValidator.</param>
        /// <param name="chatDelivery">Copia efímera de identidad, mensaje y destinatarios; null si se rechaza.</param>
        /// <returns>null si se aceptó; clave de error si se rechazó.</returns>
        string AcceptChatMessage(LobbyCaller caller, ChatSubmission submission, out ChatDelivery chatDelivery);

        /// <summary>Retira solo el canal que aún coincide; no borra el intervalo de envíos del jugador.</summary>
        /// <param name="channelId">Canal que se cerró o falló.</param>
        void RemoveChatChannel(string channelId);

        /// <summary>
        /// Autoriza la suscripción a una partida y devuelve la vista del jugador; todo bajo el mismo candado. El
        /// userId sale de la sesión y la participación debe seguir vigente (sigue en la sala de esa partida).
        /// </summary>
        /// <param name="caller">Token de cuenta registrada y canal de partida.</param>
        /// <param name="matchId">Partida solicitada.</param>
        /// <param name="callback">Callback del canal de partida; sustituye al anterior del jugador.</param>
        /// <returns>La vista del jugador si se aceptó; la clave de error si se rechazó.</returns>
        MatchDelivery SubscribeToMatch(LobbyCaller caller, Guid matchId, IMatchManagerCallback callback);

        /// <summary>
        /// Autoriza y coloca una carta en la partida a la que está suscrito el canal. Si el motor la rechaza no
        /// cambia nada; si la acepta sube la versión y devuelve una vista por cada suscrito.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="card">Tipo de carta que se quiere colocar.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        MatchDelivery PlaceCard(LobbyCaller caller, MatchCardKind card);

        MatchDelivery TryCatchFugitiveKitten(LobbyCaller caller);

        MatchDelivery CheckFugitiveKittenDeadline(LobbyCaller caller);

        /// <summary>
        /// Autoriza y abre o sube una apuesta en la partida a la que está suscrito el canal; la fase decide cuál de las
        /// dos es. Si el motor la rechaza no cambia nada; si la acepta sube la versión y devuelve una vista por cada
        /// suscrito.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="betValue">Cartas que el jugador declara poder voltear.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        MatchDelivery PlaceBet(LobbyCaller caller, int betValue);

        /// <summary>
        /// Autoriza y retira al jugador de la apuesta en curso. Con un solo apostador restante el reto queda listo y su
        /// desenlace empieza a registrarse.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        MatchDelivery WithdrawFromBet(LobbyCaller caller);

        /// <summary>
        /// Autoriza y voltea la carta de arriba de la pila elegida durante el reto del jugador. El desenlace del reto
        /// (cartas volteadas, éxito o fallo y castigo) se anota en el registro de la partida.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="pileOwnerUserId">Cuenta del dueño de la pila de la que se voltea.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        MatchDelivery RevealCard(LobbyCaller caller, int pileOwnerUserId);

        /// <summary>
        /// Autoriza y elimina la carta elegida del castigo. En el castigo a ciegas traduce el lugar mostrado al índice
        /// real con la permutación del servidor; el cliente nunca conoce esa correspondencia.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="cardIndex">Lugar mostrado (a ciegas) o posición en la mano (castigo propio).</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        MatchDelivery ChoosePunishmentCard(LobbyCaller caller, int cardIndex);

        /// <summary>
        /// Quita solo el callback de partida del canal que se cerró o falló, si sigue siendo el registrado. No
        /// modifica la partida ni la sala.
        /// </summary>
        /// <param name="channelId">Canal que se cerró o falló.</param>
        /// <param name="closedCallback">Callback que tenía ese canal; se compara por referencia.</param>
        /// <returns>true si se quitó la suscripción; en caso contrario, false.</returns>
        bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback,
            out IReadOnlyList<MatchViewDelivery> deliveries);
    }
}