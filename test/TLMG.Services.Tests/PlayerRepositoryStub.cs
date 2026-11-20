using System;
using System.Collections.Generic;

using TLMG.Domain.Repositories;

namespace TLMG.Services.Tests
{
    internal sealed class PlayerRepositoryStub : IPlayerRepository
    {
        internal PlayerExistenceLookupStatus EmailStatus { get; set; }

        internal PlayerExistenceLookupStatus NicknameStatus { get; set; }

        internal bool SaveSucceeds { get; set; } = true;

        internal bool UpdateSucceeds { get; set; } = true;

        internal bool ThrowOnLookup { get; set; }

        internal PlayerCredentials Credentials { get; set; } = new PlayerCredentials();

        /// <summary>Result returned by the leaderboard query.</summary>
        internal LeaderboardLookup Leaderboard { get; set; } = new LeaderboardLookup();

        /// <summary>Player limits received by the leaderboard query, in call order.</summary>
        internal List<int> RequestedLeaderboardLimits { get; } = new List<int>();

        /// <summary>Emails received by the lookups of existence and of credentials, in call order.</summary>
        internal List<string> LookedUpEmails { get; } = new List<string>();

        /// <summary>Nicknames received by the existence lookup, in call order.</summary>
        internal List<string> LookedUpNicknames { get; } = new List<string>();

        /// <summary>Accounts that the service asked to create, in call order.</summary>
        internal List<CreatedPlayerRecord> CreatedPlayers { get; } = new List<CreatedPlayerRecord>();

        /// <summary>Users whose last log-in time the service asked to update, in call order.</summary>
        internal List<int> UpdatedLogInUserIds { get; } = new List<int>();

        /// <inheritdoc />
        public PlayerExistenceLookupStatus FindEmailExistenceStatus(string email)
        {
            LookedUpEmails.Add(email);
            if (ThrowOnLookup)
            {
                throw new InvalidOperationException("Fallo sintético del repositorio.");
            }
            return EmailStatus;
        }

        /// <inheritdoc />
        public PlayerExistenceLookupStatus FindNicknameExistenceStatus(string nickname)
        {
            LookedUpNicknames.Add(nickname);
            return NicknameStatus;
        }

        /// <inheritdoc />
        public bool CreatePlayer(string nickname, string email, string passwordHash)
        {
            CreatedPlayers.Add(new CreatedPlayerRecord(nickname, email, passwordHash));
            return SaveSucceeds;
        }

        /// <inheritdoc />
        public PlayerCredentials FindCredentialsByEmail(string email)
        {
            LookedUpEmails.Add(email);
            if (ThrowOnLookup)
            {
                throw new InvalidOperationException("Fallo sintético del repositorio.");
            }
            return Credentials;
        }

        /// <inheritdoc />
        public bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc)
        {
            UpdatedLogInUserIds.Add(userId);
            return UpdateSucceeds;
        }

        /// <inheritdoc />
        public LeaderboardLookup FindTopRankedPlayers(int maximumPlayerCount)
        {
            RequestedLeaderboardLimits.Add(maximumPlayerCount);

            return Leaderboard;
        }

        /// <summary>Arguments of one call to CreatePlayer.</summary>
        internal sealed class CreatedPlayerRecord
        {
            internal CreatedPlayerRecord(string nickname, string email, string passwordHash)
            {
                Nickname = nickname;
                Email = email;
                PasswordHash = passwordHash;
            }

            internal string Nickname { get; }

            internal string Email { get; }

            internal string PasswordHash { get; }
        }
    }
}
