namespace TLMG.Domain.Repositories
{
    public enum PasswordChangeStatus
    {
        Changed,
        StaleCredentials,
        UserNotFound,
        DataFailure
    }
}