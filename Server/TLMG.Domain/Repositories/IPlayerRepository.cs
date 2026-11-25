using System;

namespace TLMG.Domain.Repositories
{
    public interface IPlayerRepository
    {
        PlayerExistenceLookupStatus FindEmailExistenceStatus(string email);
        PlayerExistenceLookupStatus FindNicknameExistenceStatus(string nickname);
        bool CreatePlayer(string nickname, string email, string passwordHash);
        PlayerCredentials FindCredentialsByEmail(string email);
        bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc);
        LeaderboardLookup FindTopRankedPlayers(int maximumPlayerCount, int? registeredUserId = null);
    }
}
