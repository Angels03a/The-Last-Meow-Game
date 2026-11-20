namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable result of accepting a room invitation.
    /// </summary>
    public sealed class RoomInvitationAcceptance
    {
        private RoomInvitationAcceptance(FriendsChangeStatus status, int userId, int roomId)
        {
            Status = status;
            UserId = userId;
            RoomId = roomId;
        }

        /// <summary>Outcome of the acceptance attempt.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>Player's account ID, or NoUserId for an invalid token.</summary>
        public int UserId { get; }

        /// <summary>Authorized room ID, or NoRoomId on rejection.</summary>
        public int RoomId { get; }

        /// <summary>Creates a rejected result.</summary>
        /// <param name="status">Rejection reason.</param>
        /// <param name="userId">Account ID, or NoUserId for an invalid token.</param>
        /// <returns>Result without a room, always present.</returns>
        public static RoomInvitationAcceptance CreateRejected(FriendsChangeStatus status, int userId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                new RoomInvitationAcceptance(status, userId, LobbyMembershipChange.NoRoomId);

            return roomInvitationAcceptance;
        }

        /// <summary>Creates an applied result.</summary>
        /// <param name="userId">Player's account ID.</param>
        /// <param name="roomId">ID of the room the player may enter.</param>
        /// <returns>Applied result, always present.</returns>
        public static RoomInvitationAcceptance CreateApplied(int userId, int roomId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                new RoomInvitationAcceptance(FriendsChangeStatus.Applied, userId, roomId);

            return roomInvitationAcceptance;
        }
    }
}