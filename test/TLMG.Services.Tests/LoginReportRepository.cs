using System;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Tests
{
    /// <summary>Reads the current account prepared by the login fixture.</summary>
    internal sealed class LoginReportRepository : IReportRepository
    {
        private readonly PlayerRepositoryStub _players;

        internal SuspensionLiftStatus? StatusOverride { get; set; }

        internal LoginReportRepository(PlayerRepositoryStub players)
        {
            _players = players;
        }

        public SuspensionLiftStatus LiftExpiredSuspension(int userId, DateTime nowUtc)
        {
            SuspensionLiftStatus status = SuspensionLiftStatus.InvalidEvent;
            if (userId > 0 && nowUtc.Kind == DateTimeKind.Utc)
            {
                status = SuspensionLiftStatus.NotSuspended;
                PlayerCredentials credentials = _players.Credentials;
                if (credentials != null && credentials.UserId == userId
                    && credentials.State == ValidationRules.SuspendedUserState)
                {
                    status = SuspensionLiftStatus.DataFailure;
                }
            }

            if (StatusOverride.HasValue)
            {
                status = StatusOverride.Value;
            }

            return status;
        }

        public ReportWriteStatus RegisterPlayerReport(PlayerReport report)
        {
            return ReportWriteStatus.DataFailure;
        }
    }
}
