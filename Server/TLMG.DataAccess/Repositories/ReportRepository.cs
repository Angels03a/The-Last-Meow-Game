using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    public sealed class ReportRepository : IReportRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportRepository));

        private static readonly IReadOnlyDictionary<ReportReason, string> _reasonCodes =
            new Dictionary<ReportReason, string>
            {
                { ReportReason.OffensiveLanguage, "REASON_OFFENSIVE_LANG" },
                { ReportReason.Harassment, "REASON_HARASSMENT" },
                { ReportReason.Cheating, "REASON_CHEATING" },
                { ReportReason.Spam, "REASON_SPAM" },
                { ReportReason.InappropriateName, "REASON_INAPPROPRIATE_NAME" },
                { ReportReason.Other, "REASON_OTHER" }
            };

        private static readonly IReadOnlyDictionary<string, ReportWriteStatus> _reportStatuses =
            new Dictionary<string, ReportWriteStatus>(StringComparer.Ordinal)
            {
                { nameof(ReportWriteStatus.DataFailure), ReportWriteStatus.DataFailure },
                { nameof(ReportWriteStatus.Reported), ReportWriteStatus.Reported },
                { nameof(ReportWriteStatus.AlreadyReported), ReportWriteStatus.AlreadyReported },
                { nameof(ReportWriteStatus.Suspended), ReportWriteStatus.Suspended },
                { nameof(ReportWriteStatus.Banned), ReportWriteStatus.Banned },
                { nameof(ReportWriteStatus.InvalidReport), ReportWriteStatus.InvalidReport },
                { nameof(ReportWriteStatus.UserNotFound), ReportWriteStatus.UserNotFound },
                { nameof(ReportWriteStatus.ReporterNotActive), ReportWriteStatus.ReporterNotActive },
                { nameof(ReportWriteStatus.TargetNotActive), ReportWriteStatus.TargetNotActive },
                { nameof(ReportWriteStatus.InvalidMatch), ReportWriteStatus.InvalidMatch }
            };

        private static readonly IReadOnlyDictionary<string, SuspensionLiftStatus> _suspensionStatuses =
            new Dictionary<string, SuspensionLiftStatus>(StringComparer.Ordinal)
            {
                { nameof(SuspensionLiftStatus.DataFailure), SuspensionLiftStatus.DataFailure },
                { nameof(SuspensionLiftStatus.NotSuspended), SuspensionLiftStatus.NotSuspended },
                { nameof(SuspensionLiftStatus.Lifted), SuspensionLiftStatus.Lifted },
                { nameof(SuspensionLiftStatus.StillSuspended), SuspensionLiftStatus.StillSuspended },
                { nameof(SuspensionLiftStatus.Banned), SuspensionLiftStatus.Banned },
                { nameof(SuspensionLiftStatus.InvalidEvent), SuspensionLiftStatus.InvalidEvent },
                { nameof(SuspensionLiftStatus.UserNotFound), SuspensionLiftStatus.UserNotFound }
            };

        public ReportWriteStatus RegisterPlayerReport(PlayerReport report)
        {
            const string command = "EXEC dbo.RegisterPlayerReport @id_user_reporter, @id_user_reported, "
                + "@room_scope_id, @reason, @comment, @match_id";
            ReportWriteStatus status = ReportWriteStatus.InvalidReport;
            string reasonCode;

            if (report != null && _reasonCodes.TryGetValue(report.Reason, out reasonCode))
            {
                SqlParameter[] parameters = CreateReportParameters(report, reasonCode);
                string procedureStatus = ReadProcedureStatus(command, parameters);

                if (!_reportStatuses.TryGetValue(procedureStatus, out status))
                {
                    status = ReportWriteStatus.DataFailure;
                    _log.ErrorFormat("RegisterPlayerReport returned an unsupported status: {0}.", procedureStatus);
                }
            }

            return status;
        }

        public SuspensionLiftStatus LiftExpiredSuspension(int userId, DateTime nowUtc)
        {
            const string command = "EXEC dbo.LiftExpiredSuspension @id_user, @now_utc";
            const byte timestampScale = 3;
            SuspensionLiftStatus status = SuspensionLiftStatus.InvalidEvent;

            if (userId > ValidationRules.NoUserId && nowUtc.Kind == DateTimeKind.Utc)
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@id_user", SqlDbType.Int) { Value = userId },
                    new SqlParameter("@now_utc", SqlDbType.DateTime2)
                    {
                        Scale = timestampScale,
                        Value = nowUtc
                    }
                };
                string procedureStatus = ReadProcedureStatus(command, parameters);

                if (!_suspensionStatuses.TryGetValue(procedureStatus, out status))
                {
                    status = SuspensionLiftStatus.DataFailure;
                    _log.ErrorFormat("LiftExpiredSuspension returned an unsupported status: {0}.", procedureStatus);
                }
            }

            return status;
        }

        private static SqlParameter[] CreateReportParameters(PlayerReport report, string reasonCode)
        {
            const int reasonCodeLength = 40;
            const int unlimitedTextLength = -1;
            object commentValue = DBNull.Value;
            object matchValue = DBNull.Value;

            if (!string.IsNullOrEmpty(report.Comment))
            {
                commentValue = report.Comment;
            }

            if (report.MatchId.HasValue)
            {
                matchValue = report.MatchId.Value;
            }

            SqlParameter[] parameters =
            {
                new SqlParameter("@id_user_reporter", SqlDbType.Int) { Value = report.ReporterUserId },
                new SqlParameter("@id_user_reported", SqlDbType.Int) { Value = report.ReportedUserId },
                new SqlParameter("@room_scope_id", SqlDbType.UniqueIdentifier) { Value = report.RoomScopeId },
                new SqlParameter("@reason", SqlDbType.VarChar, reasonCodeLength) { Value = reasonCode },
                new SqlParameter("@comment", SqlDbType.NVarChar, unlimitedTextLength) { Value = commentValue },
                new SqlParameter("@match_id", SqlDbType.UniqueIdentifier) { Value = matchValue }
            };

            return parameters;
        }

        private static string ReadProcedureStatus(string command, SqlParameter[] parameters)
        {
            string status = nameof(ReportWriteStatus.DataFailure);

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    ProcedureStatusRow row = context.Database.SqlQuery<ProcedureStatusRow>(command, parameters)
                        .FirstOrDefault();

                    if (row != null && !string.IsNullOrWhiteSpace(row.Status))
                    {
                        status = row.Status;
                    }
                    else
                    {
                        _log.Error("The moderation procedure did not return a status.");
                    }
                }
            }
            catch (SqlException sqlException)
            {
                status = nameof(ReportWriteStatus.DataFailure);
                _log.Error("SQL Server could not execute the moderation procedure.", sqlException);
            }
            catch (DataException dataException)
            {
                status = nameof(ReportWriteStatus.DataFailure);
                _log.Error("The moderation procedure could not be read.", dataException);
            }

            return status;
        }

        private sealed class ProcedureStatusRow
        {
            public string Status { get; set; }
        }
    }
}
