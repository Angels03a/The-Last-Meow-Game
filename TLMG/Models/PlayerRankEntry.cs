using System;

namespace TLMG.Models
{
    /// <summary>
    /// Representa los datos de un jugador para mostrarse en el leaderboard.
    /// </summary>
    public class PlayerRankEntry
    {
        private const double WinRateBarMaxWidthPixels = 120.0;
        private const int HighWinRateThresholdPercentage = 80;
        private const double PercentageDivisor = 100.0;

        public int Rank { get; set; }

        public string AvatarImagePath { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public int Score { get; set; }

        public int Wins { get; set; }

        public int WinRatePercentage { get; set; }

        public string RankLabel => $"#{Rank}";

        public string ScoreLabel => Score.ToString("N0");

        public string WinRateLabel => $"{WinRatePercentage}%";

        public double WinRateBarWidth => WinRatePercentage / PercentageDivisor * WinRateBarMaxWidthPixels;
        public bool IsHighWinRate => WinRatePercentage >= HighWinRateThresholdPercentage;
    }
}