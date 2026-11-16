namespace TLMG.Domain.Repositories
{
    public enum VerificationCodeStatus
    {
        UnexpectedError,
        Valid,
        Invalid,
        TooManyAttempts,
        Expired,
        NoActiveCode
    }
}