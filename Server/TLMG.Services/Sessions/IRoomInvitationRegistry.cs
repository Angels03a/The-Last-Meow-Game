namespace TLMG.Services.Sessions
{
    /// <summary>Creates and accepts room invitations.</summary>
    public interface IRoomInvitationRegistry
    {
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
    }
}
