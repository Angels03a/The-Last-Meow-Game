using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    public class PlayerProfileRepository : IPlayerProfileRepository
    {
        private const int UniqueIndexViolationNumber = 2601;
        private const int UniqueConstraintViolationNumber = 2627;

        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerProfileRepository));

        public PlayerProfileLookup FindProfileByUserId(int userId)
        {
            PlayerProfileLookup profileLookup;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    IQueryable<PlayerProfileLookup> profileQuery = from user in context.User.AsNoTracking()
                                                                   where user.id_user == userId
                                                                   select new PlayerProfileLookup
                                                                   {
                                                                       Status = PlayerProfileLookupStatus.Found,
                                                                       Nickname = user.nickname,
                                                                       Email = user.email,
                                                                       AvatarKey = user.profile_image,
                                                                       PasswordChangedAtUtc = user.password_changed_at
                                                                   };

                    profileLookup = profileQuery.FirstOrDefault();
                }

                if (profileLookup == null)
                {
                    profileLookup = CreateEmptyLookup(PlayerProfileLookupStatus.NotFound);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error de conexión al consultar el perfil del jugador {0}.", userId),
                    sqlException);
                profileLookup = CreateEmptyLookup(PlayerProfileLookupStatus.DataFailure);
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format("La base de datos falló al consultar el perfil del jugador {0}.", userId),
                    entityException);
                profileLookup = CreateEmptyLookup(PlayerProfileLookupStatus.DataFailure);
            }

            return profileLookup;
        }

        public PlayerProfileUpdateStatus UpdateProfile(int userId, PlayerProfileUpdate profileUpdate)
        {
            PlayerProfileUpdateStatus updateStatus;

            try
            {
                updateStatus = SaveProfileChanges(userId, profileUpdate);
            }
            catch (DbUpdateException dbUpdateException) when (IsDuplicateKeyViolation(dbUpdateException))
            {
                _log.WarnFormat("El nickname solicitado por el jugador {0} ya está en uso.", userId);
                updateStatus = PlayerProfileUpdateStatus.NicknameTaken;
            }
            catch (DbUpdateException dbUpdateException)
            {
                _log.Error(
                    string.Format("Error al guardar el perfil del jugador {0}.", userId),
                    dbUpdateException);
                updateStatus = PlayerProfileUpdateStatus.DataFailure;
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error de conexión al guardar el perfil del jugador {0}.", userId),
                    sqlException);
                updateStatus = PlayerProfileUpdateStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format("La base de datos falló al guardar el perfil del jugador {0}.", userId),
                    entityException);
                updateStatus = PlayerProfileUpdateStatus.DataFailure;
            }

            return updateStatus;
        }

        private static PlayerProfileUpdateStatus SaveProfileChanges(int userId, PlayerProfileUpdate profileUpdate)
        {
            PlayerProfileUpdateStatus updateStatus = PlayerProfileUpdateStatus.PlayerNotFound;

            using (TLMG_DBEntities context = new TLMG_DBEntities())
            {
                User existingUser = (from user in context.User
                                     where user.id_user == userId
                                     select user).FirstOrDefault();

                if (existingUser != null)
                {
                    existingUser.nickname = profileUpdate.Nickname;
                    existingUser.profile_image = profileUpdate.AvatarKey;
                    context.SaveChanges();
                    updateStatus = PlayerProfileUpdateStatus.Updated;
                }
            }

            return updateStatus;
        }

        private static bool IsDuplicateKeyViolation(Exception exception)
        {
            bool isDuplicateKey = false;
            Exception currentException = exception;

            while (currentException != null && !isDuplicateKey)
            {
                SqlException sqlException = currentException as SqlException;
                isDuplicateKey = sqlException != null && IsUniqueViolationNumber(sqlException.Number);
                currentException = currentException.InnerException;
            }

            return isDuplicateKey;
        }

        private static bool IsUniqueViolationNumber(int errorNumber)
        {
            bool isUniqueViolation = errorNumber == UniqueIndexViolationNumber
                || errorNumber == UniqueConstraintViolationNumber;

            return isUniqueViolation;
        }

        private static PlayerProfileLookup CreateEmptyLookup(PlayerProfileLookupStatus status)
        {
            PlayerProfileLookup emptyLookup = new PlayerProfileLookup { Status = status };

            return emptyLookup;
        }

        public PlayerExistenceLookupStatus FindNicknameExistenceStatusForOtherUsers(int userId, string nickname)
        {
            PlayerExistenceLookupStatus existenceStatus;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    bool nicknameExists = (from user in context.User.AsNoTracking()
                                           where user.nickname == nickname && user.id_user != userId
                                           select user).Any();

                    existenceStatus = PlayerExistenceLookupStatus.NotFound;

                    if (nicknameExists)
                    {
                        existenceStatus = PlayerExistenceLookupStatus.Exists;
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al comprobar el nickname de un perfil.", sqlException);
                existenceStatus = PlayerExistenceLookupStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos falló al comprobar el nickname de un perfil.", entityException);
                existenceStatus = PlayerExistenceLookupStatus.DataFailure;
            }

            return existenceStatus;
        }
    }
}