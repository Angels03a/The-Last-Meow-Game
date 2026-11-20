namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable pending invitation to another player's room.
    /// </summary>
    internal sealed class RoomInvitationRecord
    {
        /// <summary>Creates a pending invitation.</summary>
        /// <param name="invitationId">Invitation ID.</param>
        /// <param name="inviterUserId">Inviting account ID.</param>
        /// <param name="inviteeUserId">Invited account ID.</param>
        /// <param name="roomId">ID of the room to join.</param>
        public RoomInvitationRecord(int invitationId, int inviterUserId, int inviteeUserId, int roomId)
        {
            InvitationId = invitationId;
            InviterUserId = inviterUserId;
            InviteeUserId = inviteeUserId;
            RoomId = roomId;
        }

        /// <summary>Invitation ID.</summary>
        public int InvitationId { get; }

        /// <summary>Inviting account ID.</summary>
        public int InviterUserId { get; }

        /// <summary>Invited account ID.</summary>
        public int InviteeUserId { get; }

        /// <summary>ID of the room to join.</summary>
        public int RoomId { get; }
    }
}