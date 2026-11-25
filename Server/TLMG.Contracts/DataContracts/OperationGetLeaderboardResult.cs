using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    [DataContract]
    public class OperationGetLeaderboardResult : OperationResult
    {
        [DataMember]
        public List<LeaderboardPlayer> Players { get; set; }

        [DataMember]
        public int? PersonalRank { get; set; }

        [DataMember]
        public int? PersonalPoints { get; set; }
    }
}
