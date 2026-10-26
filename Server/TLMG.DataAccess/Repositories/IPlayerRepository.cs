using System;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Acceso a la persistencia de las cuentas de jugador.
    /// </summary>
    public interface IPlayerRepository
    {
        /// <summary>Indica si ya existe una cuenta con el correo dado.</summary>
        /// <param name="email">Correo a buscar, en minúsculas.</param>
        /// <returns>true si el correo ya está registrado; en caso contrario, false.</returns>
        bool PlayerExistsByEmail(string email);

        /// <summary>Indica si ya existe una cuenta con el nickname dado.</summary>
        /// <param name="nickname">Nickname a buscar.</param>
        /// <returns>true si el nickname ya está registrado; en caso contrario, false.</returns>
        bool PlayerExistsByNickname(string nickname);

        /// <summary>Crea el usuario y su jugador asociado en una sola transacción.</summary>
        /// <param name="nickname">Nickname de la cuenta.</param>
        /// <param name="email">Correo de la cuenta, en minúsculas.</param>
        /// <param name="passwordHash">Hash de la contraseña.</param>
        void CreatePlayer(string nickname, string email, string passwordHash);

        /// <summary>Obtiene las credenciales de la cuenta con el correo dado.</summary>
        /// <param name="email">Correo a buscar, en minúsculas.</param>
        /// <returns>Las credenciales y su estado de búsqueda; nunca es null.</returns>
        PlayerCredentials FindCredentialsByEmail(string email);

        /// <summary>Guarda la fecha y hora del último inicio de sesión de una cuenta.</summary>
        /// <param name="userId">Identificador de la cuenta.</param>
        /// <param name="lastLogInTimeUtc">Momento del inicio de sesión, en UTC.</param>
        /// <returns>true si se guardó; false si la cuenta no existe o la operación falló.</returns>
        bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc);
    }
}