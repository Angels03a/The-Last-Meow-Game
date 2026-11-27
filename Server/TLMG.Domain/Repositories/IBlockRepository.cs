namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Provides persistent directional blocks without changing existing friendships.
    /// </summary>
    public interface IBlockRepository
    {
        /// <summary>
        /// Prevents future restricted interactions from the blocked account.
        /// Repeated blocks do not create additional records.
        /// </summary>
        BlockWriteStatus BlockPlayer(int blockerUserId, int blockedUserId);

        /// <summary>
        /// Removes only the block owned by the specified account.
        /// A block in the opposite direction remains effective.
        /// </summary>
        BlockWriteStatus UnblockPlayer(int blockerUserId, int blockedUserId);

        /// <summary>
        /// Retrieves blocks in both directions so room admission and directional restrictions
        /// can be evaluated. A failed lookup must not be treated as an empty block list.
        /// </summary>
        BlockListLookup FindBlocksInvolvingUser(int userId);
    }
}
