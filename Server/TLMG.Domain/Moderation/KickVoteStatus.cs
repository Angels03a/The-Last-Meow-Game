namespace TLMG.Domain.Moderation
{
    public enum KickVoteStatus
    {
        InvalidVoting,
        InvalidVoter,
        AlreadyVoted,
        VoteRecorded,
        ConsensusReached,
        VotingClosed,
        InvalidSession,
        NotInRoom,
        TargetUnavailable,
        DataFailure
    }
}
