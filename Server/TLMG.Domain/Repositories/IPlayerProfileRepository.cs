using TLMG.Domain.Entities;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Persistence access for the editable profile of a player, kept apart from account access so neither
    /// interface grows past the segregation limit.
    /// </summary>
    public interface IPlayerProfileRepository
    {
        /// <summary>Reads the profile of an account; a failure must never be treated as "not found".</summary>
        /// <param name="userId">Account identifier resolved from the session.</param>
        /// <returns>The profile and its lookup status; never null.</returns>
        PlayerProfileLookup FindProfileByUserId(int userId);

        /// <summary>
        /// Checks whether another account already uses the nickname, so the owner can keep or re-case their own.
        /// </summary>
        /// <param name="userId">Account that wants the nickname, excluded from the search.</param>
        /// <param name="nickname">Nickname to look for.</param>
        /// <returns>Exists, NotFound or DataFailure; a failure must never be treated as "not found".</returns>
        PlayerExistenceLookupStatus FindNicknameExistenceStatusForOtherUsers(int userId, string nickname);

        /// <summary>Saves nickname and avatar; the unique index remains the final arbiter against races.</summary>
        /// <param name="userId">Account identifier resolved from the session.</param>
        /// <param name="profileUpdate">Already validated new values.</param>
        /// <returns>Updated, NicknameTaken, PlayerNotFound or DataFailure.</returns>
        PlayerProfileUpdateStatus UpdateProfile(int userId, PlayerProfileUpdate profileUpdate);
    }
}