namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Player details needed by the leaderboard, without exposing the Entity Framework entity.
    /// </summary>
    public class RankedPlayer
    {
        /// <summary>Player's nickname.</summary>
        public string Nickname { get; set; }

        /// <summary>Player's profile image, which may be null.</summary>
        public string ProfileImage { get; set; }

        /// <summary>Total accumulated points.</summary>
        public int TotalPoints { get; set; }

        /// <summary>Matches won.</summary>
        public int TotalWins { get; set; }

        /// <summary>Matches played.</summary>
        public int TotalGamesPlayed { get; set; }
    }
}