using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// In-memory session and room registry shared by all WCF sessions.
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

        /// <summary>Revokes the token only if the player has no active participation.</summary>
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

        /// <summary>Creates a room and places the session's player in it.</summary>
        /// <param name="caller">Session token and lobby channel used by the caller.</param>
        /// <param name="lobbyCallback">Player's lobby channel callback.</param>
        /// <returns>Applied with the room, InvalidSession or AlreadyInRoom. Always present.</returns>
        LobbyMembershipChange CreateRoom(SessionChannelCaller caller, ILobbyManagerCallback lobbyCallback);

        /// <summary>
        /// Joins the player to a room if they have entry permission and space is available. Uses up the permission.
        /// </summary>
        /// <param name="caller">Session token and lobby channel used by the caller.</param>
        /// <param name="roomId">ID of the room to join.</param>
        /// <param name="lobbyCallback">Player's lobby channel callback.</param>
        /// <returns>
        /// Applied with the room and other players, InvalidSession, AlreadyInRoom, NotAuthorized or RoomFull.
        /// NotAuthorized covers missing rooms, missing permission and matches already started. Always present.
        /// </returns>
        LobbyMembershipChange EnterRoom(SessionChannelCaller caller, int roomId, ILobbyManagerCallback lobbyCallback);

        /// <summary>
        /// Handles a voluntary exit and deletes the room if empty. If it had a match, removes the player from the
        /// engine under the same lock.
        /// </summary>
        /// <param name="sessionToken">Client's session token, which may be null or unknown.</param>
        /// <param name="matchViewDeliveries">
        /// Views for other match subscribers, empty if the match did not change.
        /// </param>
        /// <returns>Applied, InvalidSession or NotInRoom. Always present.</returns>
        LobbyMembershipChange LeaveRoom(string sessionToken, out IReadOnlyList<MatchViewDelivery> matchViewDeliveries);

        /// <summary>
        /// Detaches a closed lobby channel after match authorization so the player keeps their seat. Before
        /// authorization, removes the player and deletes the room if empty.
        /// </summary>
        /// <param name="channelId">ID of the lobby channel that ended.</param>
        /// <returns>Applied or NotInRoom. Always present.</returns>
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
        /// Invites an online friend who is outside a room to the session player's room. The database must already have
        /// confirmed the friendship. A duplicate request reuses the existing invitation.
        /// </summary>
        /// <param name="sessionToken">Client's session token, which may be null or unknown.</param>
        /// <param name="friendUserId">Invited friend's account ID.</param>
        /// <returns>
        /// Applied with the invitation and recipient callback, InvalidSession, NotInRoom, RoomFull or CannotInvite.
        /// CannotInvite covers offline friends and friends already in a room. Always present.
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
        /// Consumes an accepted invitation and grants entry permission. The player must still join the room.
        /// </summary>
        /// <param name="sessionToken">Client's session token, which may be null or unknown.</param>
        /// <param name="invitationId">Received invitation ID.</param>
        /// <returns>
        /// Applied with the room ID, InvalidSession or InvitationNotValid. The invitation may be missing, belong to
        /// another player or refer to a deleted room. Always present.
        /// </returns>
        
        RoomInvitationAcceptance AcceptRoomInvitation(string sessionToken, int invitationId);
        /// <summary>Changes the caller's own ready state. Checks the token and channel before copying state.</summary>
        /// <param name="caller">Session token and lobby channel linked to the player.</param>
        /// <param name="roomId">Room named in the request.</param>
        /// <param name="isReady">Player's requested ready state. Repeating it keeps the existing countdown.</param>
        /// <returns>Current state and recipients, or a rejection without room data.</returns>
        LobbyMembershipChange ChangePlayerReadiness(SessionChannelCaller caller, int roomId, bool isReady);

        /// <summary>
        /// Changes cards per player after checking the token and channel. A new value resets everyone to not ready
        /// under the same _sessionsLock. Repeating the value changes nothing.
        /// </summary>
        /// <param name="caller">Session token and lobby channel linked to the player.</param>
        /// <param name="roomId">Room named in the request.</param>
        /// <param name="cardsPerPlayerCount">Count checked by the service (4, 6 or 8).</param>
        /// <returns>
        /// Current state and other players to notify, or a rejection without room data. An unchanged value has no
        /// recipients.
        /// </returns>
        LobbyMembershipChange ChangeCardsPerPlayer(SessionChannelCaller caller, int roomId, int cardsPerPlayerCount);

        /// <summary>
        /// Checks the countdown generation and UTC deadline before starting. Checks token and channel before copying
        /// state.
        /// </summary>
        /// <param name="caller">Session token and lobby channel linked to the player.</param>
        /// <param name="roomId">Room named in the request.</param>
        /// <param name="countdownId">Observed countdown generation. An old generation only returns the state.</param>
        /// <returns>Current state and recipients, or a rejection without room data.</returns>
        LobbyMembershipChange RequestMatchStart(SessionChannelCaller caller, int roomId, long countdownId);

        /// <summary>Suscribe el canal a la sala actual; la última suscripción válida de la sesión gana.</summary>
        /// <param name="caller">Token de cuenta registrada y canal de chat.</param>
        /// <param name="callback">Callback del canal de chat.</param>
        /// <returns>null si se suscribió; clave de error si se rechazó.</returns>
        string SubscribeToChat(SessionChannelCaller caller, IChatManagerCallback callback);

        /// <summary>Valida identidad, canal y sala y reserva atómicamente el intervalo del envío.</summary>
        /// <param name="caller">Token vigente y canal suscrito del emisor.</param>
        /// <param name="submission">Sala, correlación y texto ya validado por ChatValidator.</param>
        /// <param name="chatDelivery">Copia efímera de identidad, mensaje y destinatarios; null si se rechaza.</param>
        /// <returns>null si se aceptó; clave de error si se rechazó.</returns>
        string AcceptChatMessage(SessionChannelCaller caller, ChatSubmission submission, out ChatDelivery chatDelivery);

        /// <summary>Retira solo el canal que aún coincide; no borra el intervalo de envíos del jugador.</summary>
        /// <param name="channelId">Closed or failed channel.</param>
        void RemoveChatChannel(string channelId);

        /// <summary>
        /// Authorizes a subscription and returns the player's view under the same lock. The userId comes from the
        /// session and the player must still belong to the match's room.
        /// </summary>
        /// <param name="caller">Registered account token and match channel.</param>
        /// <param name="matchId">Requested match.</param>
        /// <param name="callback">Match channel callback replacing the player's previous one.</param>
        /// <returns>Player's view on acceptance, otherwise the error key.</returns>
        MatchDelivery SubscribeToMatch(SessionChannelCaller caller, Guid matchId, IMatchManagerCallback callback);

        /// <summary>
        /// Authorizes card placement in the channel's subscribed match. Rejection leaves state unchanged. Acceptance
        /// increases the version and returns one view per subscriber.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="card">Type of card to place.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery PlaceCard(SessionChannelCaller caller, MatchCardKind card);

        MatchDelivery TryCatchFugitiveKitten(SessionChannelCaller caller);

        MatchDelivery CheckFugitiveKittenDeadline(SessionChannelCaller caller);

        /// <summary>
        /// Authorizes a bid in the subscribed match. The phase determines whether to open or raise. Rejection leaves
        /// state unchanged. Acceptance increases the version and returns one view per subscriber.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="betValue">Number of cards the player claims they can reveal.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery PlaceBet(SessionChannelCaller caller, int betValue);

        /// <summary>
        /// Authorizes folding. When only one bidder remains, the challenge is ready and the registry starts recording
        /// its outcome.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery WithdrawFromBet(SessionChannelCaller caller);

        /// <summary>
        /// Authorizes revealing the top card of the chosen pile. Records revealed cards, success or failure and
        /// punishment in the match record.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="pileOwnerUserId">Account of the player whose pile supplies the card.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery RevealCard(SessionChannelCaller caller, int pileOwnerUserId);

        /// <summary>
        /// Authorizes removal of the chosen punishment card. For a blind choice, the server maps the displayed slot to
        /// the actual index and keeps that mapping private.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="cardIndex">Displayed slot for a blind choice or hand position for a visible choice.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery ChoosePunishmentCard(SessionChannelCaller caller, int cardIndex);

        /// <summary>
        /// Removes the callback only if the closed or failed channel is still current. A lost match connection can
        /// start a reconnection window.
        /// </summary>
        /// <param name="channelId">Closed or failed channel.</param>
        /// <param name="closedCallback">Original channel callback, compared by reference.</param>
        /// <returns>True if the subscription was removed.</returns>
        bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback,
            out IReadOnlyList<MatchViewDelivery> deliveries);
    }
}