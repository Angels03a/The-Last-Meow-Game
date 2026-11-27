using System;
using System.Collections.Generic;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Sessions
{
    /// <summary>Coordinates reservations without exposing mutable gameplay state or accessing SQL under the session lock.</summary>
    public interface IMatchStartRegistry
    {
        /// <summary>Reserves one start attempt or returns the current room state.</summary>
        MatchStartPreparation PrepareMatchStart(SessionChannelCaller caller, int roomId, long countdownId);

        /// <summary>Publishes only a committed and still-valid reservation.</summary>
        LobbyMembershipChange CompleteMatchStart(
            SessionChannelCaller caller, MatchStartPreparation preparation, GameRegistrationResult registrationResult);

        /// <summary>Copies cancelled identities for reconciliation outside the session lock.</summary>
        IReadOnlyList<Guid> FindCancelledMatchIds();

        /// <summary>Removes a cancelled identity once its persistence state has been reconciled.</summary>
        void ConfirmCancelledMatch(Guid matchId);
    }
}
