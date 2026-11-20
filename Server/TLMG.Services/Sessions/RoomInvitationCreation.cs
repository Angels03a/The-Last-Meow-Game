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
            bool isNewInvitation,
            IFriendsManagerCallback inviteeCallback)
        {
            Status = status;
            InvitationId = invitationId;
            InviterNickname = inviterNickname;
            IsNewInvitation = isNewInvitation;
            InviteeCallback = inviteeCallback;
        }

        /// <summary>Outcome of the invitation attempt.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>ID of the new or existing invitation, or NoInvitationId on rejection.</summary>
        public int InvitationId { get; }

        /// <summary>Inviting player's nickname, or null on rejection.</summary>
        public string InviterNickname { get; }

        /// <summary>Whether the invitation is new. False for an existing identical invitation.</summary>
        public bool IsNewInvitation { get; }

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
        /// <param name="isNewInvitation">True for a new invitation, false for an existing one.</param>
        /// <param name="inviteeCallback">Invited player's friends channel callback.</param>
        /// <returns>Applied result, always present.</returns>
        public static RoomInvitationCreation CreateApplied(
            int invitationId,
            string inviterNickname,
            bool isNewInvitation,
            IFriendsManagerCallback inviteeCallback)
        {
            RoomInvitationCreation roomInvitationCreation = new RoomInvitationCreation(
                FriendsChangeStatus.Applied,
                invitationId,
                inviterNickname,
                isNewInvitation,
                inviteeCallback);

            return roomInvitationCreation;
        }
    }
}