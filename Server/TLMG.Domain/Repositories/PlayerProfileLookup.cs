using System;

namespace TLMG.Domain.Repositories
{
    public class PlayerProfileLookup
    {
        public PlayerProfileLookupStatus Status { get; set; }

        public string Nickname { get; set; }

        public string Email { get; set; }

        public string AvatarKey { get; set; }

        public DateTime PasswordChangedAtUtc { get; set; }
    }
}