using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Maintains room membership and readiness.</summary>
    public interface ILobbyRegistry
    {
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

        /// <summary>Changes the caller's own ready state. Checks the token and channel before copying state.</summary>
        /// <param name="caller">Session token and lobby channel linked to the player.</param>
        /// <param name="roomId">Room named in the request.</param>
        /// <param name="isReady">Player's requested ready state. Repeating it keeps the existing countdown.</param>
        /// <returns>Current state and recipients, or a rejection without room data.</returns>
        LobbyMembershipChange ChangePlayerReadiness(SessionChannelCaller caller, int roomId, bool isReady);
    }
}
