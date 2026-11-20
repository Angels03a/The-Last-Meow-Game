using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable room change recording what happened, who was affected and who to notify after releasing the lock.
    /// </summary>
    public sealed class LobbyMembershipChange
    {
        /// <summary>RoomId when the result refers to no room.</summary>
        public const int NoRoomId = 0;

        /// <summary>UserId when the token belongs to no session.</summary>
        public const int NoUserId = 0;

        private static readonly IReadOnlyList<ILobbyManagerCallback> _noCallbacks =
            new List<ILobbyManagerCallback>().AsReadOnly();

        private LobbyMembershipChange(
            LobbyChangeStatus status,
            int userId,
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks,
            RoomExitCause exitCause,
            bool leftAuthorizedMatch)
        {
            Status = status;
            UserId = userId;
            RoomId = roomId;
            Room = room;
            RecipientCallbacks = recipientCallbacks;
            ExitCause = exitCause;
            LeftAuthorizedMatch = leftAuthorizedMatch;
        }

        /// <summary>Outcome of the requested change.</summary>
        public LobbyChangeStatus Status { get; }

        /// <summary>Player's account ID, or NoUserId for an invalid token.</summary>
        public int UserId { get; }

        /// <summary>Affected room ID, or NoRoomId if none.</summary>
        public int RoomId { get; }

        /// <summary>Room after the change, or null on rejection or when the empty room was deleted.</summary>
        public LobbyRoomSnapshot Room { get; }

        /// <summary>Callbacks to notify, always present.</summary>
        public IReadOnlyList<ILobbyManagerCallback> RecipientCallbacks { get; }

        /// <summary>Exit cause, or None for a change other than an exit.</summary>
        public RoomExitCause ExitCause { get; }

        /// <summary>Whether the player left a room with an authorized match.</summary>
        public bool LeftAuthorizedMatch { get; }

        /// <summary>Creates a rejected change result.</summary>
        /// <param name="status">Rejection reason.</param>
        /// <param name="userId">Account ID, or NoUserId for an invalid token.</param>
        /// <returns>Result without a room or recipients, always present.</returns>
        public static LobbyMembershipChange CreateRejected(LobbyChangeStatus status, int userId)
        {
            LobbyMembershipChange lobbyMembershipChange =
                new LobbyMembershipChange(
                    status, userId, NoRoomId, null, _noCallbacks, RoomExitCause.None, false);

            return lobbyMembershipChange;
        }

        /// <summary>Creates an applied result with nobody to notify.</summary>
        /// <param name="userId">Player's account ID.</param>
        /// <param name="roomId">Affected room ID.</param>
        /// <param name="room">Room after the change, or null if deleted.</param>
        /// <returns>Applied result, always present.</returns>
        public static LobbyMembershipChange CreateApplied(int userId, int roomId, LobbyRoomSnapshot room)
        {
            LobbyMembershipChange lobbyMembershipChange = CreateApplied(userId, roomId, room, _noCallbacks);

            return lobbyMembershipChange;
        }

        /// <summary>Creates the result for an applied change.</summary>
        /// <param name="userId">Player's account ID.</param>
        /// <param name="roomId">Affected room ID.</param>
        /// <param name="room">Room after the change, or null if deleted.</param>
        /// <param name="recipientCallbacks">
        /// Callbacks to notify, including the requester when appropriate. Leave the list unchanged afterward.
        /// </param>
        /// <returns>Applied result, always present.</returns>
        public static LobbyMembershipChange CreateApplied(
            int userId,
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks)
        {
            LobbyMembershipChange lobbyMembershipChange =
                new LobbyMembershipChange(
                    LobbyChangeStatus.Applied, userId, roomId, room, recipientCallbacks, RoomExitCause.None, false);

            return lobbyMembershipChange;
        }

        /// <summary>Creates a room exit change with its cause.</summary>
        /// <param name="userId">Player who left.</param>
        /// <param name="roomId">Room the player left.</param>
        /// <param name="room">Remaining room, or null if empty.</param>
        /// <param name="recipientCallbacks">Lobby callbacks for players still in the room.</param>
        /// <param name="exitCause">Exit cause.</param>
        /// <param name="leftAuthorizedMatch">Whether the room already had an authorized match.</param>
        /// <returns>Applied change.</returns>
        public static LobbyMembershipChange CreateDeparture(
            int userId,
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks,
            RoomExitCause exitCause,
            bool leftAuthorizedMatch)
        {
            LobbyMembershipChange lobbyMembershipChange = new LobbyMembershipChange(
                LobbyChangeStatus.Applied, userId, roomId, room, recipientCallbacks, exitCause, leftAuthorizedMatch);

            return lobbyMembershipChange;
        }
    }
}