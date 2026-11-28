namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Provides account friendship persistence independently of request direction.
    /// </summary>
    public interface IFriendshipRepository
    {
        /// <summary>Retrieves accepted friends without duplicates and reports lookup failure separately.</summary>
        /// <returns>A non-null lookup and list. Check the success flag even when the list is empty.</returns>
        FriendListLookup FindAcceptedFriends(int userId);

        /// <summary>
        /// Checks accepted friendship in either direction. DataFailure must not be interpreted
        /// as either an accepted or a missing friendship.
        /// </summary>
        FriendshipLookupStatus FindFriendshipStatus(int userId, int otherUserId);

        /// <summary>Resolves a friendship candidate by a validated and trimmed nickname.</summary>
        FriendCandidateLookup FindFriendCandidateByNickname(string nickname);

        /// <summary>Retrieves a friendship record regardless of its request direction.</summary>
        FriendshipRecordLookup FindFriendshipRecord(int userId, int otherUserId);

        /// <summary>Retrieves the friendship record associated with a request identifier.</summary>
        FriendshipRecordLookup FindFriendshipRecordById(int friendshipId);

        /// <summary>Retrieves both incoming and outgoing pending requests.</summary>
        /// <returns>A non-null lookup and list. Check the success flag even when the list is empty.</returns>
        PendingFriendRequestsLookup FindPendingFriendRequests(int userId);

        /// <summary>
        /// Creates a pending request. Applied includes the new request identifier; DuplicatePair
        /// indicates that the pair already has a record.
        /// </summary>
        FriendshipWriteResult CreateFriendRequest(int senderUserId, int receiverUserId);

        /// <summary>
        /// Reopens a rejected request with the current sender and receiver without a waiting period.
        /// StateChanged indicates that the record is no longer rejected.
        /// </summary>
        FriendshipWriteStatus ReactivateFriendRequest(int friendshipId, int senderUserId, int receiverUserId);

        /// <summary>
        /// Accepts or rejects a request only while it is pending and belongs to the authenticated receiver.
        /// StateChanged indicates that these conditions no longer hold.
        /// </summary>
        FriendshipWriteStatus AnswerFriendRequest(int friendshipId, int receiverUserId, FriendshipState newState);
    }
}