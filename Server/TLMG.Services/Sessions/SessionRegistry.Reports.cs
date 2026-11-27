using System;
using System.Collections.Generic;

using TLMG.Domain.Repositories;
using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry : IReportSessionAccess
    {
        public bool TryPrepareReport(string sessionToken, PlayerReport report)
        {
            bool wasPrepared = false;

            if (!string.IsNullOrEmpty(sessionToken) && report != null)
            {
                lock (_sessionsLock)
                {
                    wasPrepared = TryPrepareReportInLock(sessionToken, report);
                }
            }

            return wasPrepared;
        }

        public IReadOnlyList<Action> RevokeSanctionedSession(int userId)
        {
            IReadOnlyList<Action> notifications = Array.Empty<Action>();
            SessionEnd sessionEnd = null;
            bool wasRevoked = false;

            lock (_sessionsLock)
            {
                string sessionToken;
                PlayerSession session;
                if (_tokensByUserId.TryGetValue(userId, out sessionToken)
                    && _sessionsByToken.TryGetValue(sessionToken, out session))
                {
                    sessionEnd = EndSessionInLock(session, RoomExitCause.Sanctioned);
                    wasRevoked = true;
                }
            }

            if (wasRevoked)
            {
                PersistPendingMatchFinalizations();
                notifications = SessionNotificationHelper.CreateRevokedSessionNotifications(sessionEnd);
                _log.InfoFormat("Se revocó la sesión del jugador sancionado {0}.", userId);
            }

            return notifications;
        }

        private bool TryPrepareReportInLock(string sessionToken, PlayerReport report)
        {
            bool wasPrepared = false;
            PlayerSession reporter;
            int reporterRoomId;
            int targetRoomId;
            LobbyStartState roomState;

            if (_sessionsByToken.TryGetValue(sessionToken, out reporter) && reporter.IsRegistered
                && reporter.UserId != report.ReportedUserId && IsRegisteredReportTargetInLock(report.ReportedUserId)
                && _roomIdsByUserId.TryGetValue(reporter.UserId, out reporterRoomId)
                && _roomIdsByUserId.TryGetValue(report.ReportedUserId, out targetRoomId)
                && reporterRoomId == targetRoomId && _startsByRoomId.TryGetValue(reporterRoomId, out roomState)
                && roomState.RoomScopeId != Guid.Empty)
            {
                report.ReporterUserId = reporter.UserId;
                report.RoomScopeId = roomState.RoomScopeId;
                report.MatchId = roomState.MatchId;
                wasPrepared = true;
            }

            return wasPrepared;
        }

        private bool IsRegisteredReportTargetInLock(int targetUserId)
        {
            string targetToken;
            PlayerSession target;
            bool isRegistered = _tokensByUserId.TryGetValue(targetUserId, out targetToken)
                && _sessionsByToken.TryGetValue(targetToken, out target) && target.IsRegistered;

            return isRegistered;
        }
    }
}
