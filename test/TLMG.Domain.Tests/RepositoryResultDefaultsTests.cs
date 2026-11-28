using TLMG.Domain.Repositories;
using Xunit;

namespace TLMG.Domain.Tests
{
    public sealed class RepositoryResultDefaultsTests
    {
        [Fact]
        public void PasswordHashLookup_WhenCreated_ReportsDataFailure()
        {
            PasswordHashLookup result = new PasswordHashLookup();

            Assert.Equal(CredentialsLookupStatus.DataFailure, result.Status);
        }

        [Fact]
        public void FriendshipWriteResult_WhenCreated_ReportsDataFailure()
        {
            FriendshipWriteResult result = new FriendshipWriteResult();

            Assert.Equal(FriendshipWriteStatus.DataFailure, result.Status);
        }

        [Fact]
        public void FriendCandidateLookup_WhenCreated_ReportsDataFailure()
        {
            FriendCandidateLookup result = new FriendCandidateLookup();

            Assert.Equal(FriendLookupStatus.DataFailure, result.Status);
        }

        [Fact]
        public void FriendshipRecordLookup_WhenCreated_ReportsDataFailure()
        {
            FriendshipRecordLookup result = new FriendshipRecordLookup();

            Assert.Equal(FriendLookupStatus.DataFailure, result.Status);
        }
    }
}
