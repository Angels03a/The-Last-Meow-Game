using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Moderation
{
    public sealed class ReportService
    {
        private const int MaximumCommentLength = 300;
        private const string InvalidReportErrorKey = "Moderation_InvalidReportError";
        private const string ReportTargetUnavailableErrorKey = "Moderation_ReportTargetUnavailableError";
        private const string AlreadyReportedErrorKey = "Moderation_AlreadyReportedError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportService));
        private readonly IReportRepository _reportRepository;
        private readonly IReportSessionAccess _sessionAccess;
        private readonly IMatchStartCoordinator _matchStartCoordinator;

        internal object AccountStateSynchronizationRoot { get; } = new object();

        public ReportService(IReportRepository reportRepository, IReportSessionAccess sessionAccess,
            IMatchStartCoordinator matchStartCoordinator)
        {
            _reportRepository = reportRepository;
            _sessionAccess = sessionAccess;
            _matchStartCoordinator = matchStartCoordinator;
        }

        public ResultWithNotification<OperationResult> ReportPlayer(string sessionToken, ReportSubmission submission)
        {
            OperationResult operation = new OperationResult { ErrorKey = ErrorKeys.Common.Unexpected };
            IReadOnlyList<Action> notifications = Array.Empty<Action>();

            lock (AccountStateSynchronizationRoot)
            {
                SessionAuthorization authorization = _sessionAccess.Authorize(sessionToken, SystemPermission.ReportPlayer);
                PlayerReport report = new PlayerReport();

                if (authorization == null)
                {
                    _log.Error("No se obtuvo una autorización para registrar el reporte.");
                }
                else if (authorization.Status != AuthorizationStatus.Granted)
                {
                    operation.ErrorKey = FindAuthorizationErrorKey(authorization.Status);
                }
                else if (authorization.UserId <= ValidationRules.NoUserId || !TryReadSubmission(submission, report))
                {
                    operation.ErrorKey = InvalidReportErrorKey;
                }
                else if (!_sessionAccess.TryPrepareReport(sessionToken, report))
                {
                    operation.ErrorKey = ReportTargetUnavailableErrorKey;
                }
                else
                {
                    notifications = RegisterReport(report, operation);
                }
            }

            ResultWithNotification<OperationResult> completion =
                new ResultWithNotification<OperationResult>(operation, notifications);

            return completion;
        }

        internal SuspensionLiftStatus CheckAccountState(int userId)
        {
            SuspensionLiftStatus status = _reportRepository.LiftExpiredSuspension(userId, DateTime.UtcNow);

            return status;
        }

        private static bool TryReadSubmission(ReportSubmission submission, PlayerReport report)
        {
            bool isValid = false;
            ReportReason reason;

            if (submission != null && submission.TargetUserId > ValidationRules.NoUserId
                && Enum.TryParse(submission.ReasonCode, false, out reason)
                && Enum.IsDefined(typeof(ReportReason), reason) && reason != ReportReason.Unspecified
                && string.Equals(reason.ToString(), submission.ReasonCode, StringComparison.Ordinal))
            {
                string comment = string.Empty;

                if (submission.Comment != null)
                {
                    comment = submission.Comment;
                }

                if (comment.Length <= MaximumCommentLength)
                {
                    report.ReportedUserId = submission.TargetUserId;
                    report.Reason = reason;
                    report.Comment = comment.Trim();
                    isValid = true;
                }
            }

            return isValid;
        }

        private IReadOnlyList<Action> RegisterReport(PlayerReport report, OperationResult operationRegisterReport)
        {
            IReadOnlyList<Action> notifications = Array.Empty<Action>();
            ReportWriteStatus status = _reportRepository.RegisterPlayerReport(report);

            if (status == ReportWriteStatus.Reported || status == ReportWriteStatus.Suspended
                || status == ReportWriteStatus.Banned)
            {
                operationRegisterReport.IsSuccess = true;
                operationRegisterReport.ErrorKey = string.Empty;
                _log.InfoFormat("Reporte registrado del jugador {0} contra el jugador {1}.",
                    report.ReporterUserId, report.ReportedUserId);

                if (status == ReportWriteStatus.Suspended || status == ReportWriteStatus.Banned)
                {
                    notifications = _sessionAccess.RevokeSanctionedSession(report.ReportedUserId);
                    _matchStartCoordinator.ReconcileCancelledStarts();
                }
            }
            else
            {
                operationRegisterReport.ErrorKey = FindReportErrorKey(status);
            }

            return notifications;
        }

        private static string FindReportErrorKey(ReportWriteStatus status)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (status == ReportWriteStatus.AlreadyReported)
            {
                errorKey = AlreadyReportedErrorKey;
            }
            else if (status == ReportWriteStatus.InvalidReport)
            {
                errorKey = InvalidReportErrorKey;
            }
            else if (status == ReportWriteStatus.TargetNotActive || status == ReportWriteStatus.UserNotFound
                || status == ReportWriteStatus.InvalidMatch)
            {
                errorKey = ReportTargetUnavailableErrorKey;
            }
            else if (status == ReportWriteStatus.ReporterNotActive)
            {
                errorKey = ErrorKeys.Common.PermissionDenied;
            }

            return errorKey;
        }

        private static string FindAuthorizationErrorKey(AuthorizationStatus status)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (status == AuthorizationStatus.InvalidSession)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }
            else if (status == AuthorizationStatus.PermissionDenied)
            {
                errorKey = ErrorKeys.Common.PermissionDenied;
            }

            return errorKey;
        }
    }
}
