namespace TLMG.Domain.Repositories
{
    public enum PasswordResetStatus
    {
        Reset,
        NoVerifiedCode,
        UserNotFound,
        DataFailure
    }
}