namespace TLMG.Domain.Repositories
{
    public enum ReportWriteStatus
    {
        DataFailure,
        Reported,
        AlreadyReported,
        Suspended,
        Banned,
        InvalidReport,
        UserNotFound,
        ReporterNotActive,
        TargetNotActive,
        InvalidMatch
    }
}
