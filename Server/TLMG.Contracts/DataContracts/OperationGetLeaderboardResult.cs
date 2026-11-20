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
        /// <summary>Players ordered by points, highest first.</summary>
        [DataMember]
        public List<LeaderboardPlayer> Players { get; set; }
    }
}