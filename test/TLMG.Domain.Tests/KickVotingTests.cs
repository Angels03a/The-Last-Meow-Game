using TLMG.Domain.Moderation;
using Xunit;

namespace TLMG.Domain.Tests
{
    public class KickVotingTests
    {
        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        public void RegisterVote_WhenVoterIsTargetOrOutsider_PreservesVoteCount(int voterId)
        {
            KickVoting voting = new KickVoting(3, new int[] { 1, 2, 3 });

            Assert.Equal(KickVoteStatus.InvalidVoter, voting.RegisterVote(voterId));
            Assert.Equal(0, voting.VoteCount);
            Assert.False(voting.HasConsensus);
        }

        [Fact]
        public void RegisterVote_RequiresEveryOtherParticipantExactlyOnce()
        {
            KickVoting voting = new KickVoting(3, new int[] { 1, 2, 3 });

            Assert.True(voting.IsValid);
            Assert.Equal(2, voting.RequiredVoteCount);
            Assert.Equal(KickVoteStatus.VoteRecorded, voting.RegisterVote(2));
            Assert.Equal(KickVoteStatus.AlreadyVoted, voting.RegisterVote(2));
            Assert.False(voting.HasConsensus);
            Assert.Equal(KickVoteStatus.ConsensusReached, voting.RegisterVote(1));
            Assert.True(voting.HasConsensus);
            Assert.Equal(KickVoteStatus.VotingClosed, voting.RegisterVote(2));
            Assert.Equal(new int[] { 1, 2 }, voting.CopyVoterUserIds());
        }
    }
}
