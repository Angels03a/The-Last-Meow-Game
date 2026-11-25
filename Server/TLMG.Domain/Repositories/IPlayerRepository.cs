
using System;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Acceso a la persistencia de las cuentas de jugador.
    /// </summary>
    public interface IPlayerRepository
    {
        /// <summary>Comprueba si ya existe una cuenta con el correo dado.</summary>
        /// <param name="email">Correo a buscar, en minúsculas.</param>
        /// <returns>Exists, NotFound o DataFailure; un fallo nunca debe tratarse como "no existe".</returns>
        PlayerExistenceLookupStatus FindEmailExistenceStatus(string email);

        /// <summary>Comprueba si ya existe una cuenta con el nickname dado.</summary>
        /// <param name="nickname">Nickname a buscar.</param>
        /// <returns>Exists, NotFound o DataFailure; un fallo nunca debe tratarse como "no existe".</returns>
        PlayerExistenceLookupStatus FindNicknameExistenceStatus(string nickname);

        /// <summary>Crea el usuario y su jugador asociado en una sola transacción.</summary>
        /// <param name="nickname">Nickname de la cuenta.</param>
        /// <param name="email">Correo de la cuenta, en minúsculas.</param>
        /// <param name="passwordHash">Hash de la contraseña.</param>
        /// <returns>true si la cuenta quedó guardada; false si el guardado falló.</returns>
        bool CreatePlayer(string nickname, string email, string passwordHash);

        /// <summary>Obtiene las credenciales de la cuenta con el correo dado.</summary>
        /// <param name="email">Correo a buscar, en minúsculas.</param>
        /// <returns>Las credenciales y su estado de búsqueda; nunca es null.</returns>
        PlayerCredentials FindCredentialsByEmail(string email);

        /// <summary>Guarda la fecha y hora del último inicio de sesión de una cuenta.</summary>
        /// <param name="userId">Identificador de la cuenta.</param>
        /// <param name="lastLogInTimeUtc">Momento del inicio de sesión, en UTC.</param>
        /// <returns>true si se guardó; false si la cuenta no existe o la operación falló.</returns>
        bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc);

        /// <summary>Returns the highest-scoring active players and optional personal standing.</summary>
        /// <param name="maximumPlayerCount">Maximum number of players to return.</param>
        /// <param name="registeredUserId">Registered user identifier for personal standing, if available.</param>
        /// <returns>Players and query status, always present.</returns>
        LeaderboardLookup FindTopRankedPlayers(
            int maximumPlayerCount,
            int? registeredUserId = null);
    }
}
