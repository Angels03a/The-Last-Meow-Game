using System;

namespace TLMG.Client.Models
{
    /// <summary>
    /// Player details displayed on the leaderboard.
    /// </summary>
    public class PlayerRankEntry
    {
        private const double WinRateBarMaxWidthPixels = 120.0;
        private const int HighWinRateThresholdPercentage = 80;
        private const double PercentageDivisor = 100.0;

        /// <summary>
        /// Player's overall rank.
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Emoji used for the player's avatar.
        /// </summary>
        public string AvatarEmoji { get; set; } = string.Empty;

        /// <summary>
        /// Player's display name or nickname.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// Player's total accumulated points.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Player's total wins.
        /// </summary>
        public int Wins { get; set; }

        /// <summary>
        /// Percentage of matches the player won.
        /// </summary>
        public int WinRatePercentage { get; set; }

        /// <summary>
        /// Formatted rank, such as "#1".
        /// </summary>
        public string RankLabel => $"#{Rank}";

        /// <summary>
        /// Score formatted with a thousands separator.
        /// </summary>
        public string ScoreLabel => Score.ToString("N0");

        /// <summary>
        /// Formatted win rate, such as "80%".
        /// </summary>
        public string WinRateLabel => $"{WinRatePercentage}%";

        /// <summary>
        /// Width of the win-rate bar in pixels, proportional to the player's win percentage.
        /// </summary>
        public double WinRateBarWidth => WinRatePercentage / PercentageDivisor * WinRateBarMaxWidthPixels;

        /// <summary>
        /// Whether the win rate reaches the highlight threshold.
        /// </summary>
        public bool IsHighWinRate => WinRatePercentage >= HighWinRateThresholdPercentage;
    }
}