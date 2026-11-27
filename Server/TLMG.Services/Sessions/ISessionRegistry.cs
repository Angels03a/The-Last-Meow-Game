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
        /// Starts a registered session, replacing any previous session and revoking its room access, callbacks and entry permissions.
        /// </summary>
        /// <param name="userId">Account identifier.</param>
        /// <param name="nickname">Account nickname.</param>
        /// <param name="replacedSession">Notifications for the replaced session, or null if none existed.</param>
        /// <returns>The new session token, always present.</returns>
        string StartRegisteredSession(int userId, string nickname, out SessionEnd replacedSession);

        /// <summary>Finds a current session by its token.</summary>
        /// <param name="sessionToken">Client session token, which may be null or unknown.</param>
        /// <param name="playerSession">The session, or null if not found.</param>
        /// <returns>True if the token belongs to a current session.</returns>
        bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession);

        /// <summary>Ends a session and revokes its room access, callbacks and entry permissions.</summary>
        /// <param name="sessionToken">Client session token, which may be null or unknown.</param>
        /// <param name="sessionEnd">Notifications for the ended session, or null if none existed.</param>
        /// <returns>True if a current session was ended.</returns>
        bool EndSession(string sessionToken, out SessionEnd sessionEnd);

        /// <summary>Revokes the token only if the player has no active participation.</summary>
        bool TryLogOut(string sessionToken, out SessionEnd sessionEnd, out bool hasActiveParticipation);

        IReadOnlyList<Action> EvaluateConnectionDeadlines(bool force = true);

        ResultWithNotification<OperationRecoveryContextResult> GetRecoveryContext(string sessionToken);

        IReadOnlyList<Action> ProcessHeartbeat(string token, string channelId, HeartbeatChannel channel,
            int sequence, int confirmedSequence, Action<int, string> echo);

        /// <summary>Ends the session only if the closed or failed friends channel is still current.</summary>
        /// <param name="channelId">Closed or failed friends channel.</param>
        /// <param name="sessionEnd">Notifications for the ended session, or null if none was ended.</param>
        /// <returns>True if a session was ended.</returns>
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

        /// <summary>Registers the current friends channel and identifies recipients for online presence notifications.</summary>
        /// <param name="sessionToken">Client session token, which may be null or unknown.</param>
        /// <param name="channelId">Caller's friends channel identifier.</param>
        /// <param name="friendsCallback">Caller's friends channel callback.</param>
        /// <param name="friendUserIds">Accepted friend identifiers, including temporary friendships.</param>
        /// <returns>Applied with online friends, or InvalidSession. Always present.</returns>
        FriendsSubscription SubscribeFriendsChannel(string sessionToken, string channelId,
            IFriendsManagerCallback friendsCallback, IReadOnlyList<int> friendUserIds);

        /// <summary>
        /// Invites an online friend who is outside a room. The friendship must already be verified by the caller.
        /// A duplicate request reuses the existing invitation.
        /// </summary>
        /// <param name="sessionToken">Client's session token, which may be null or unknown.</param>
        /// <param name="friendUserId">Invited friend's user ID.</param>
        /// <returns>
        /// Applied with the invitation and recipient callback, InvalidSession, NotInRoom, RoomFull or CannotInvite.
        /// CannotInvite covers offline friends and friends already in a room. Always present.
        /// </returns>
        RoomInvitationCreation CreateRoomInvitation(string sessionToken, int friendUserId);

        /// <summary>Finds a friends channel callback for notification outside the lock.</summary>
        /// <param name="userId">Player identifier.</param>
        /// <returns>The callback, or null if the player is offline.</returns>
        IFriendsManagerCallback FindFriendsCallbackOfUser(int userId);

        /// <summary>Updates both online friends channels under the session lock without invoking callbacks.</summary>
        /// <param name="firstUserId">First player identifier.</param>
        /// <param name="secondUserId">Second player identifier.</param>
        /// <returns>Presence and callbacks for both players; offline callbacks are null.</returns>
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

        /// <summary>Subscribes a channel to the current room, replacing the previous chat subscription.</summary>
        /// <param name="caller">Registered account token and chat channel.</param>
        /// <param name="callback">Chat channel callback.</param>
        /// <returns>Null on success, otherwise the error key.</returns>
        string SubscribeToChat(SessionChannelCaller caller, IChatManagerCallback callback);

        /// <summary>Validates the caller, channel and room and reserves the sending interval atomically.</summary>
        /// <param name="caller">Current token and subscribed chat channel.</param>
        /// <param name="submission">Room, correlation identifier and validated text.</param>
        /// <param name="chatDelivery">Delivery snapshot, or null on rejection.</param>
        /// <returns>Null on acceptance, otherwise the error key.</returns>
        string AcceptChatMessage(SessionChannelCaller caller, ChatSubmission submission, out ChatDelivery chatDelivery);

        /// <summary>Removes only the matching chat channel while preserving the player's sending interval.</summary>
        /// <param name="channelId">Closed or failed channel.</param>
        void RemoveChatChannel(string channelId);

        /// <summary>
        /// Authorizes a subscription and returns the player's view under the same lock. The userId comes from the
        /// session and the player must still belong to the match's room.
        /// </summary>
        /// <param name="caller">Current session token and match channel.</param>
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
