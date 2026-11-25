using System;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Pending invitation to another player's room. Only the time of its last notification changes.
    /// </summary>
    internal sealed class RoomInvitationRecord
    {
        /// <summary>Creates a pending invitation.</summary>
        /// <param name="invitationId">Invitation ID.</param>
        /// <param name="inviterUserId">Inviting account ID.</param>
        /// <param name="inviteeUserId">Invited account ID.</param>
        /// <param name="roomId">ID of the room to join.</param>
        /// <param name="createdUtc">UTC time of creation, which is also the first notification.</param>
        public RoomInvitationRecord(
            int invitationId,
            int inviterUserId,
            int inviteeUserId,
            int roomId,
            DateTime createdUtc)
        {
            InvitationId = invitationId;
            InviterUserId = inviterUserId;
            InviteeUserId = inviteeUserId;
            RoomId = roomId;
            LastNotifiedUtc = createdUtc;
        }

        /// <summary>Invitation ID.</summary>
        public int InvitationId { get; }

        /// <summary>Inviting account ID.</summary>
        public int InviterUserId { get; }

        /// <summary>Invited account ID.</summary>
        public int InviteeUserId { get; }

        /// <summary>ID of the room to join.</summary>
        public int RoomId { get; }

        /// <summary>UTC time when the invitee was last notified of this invitation.</summary>
        public DateTime LastNotifiedUtc { get; set; }
    }
}