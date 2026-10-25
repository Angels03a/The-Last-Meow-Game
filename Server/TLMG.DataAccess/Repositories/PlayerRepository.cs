using System;
using System.Data.Entity;
using System.Linq;

using TLMG.DataAccess.DatabaseModel;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación del acceso a cuentas con Entity Framework 6.
    /// </summary>
    public class PlayerRepository : IPlayerRepository
    {
        private const int InitialCount = 0;

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
                    last_time = DateTime.Now
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
    }
}