using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable result of inviting a friend to a room, with the outcome and recipients to notify outside the lock.
    /// </summary>
    public sealed class RoomInvitationCreation
    {
        /// <summary>InvitationId when the result refers to no invitation.</summary>
        public const int NoInvitationId = 0;

        private RoomInvitationCreation(
            FriendsChangeStatus status,
            int invitationId,
            string inviterNickname,
            bool shouldNotifyInvitee,
            IFriendsManagerCallback inviteeCallback)
        {
            Status = status;
            InvitationId = invitationId;
            InviterNickname = inviterNickname;
            ShouldNotifyInvitee = shouldNotifyInvitee;
            InviteeCallback = inviteeCallback;
        }

        /// <summary>Outcome of the invitation attempt.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>ID of the new or existing invitation, or NoInvitationId on rejection.</summary>
        public int InvitationId { get; }

        /// <summary>Inviting player's nickname, or null on rejection.</summary>
        public string InviterNickname { get; }

        /// <summary>
        /// Whether the invitee must be notified: a new invitation, or an existing one not notified within the
        /// renotification interval.
        /// </summary>
        public bool ShouldNotifyInvitee { get; }

        /// <summary>Recipient's friends channel callback, or null on rejection.</summary>
        public IFriendsManagerCallback InviteeCallback { get; }

        /// <summary>Creates a rejected result.</summary>
        /// <param name="status">Rejection reason.</param>
        /// <returns>Result without an invitation, always present.</returns>
        public static RoomInvitationCreation CreateRejected(FriendsChangeStatus status)
        {
            RoomInvitationCreation roomInvitationCreation =
                new RoomInvitationCreation(status, NoInvitationId, null, false, null);

            return roomInvitationCreation;
        }

        /// <summary>Creates an applied result.</summary>
        /// <param name="invitationId">Invitation ID.</param>
        /// <param name="inviterNickname">Inviting player's nickname.</param>
        /// <param name="shouldNotifyInvitee">True if the invitee must be notified now.</param>
        /// <param name="inviteeCallback">Invited player's friends channel callback.</param>
        /// <returns>Applied result, always present.</returns>
        public static RoomInvitationCreation CreateApplied(
            int invitationId,
            string inviterNickname,
            bool shouldNotifyInvitee,
            IFriendsManagerCallback inviteeCallback)
        {
            RoomInvitationCreation roomInvitationCreation = new RoomInvitationCreation(
                FriendsChangeStatus.Applied,
                invitationId,
                inviterNickname,
                shouldNotifyInvitee,
                inviteeCallback);

            return roomInvitationCreation;
        }
    }
}