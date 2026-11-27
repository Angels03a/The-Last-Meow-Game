namespace TLMG.Domain.Repositories
{
    public enum KickVotingWriteStatus
    {
        DataFailure,
        Recorded,
        AlreadyRecorded,
        VotingConflict,
        InvalidVoting,
        MatchNotFound,
        UserNotFound
    }
}