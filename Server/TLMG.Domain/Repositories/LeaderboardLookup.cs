using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    public class LeaderboardLookup
    {
        public bool WasLeaderboardLookupSuccessful { get; set; }
        public List<RankedPlayer> Players { get; set; }
        public int? PersonalRank { get; set; }
        public int? PersonalPoints { get; set; }
    }
}
