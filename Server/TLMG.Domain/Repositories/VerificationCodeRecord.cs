using System;

namespace TLMG.Domain.Repositories
{
    public class VerificationCodeRecord
    {
        public int? UserId { get; set; }
        public int? PendingId { get; set; }
        public string CodeHash { get; set; }
        public VerificationCodeType Type { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}