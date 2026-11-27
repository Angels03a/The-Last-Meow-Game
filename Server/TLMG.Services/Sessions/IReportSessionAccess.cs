using System;
using System.Collections.Generic;

using TLMG.Domain.Repositories;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Verifies report eligibility against authoritative session state and revokes sanctioned sessions.
    /// </summary>
    public interface IReportSessionAccess : ISessionAuthorizer
    {
        /// <summary>Completes the report identity and scope only while both registered players share a room.</summary>
        bool TryPrepareReport(string sessionToken, PlayerReport report);

        /// <summary>Revokes the account's current session and returns notifications to dispatch outside the lock.</summary>
        IReadOnlyList<Action> RevokeSanctionedSession(int userId);
    }
}
