namespace TLMG.Domain.Repositories
{
    /// <summary>Persists the evidence of completed unanimous expulsions.</summary>
    public interface IKickVotingRepository
    {
        /// <summary>Records all votes atomically; an identical retry must not duplicate votes.</summary>
        KickVotingWriteStatus RecordCompletedVoting(CompletedKickVoting voting);
    }
}