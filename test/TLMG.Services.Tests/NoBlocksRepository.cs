using TLMG.Domain.Repositories;

namespace TLMG.Services.Tests
{
    /// <summary>Read-only snapshot for friendship scenarios without blocked players.</summary>
    internal class NoBlocksRepository : IBlockRepository
    {
        public virtual BlockListLookup FindBlocksInvolvingUser(int userId)
        {
            return new BlockListLookup { WasLookupSuccessful = userId > 0 };
        }

        public BlockWriteStatus BlockPlayer(int blockerUserId, int blockedUserId)
        {
            return BlockWriteStatus.DataFailure;
        }

        public BlockWriteStatus UnblockPlayer(int blockerUserId, int blockedUserId)
        {
            return BlockWriteStatus.DataFailure;
        }
    }
}
