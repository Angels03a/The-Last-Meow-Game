using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Leaderboard query result with players or a data access failure.
    /// </summary>
    public class LeaderboardLookup
    {
        /// <summary>Whether the query succeeded. False on a data access failure.</summary>
        public bool WasLeaderboardLookupSuccessful { get; set; }

        /// <summary>Players ordered by points, highest first. Empty if the query failed.</summary>
        public List<RankedPlayer> Players { get; set; }
    }
}