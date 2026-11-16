namespace TLMG.Domain.Repositories
{
    public class VerificationCodeValidationResult
    {
        public VerificationCodeStatus Status { get; set; }
        public int AttemptsFailed { get; set; }
    }
}