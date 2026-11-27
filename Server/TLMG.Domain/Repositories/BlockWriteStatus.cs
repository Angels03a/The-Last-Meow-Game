namespace TLMG.Domain.Repositories
{
    public enum BlockWriteStatus
    {
        DataFailure,
        Blocked,
        AlreadyBlocked,
        Unblocked,
        NotBlocked,
        InvalidTarget,
        UserNotFound
    }
}
