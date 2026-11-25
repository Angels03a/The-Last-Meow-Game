using System;

namespace TLMG.Domain.Repositories
{
    /// <summary>Persists match identities and reconciles unpublished or interrupted matches.</summary>
    public interface IGameHistoryRepository
    {
        /// <summary>Registers the same authorized match at most once.</summary>
        GameRegistrationResult RegisterStartedMatch(GameRegistration registration);

        /// <summary>Commits the final result and registered player statistics together.</summary>
        GameFinalizationStatus FinalizeMatch(GameFinalization gameFinalization);

        /// <summary>Closes a cancelled reservation without awarding historical points.</summary>
        bool AbandonUnpublishedMatch(Guid matchId);

        /// <summary>Closes interrupted matches before the sole authoritative server accepts new starts.</summary>
        bool RecoverInterruptedMatches();
    }
}
