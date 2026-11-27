using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Carries the session owner's profile and persisted personal statistics.
    /// </summary>
    [DataContract]
    public class OperationPlayerProfileResult : OperationResult
    {
        [DataMember]
        public string Nickname { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string AvatarKey { get; set; }

        [DataMember]
        public DateTime PasswordChangedAtUtc { get; set; }

        [DataMember]
        public int? TotalPoints { get; set; }

        [DataMember]
        public int? TotalWins { get; set; }

        [DataMember]
        public int? TotalGamesPlayed { get; set; }

        [DataMember]
        public int? PersonalRank { get; set; }
    }
}