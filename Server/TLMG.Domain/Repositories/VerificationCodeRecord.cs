using System;

namespace TLMG.Domain.Repositories
{
    public class VerificationCodeRecord
    {
        public int CodeId { get; set; }
        public int? UserId { get; set; }
        public int? PendingId { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public string CodeHash { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int AttemptsFailed { get; set; }
    }
}