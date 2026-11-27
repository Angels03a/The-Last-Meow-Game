using System;

namespace TLMG.Domain.Repositories
{
    public class PlayerProfileLookup
    {
        public PlayerProfileLookupStatus Status { get; set; } = PlayerProfileLookupStatus.DataFailure;

        public string Nickname { get; set; }

        public string Email { get; set; }

        public string AvatarKey { get; set; }

        public DateTime PasswordChangedAtUtc { get; set; }

        public int TotalPoints { get; set; }

        public int TotalWins { get; set; }

        public int TotalGamesPlayed { get; set; }

        public int PersonalRank { get; set; }
    }
}