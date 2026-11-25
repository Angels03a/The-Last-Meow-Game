using System;

namespace TLMG.Client.Models
{
    /// <summary>Display data for a notification authorized by the server.</summary>
    public sealed class GameNotification
    {
        private const int DefaultDurationSeconds = 5;

        public string TitleKey { get; set; }
        public string BodyKey { get; set; }
        public string Subject { get; set; }
        public string Icon { get; set; }
        public bool IsPersonal { get; set; }
        public bool IsSuccess { get; set; }

        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(DefaultDurationSeconds);
    }
}
