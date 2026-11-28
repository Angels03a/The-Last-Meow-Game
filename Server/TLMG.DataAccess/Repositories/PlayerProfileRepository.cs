using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

using PlayerProfileUpdate = TLMG.Domain.Entities.PlayerProfileUpdate;

namespace TLMG.DataAccess.Repositories
{
    public class PlayerProfileRepository : IPlayerProfileRepository
    {
        private const int UniqueIndexViolationNumber = 2601;
        private const int UniqueConstraintViolationNumber = 2627;

        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerProfileRepository));

        public PlayerProfileLookup FindProfileByUserId(int userId)
        {
            PlayerProfileLookup profileLookup = CreateEmptyLookup(PlayerProfileLookupStatus.DataFailure);

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    PlayerProfileLookup storedProfile = ReadProfile(context, userId);

                    if (storedProfile == null)
                    {
                        storedProfile = CreateEmptyLookup(PlayerProfileLookupStatus.NotFound);
                    }
                    else if (!TryPopulateStatistics(context, userId, storedProfile))
                    {
                        storedProfile = CreateEmptyLookup(PlayerProfileLookupStatus.DataFailure);
                        _log.WarnFormat("No se encontraron estadísticas válidas para el jugador {0}.", userId);
                    }
                    else
                    {
                        storedProfile.Status = PlayerProfileLookupStatus.Found;
                    }

                    transaction.Commit();
                    profileLookup = storedProfile;
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format("No se pudo leer el perfil del jugador {0}.", userId), sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error(string.Format("No se pudieron leer los datos del perfil del jugador {0}.", userId), dataException);
            }

            return profileLookup;
        }

        public PlayerProfileUpdateStatus UpdateProfile(int userId, PlayerProfileUpdate profileUpdate)
        {
            PlayerProfileUpdateStatus updateStatus = PlayerProfileUpdateStatus.DataFailure;

            try
            {
                updateStatus = SaveProfileChanges(userId, profileUpdate);
            }
            catch (DbUpdateException dbUpdateException)
            {
                if (IsDuplicateKeyViolation(dbUpdateException))
                {
                    _log.WarnFormat("El apodo solicitado por el jugador {0} ya está en uso.", userId);
                    updateStatus = PlayerProfileUpdateStatus.NicknameTaken;
                }
                else
                {
                    _log.Error(string.Format("No se pudo guardar el perfil del jugador {0}.", userId), dbUpdateException);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format("No se pudo guardar el perfil del jugador {0}.", userId), sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error(string.Format("No se pudieron guardar los datos del perfil del jugador {0}.", userId), dataException);
            }

            return updateStatus;
        }

        public PlayerExistenceLookupStatus FindNicknameExistenceStatusForOtherUsers(int userId, string nickname)
        {
            PlayerExistenceLookupStatus existenceStatus = PlayerExistenceLookupStatus.DataFailure;

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
                _log.Error("No se pudo consultar la disponibilidad del apodo del perfil.", sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error("No se pudo leer la disponibilidad del apodo del perfil.", dataException);
            }

            return existenceStatus;
        }

        private static PlayerProfileLookup ReadProfile(TLMG_DBEntities context, int userId)
        {
            PlayerProfileLookup profileLookup = (
                from user in context.User.AsNoTracking()
                where user.id_user == userId
                select new PlayerProfileLookup
                {
                    Nickname = user.nickname,
                    Email = user.email,
                    AvatarKey = user.profile_image,
                    PasswordChangedAtUtc = user.password_changed_at
                }).FirstOrDefault();

            return profileLookup;
        }

        private static bool TryPopulateStatistics(TLMG_DBEntities context, int userId, PlayerProfileLookup profile)
        {
            const int MinimumRank = 1;
            IQueryable<Player> activePlayers = PlayerRankingQuery.FindActivePlayers(context);
            Player currentPlayer = (from player in activePlayers
                                    where player.id_user == userId
                                    select player).FirstOrDefault();
            bool hasStatistics = currentPlayer != null
                && currentPlayer.total_points >= ValidationRules.InitialStatisticValue
                && currentPlayer.total_wins >= ValidationRules.InitialStatisticValue
                && currentPlayer.total_games_played >= currentPlayer.total_wins;

            if (hasStatistics)
            {
                profile.TotalPoints = currentPlayer.total_points;
                profile.TotalWins = currentPlayer.total_wins;
                profile.TotalGamesPlayed = currentPlayer.total_games_played;
                profile.PersonalRank = PlayerRankingQuery.FindRank(activePlayers, currentPlayer);
                hasStatistics = profile.PersonalRank >= MinimumRank;
            }

            return hasStatistics;
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
    }
}