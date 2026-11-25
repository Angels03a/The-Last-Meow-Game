
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

        /// <summary>Players ordered by leaderboard position. Empty if the query failed.</summary>
        public List<RankedPlayer> Players { get; set; }

        /// <summary>Personal leaderboard position, when available.</summary>
        public int? PersonalRank { get; set; }

        /// <summary>Personal accumulated points, when available.</summary>
        public int? PersonalPoints { get; set; }
    }
}
