namespace TLMG.Domain.Repositories
{
    public class VerificationCodeValidationResult
    {
        public string Status { get; set; }
        public int AttemptsFailed { get; set; }
    }
}