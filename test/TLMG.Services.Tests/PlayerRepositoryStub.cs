using System;

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

        /// <inheritdoc />
        public PlayerExistenceLookupStatus FindEmailExistenceStatus(string email)
        {
            if (ThrowOnLookup)
            {
                throw new InvalidOperationException("Fallo sintético del repositorio.");
            }
            return EmailStatus;
        }

        /// <inheritdoc />
        public PlayerExistenceLookupStatus FindNicknameExistenceStatus(string nickname)
        {
            return NicknameStatus;
        }

        /// <inheritdoc />
        public bool CreatePlayer(string nickname, string email, string passwordHash)
        {
            return SaveSucceeds;
        }

        /// <inheritdoc />
        public PlayerCredentials FindCredentialsByEmail(string email)
        {
            if (ThrowOnLookup)
            {
                throw new InvalidOperationException("Fallo sintético del repositorio.");
            }
            return Credentials;
        }

        /// <inheritdoc />
        public bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc)
        {
            return UpdateSucceeds;
        }

        /// <inheritdoc />
        public LeaderboardLookup FindTopRankedPlayers(int maximumPlayerCount)
        {
            throw new NotSupportedException("Consulta fuera del alcance de estas pruebas.");
        }
    }
}
