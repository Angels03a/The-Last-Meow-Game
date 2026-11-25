using System;
using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>Immutable readiness, countdown and start snapshot without tokens or callbacks.</summary>
    public sealed class LobbyStartSnapshot
    {
        /// <summary>Creates the snapshot from a read-only copy of the list.</summary>
        /// <param name="readyUserIds">Ready players at the time of the snapshot.</param>
        /// <param name="version">Increasing version used to discard late updates for this room.</param>
        /// <param name="countdownId">
        /// Latest countdown generation. Active only when CountdownEndsAtUtc has a value.
        /// </param>
        /// <param name="countdownEndsAtUtc">Server deadline in UTC, or null without an active countdown.</param>
        /// <param name="serverTimeUtc">Snapshot time in UTC. The server decides when the countdown expires.</param>
        /// <param name="matchId">Stable ID of the authorized match, or null before the start.</param>
        /// <param name="matchStartedAtUtc">Authorization time in UTC, or null before the start.</param>
        /// <param name="cardsPerPlayerCount">
        /// Cards per player for the authorized or upcoming match.
        /// </param>
        public LobbyStartSnapshot(
            IReadOnlyList<int> readyUserIds,
            long version,
            long countdownId,
            DateTime? countdownEndsAtUtc,
            DateTime serverTimeUtc,
            Guid? matchId,
            DateTime? matchStartedAtUtc,
            int cardsPerPlayerCount)
        {
            ReadyUserIds = readyUserIds;
            Version = version;
            CountdownId = countdownId;
            CountdownEndsAtUtc = countdownEndsAtUtc;
            ServerTimeUtc = serverTimeUtc;
            MatchId = matchId;
            MatchStartedAtUtc = matchStartedAtUtc;
            CardsPerPlayerCount = cardsPerPlayerCount;
        }

        /// <summary>Ready players at the time of the snapshot.</summary>
        public IReadOnlyList<int> ReadyUserIds { get; }

        /// <summary>Increasing version used to discard late updates for this room.</summary>
        public long Version { get; }

        /// <summary>Latest countdown generation. Active only when CountdownEndsAtUtc has a value.</summary>
        public long CountdownId { get; }

        /// <summary>Server deadline in UTC, or null without an active countdown.</summary>
        public DateTime? CountdownEndsAtUtc { get; }

        /// <summary>Snapshot time in UTC. The server decides when the countdown expires.</summary>
        public DateTime ServerTimeUtc { get; }

        /// <summary>Stable ID of the authorized match, or null before the start.</summary>
        public Guid? MatchId { get; }

        /// <summary>Authorization time in UTC, or null before the start.</summary>
        public DateTime? MatchStartedAtUtc { get; }

        /// <summary>Cards per player for the authorized or upcoming match.</summary>
        public int CardsPerPlayerCount { get; }
    }
}
