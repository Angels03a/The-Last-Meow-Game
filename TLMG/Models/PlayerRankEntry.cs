using System;

namespace TLMG.Models
{
    /// <summary>
    /// Representa los datos de un jugador para mostrarse en el leaderboard.
    /// </summary>
    public class PlayerRankEntry
    {
        private const double WinRateBarMaxWidth = 120;
        private const int HighWinRateThresholdPercentage = 80;

        public int Rank { get; set; }

        public string AvatarImagePath { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public int Score { get; set; }

        public int Wins { get; set; }

        public int WinRatePercentage { get; set; }

        public string RankLabel => $"#{Rank}";

        public string ScoreLabel => Score.ToString("N0");

        public string WinRateLabel => $"{WinRatePercentage}%";

        public double WinRateBarWidth => WinRatePercentage / 100.0 * WinRateBarMaxWidth;
        public bool IsHighWinRate => WinRatePercentage >= HighWinRateThresholdPercentage;
    }
}