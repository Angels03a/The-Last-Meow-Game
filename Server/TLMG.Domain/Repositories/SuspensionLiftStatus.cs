namespace TLMG.Domain.Repositories
{
    public enum SuspensionLiftStatus
    {
        DataFailure,
        NotSuspended,
        Lifted,
        StillSuspended,
        Banned,
        InvalidEvent,
        UserNotFound
    }
}
