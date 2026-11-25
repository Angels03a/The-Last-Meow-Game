namespace TLMG.Domain.Engine
{
    /// <summary>Result of calculating final standings. Other values indicate rejection.</summary>
    public enum FinalStandingsStatus
    {
        /// <summary>Standings are ready with a rank and points for each player.</summary>
        Calculated,

        /// <summary>The match is still running and has no final standings.</summary>
        MatchNotFinished
    }
}
