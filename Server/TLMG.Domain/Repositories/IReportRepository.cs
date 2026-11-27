using System;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Persists reports and resolves account sanctions through atomic database operations.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// Registers an authorized report and applies any resulting sanction in the same transaction.
        /// </summary>
        ReportWriteStatus RegisterPlayerReport(PlayerReport report);

        /// <summary>
        /// Restores an account only if its temporary suspension has expired; permanent bans are preserved.
        /// </summary>
        SuspensionLiftStatus LiftExpiredSuspension(int userId, DateTime nowUtc);
    }
}
