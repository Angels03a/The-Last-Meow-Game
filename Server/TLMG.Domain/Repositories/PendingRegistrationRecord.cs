using System;

namespace TLMG.Domain.Repositories
{
    public class PendingRegistrationRecord
    {
        public int PendingId { get; set; }
        public string Email { get; set; }
        public string Nickname { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}