using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Player details shown on the leaderboard.
    /// </summary>
    [DataContract]
    public class LeaderboardPlayer
    {
        /// <summary>Player's nickname.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Player's profile image, which may be null.</summary>
        [DataMember]
        public string ProfileImage { get; set; }

        /// <summary>Total accumulated points.</summary>
        [DataMember]
        public int TotalPoints { get; set; }

        /// <summary>Matches won.</summary>
        [DataMember]
        public int TotalWins { get; set; }

        /// <summary>Matches played.</summary>
        [DataMember]
        public int TotalGamesPlayed { get; set; }
    }
}