using System;
using System.Collections.Generic;

using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Engine state and separate registry data needed to build one player's view. Used only inside the registry under
    /// _sessionsLock.
    /// </summary>
    internal sealed class MatchViewContext
    {
        /// <summary>Creates the view context.</summary>
        /// <param name="matchId">Match for this view.</param>
        /// <param name="version">Match version.</param>
        /// <param name="matchState">Engine state, used for reading only.</param>
        /// <param name="nicknamesByUserId">Nicknames recorded at match start.</param>
        /// <param name="lastChallenge">Latest challenge outcome, or null if none is current.</param>
        /// <param name="guestUserIds">Guest identifiers recorded at match start; guests receive zero points.</param>
        internal MatchViewContext(
            Guid matchId,
            int version,
            MatchState matchState,
            IReadOnlyDictionary<int, string> nicknamesByUserId,
            MatchChallengeRecord lastChallenge,
            IReadOnlyList<int> guestUserIds = null)
        {
            MatchId = matchId;
            Version = version;
            MatchState = matchState;
            NicknamesByUserId = nicknamesByUserId;
            LastChallenge = lastChallenge;
            GuestUserIds = guestUserIds;
        }

        /// <summary>Match for this view.</summary>
        internal Guid MatchId { get; }

        /// <summary>Match version.</summary>
        internal int Version { get; }

        /// <summary>Engine state.</summary>
        internal MatchState MatchState { get; }

        /// <summary>Nicknames recorded at match start.</summary>
        internal IReadOnlyDictionary<int, string> NicknamesByUserId { get; }

        /// <summary>Guests of the match, whose standings show zero points.</summary>
        internal IReadOnlyList<int> GuestUserIds { get; }

        /// <summary>Latest challenge outcome, or null if none is current.</summary>
        internal MatchChallengeRecord LastChallenge { get; }
    }
}
