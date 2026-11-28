using System;
using System.Collections.Generic;

using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Manages session lifetime, recovery and connectivity.</summary>
    public interface ISessionRegistry : ISessionLookup
    {
        /// <summary>
        /// Starts a registered session, replacing any previous session and revoking its room access, callbacks and entry permissions.
        /// </summary>
        /// <param name="userId">Account identifier.</param>
        /// <param name="nickname">Account nickname.</param>
        /// <param name="replacedSession">Notifications for the replaced session, or null if none existed.</param>
        /// <returns>The new session token, always present.</returns>
        string StartRegisteredSession(int userId, string nickname, out SessionEnd replacedSession);

        /// <summary>Revokes the token only if the player has no active participation.</summary>
        bool TryLogOut(string sessionToken, out SessionEnd sessionEnd, out bool hasActiveParticipation);

        IReadOnlyList<Action> EvaluateConnectionDeadlines(bool force = true);

        ResultWithNotification<OperationRecoveryContextResult> GetRecoveryContext(string sessionToken);

        IReadOnlyList<Action> ProcessHeartbeat(string token, string channelId, HeartbeatChannel channel,
            int sequence, int confirmedSequence, Action<int, string> echo);
    }
}
