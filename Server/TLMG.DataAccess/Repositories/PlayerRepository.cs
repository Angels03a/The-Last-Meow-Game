using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación del acceso a cuentas con Entity Framework 6.
    /// </summary>
    public class PlayerRepository : IPlayerRepository
    {
        private const int InitialCount = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerRepository));

        // FIXME: Representar los estados de usuario con un enum en lugar de cadenas. (BL-047).
        private const string DefaultUserState = "USER_ACTIVE";

        // FIXME: Implementar la gestión de imágenes de perfil; se define en el pendiente de imágenes. (BL-045).
        private const string DefaultProfileImage = "/img/default.png";

        /// <inheritdoc />
        public bool PlayerExistsByEmail(string email)
        {
            bool playerExists;

            using (TLMG_DBEntities context = new TLMG_DBEntities())
            {
                playerExists = context.User.Any(user => user.email == email);
            }

            return playerExists;
        }

        /// <inheritdoc />
        public bool PlayerExistsByNickname(string nickname)
        {
            bool playerExists;

            using (TLMG_DBEntities context = new TLMG_DBEntities())
            {
                playerExists = context.User.Any(user => user.nickname == nickname);
            }

            return playerExists;
        }

        /// <inheritdoc />
        public void CreatePlayer(string nickname, string email, string passwordHash)
        {
            using (TLMG_DBEntities context = new TLMG_DBEntities())
            using (DbContextTransaction transaction = context.Database.BeginTransaction())
            {
                User newUser = new User
                {
                    email = email,
                    nickname = nickname,
                    password_hash = passwordHash,
                    state = DefaultUserState,
                    profile_image = DefaultProfileImage,
                    // Se guarda en UTC con el reloj del servidor
                    last_time = DateTime.UtcNow
                };

                context.User.Add(newUser);
                context.SaveChanges();

                Player newPlayer = new Player
                {
                    id_user = newUser.id_user,
                    total_points = InitialCount,
                    total_wins = InitialCount,
                    total_games_played = InitialCount
                };

                context.Player.Add(newPlayer);
                context.SaveChanges();

                transaction.Commit();
            }
        }

        /// <inheritdoc />
        public PlayerCredentials FindCredentialsByEmail(string email)
        {
            PlayerCredentials playerCredentials;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    playerCredentials = context.User
                        .AsNoTracking()
                        .Where(user => user.email == email)
                        .Select(user => new PlayerCredentials
                        {
                            Status = CredentialsLookupStatus.Found,
                            UserId = user.id_user,
                            PasswordHash = user.password_hash,
                            State = user.state
                        })
                        .FirstOrDefault();
                }

                if (playerCredentials == null)
                {
                    playerCredentials = new PlayerCredentials { Status = CredentialsLookupStatus.NotFound };
                }
            }
            catch (SqlException ex)
            {
                _log.Error("Error de SQL Server al buscar las credenciales de una cuenta.", ex);
                playerCredentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };
            }
            catch (EntityException ex)
            {
                _log.Error("La base de datos no respondió al buscar las credenciales de una cuenta.", ex);
                playerCredentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };
            }

            return playerCredentials;
        }

        /// <inheritdoc />
        public bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc)
        {
            bool wasUpdated = false;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    User existingUser = context.User.FirstOrDefault(user => user.id_user == userId);

                    if (existingUser != null)
                    {
                        existingUser.last_time = lastLogInTimeUtc;
                        context.SaveChanges();
                        wasUpdated = true;
                    }
                }
            }
            catch (SqlException ex)
            {
                _log.Error(string.Format("Error de SQL Server al guardar el acceso del jugador {0}.", userId), ex);
            }
            catch (EntityException ex)
            {
                _log.Error(string.Format("La base de datos falló al guardar el acceso del jugador {0}.", userId), ex);
            }

            return wasUpdated;
        }
    }
}