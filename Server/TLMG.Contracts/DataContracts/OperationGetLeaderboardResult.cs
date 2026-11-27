using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Leaderboard query result with ranked players or an error key.
    /// </summary>
    [DataContract]
    public class OperationGetLeaderboardResult : OperationResult
    {
        /// <summary>Players ordered by leaderboard position.</summary>
        [DataMember]
        public List<LeaderboardPlayer> Players { get; set; }

        /// <summary>Personal leaderboard position, when available.</summary>
        [DataMember]
        public int? PersonalRank { get; set; }

        /// <summary>Personal accumulated points, when available.</summary>
        [DataMember]
        public int? PersonalPoints { get; set; }
    }
}
