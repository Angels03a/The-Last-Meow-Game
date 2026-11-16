using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación del acceso a cuentas con Entity Framework 6.
    /// </summary>
    public class PlayerRepository : IPlayerRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerRepository));

        public PlayerExistenceLookupStatus FindEmailExistenceStatus(string email)
        {
            PlayerExistenceLookupStatus playerExistenceLookupStatus;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    bool playerExists = context.User.Any(user => user.email == email);
                    playerExistenceLookupStatus = ConvertToExistenceStatus(playerExists);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al comprobar el correo de una cuenta.", sqlException);
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos falló al comprobar el correo de una cuenta.", entityException);
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.DataFailure;
            }

            return playerExistenceLookupStatus;
        }

        public PlayerExistenceLookupStatus FindNicknameExistenceStatus(string nickname)
        {
            PlayerExistenceLookupStatus playerExistenceLookupStatus;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    bool playerExists = context.User.Any(user => user.nickname == nickname);
                    playerExistenceLookupStatus = ConvertToExistenceStatus(playerExists);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al comprobar el nickname de una cuenta.", sqlException);
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos falló al comprobar el nickname de una cuenta.", entityException);
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.DataFailure;
            }

            return playerExistenceLookupStatus;
        }

        public bool CreatePlayer(string nickname, string email, string passwordHash)
        {
            bool wasPlayerCreated = false;

            try
            {
                InsertPlayerWithInitialStatistics(nickname, email, passwordHash);
                wasPlayerCreated = true;
            }
            catch (DbUpdateException dbUpdateException)
            {
                _log.Error(
                    string.Format("Error al guardar la cuenta del jugador {0}.", nickname),
                    dbUpdateException);
            }
            catch (DbEntityValidationException dbEntityValidationException)
            {
                _log.Error(
                    string.Format("Datos inválidos para la base de datos del jugador {0}.", nickname),
                    dbEntityValidationException);
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format("La base de datos no respondió al registrar a {0}.", nickname),
                    entityException);
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error de conexión al registrar a {0}.", nickname),
                    sqlException);
            }
            catch (Exception exception)
            {
                _log.Error(
                    string.Format("Error inesperado al registrar a {0}.", nickname),
                    exception);
            }
            return wasPlayerCreated;
        }

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
                            Nickname = user.nickname,
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
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexiónal buscar las credenciales de una cuenta.", sqlException);
                playerCredentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    "La base de datos no respondió al buscar las credenciales de una cuenta.",
                    entityException);
                playerCredentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };
            }

            return playerCredentials;
        }

        public bool UpdateLastLogInTime(int userId, DateTime lastLogInTimeUtc)
        {
            bool wasLastLogInTimeUpdated = false;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    User existingUser = context.User.FirstOrDefault(user => user.id_user == userId);

                    if (existingUser != null)
                    {
                        existingUser.last_time = lastLogInTimeUtc;
                        context.SaveChanges();
                        wasLastLogInTimeUpdated = true;
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error de conexión al guardar el acceso del jugador {0}.", userId),
                    sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format("La base de datos falló al guardar el acceso del jugador {0}.", userId),
                    entityException);
            }

            return wasLastLogInTimeUpdated;
        }

        public LeaderboardLookup FindTopRankedPlayers(int maximumPlayerCount)
        {
            LeaderboardLookup leaderboardLookup;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    IQueryable<RankedPlayer> rankedPlayersQuery = from player in context.Player.AsNoTracking()
                                                                  join user in context.User.AsNoTracking()
                                                                      on player.id_user equals user.id_user
                                                                  where user.state == ValidationRules.ActiveUserState
                                                                  orderby player.total_points descending, user.nickname
                                                                  select new RankedPlayer
                                                                  {
                                                                      Nickname = user.nickname,
                                                                      ProfileImage = user.profile_image,
                                                                      TotalPoints = player.total_points,
                                                                      TotalWins = player.total_wins,
                                                                      TotalGamesPlayed = player.total_games_played
                                                                  };

                    leaderboardLookup = new LeaderboardLookup
                    {
                        WasLeaderboardLookupSuccessful = true,
                        Players = rankedPlayersQuery.Take(maximumPlayerCount).ToList()
                    };
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al consultar la tabla de posiciones.", sqlException);
                leaderboardLookup = CreateFailedLeaderboardLookup();
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos no respondió al consultar la tabla de posiciones.", entityException);
                leaderboardLookup = CreateFailedLeaderboardLookup();
            }

            return leaderboardLookup;
        }

        private static PlayerExistenceLookupStatus ConvertToExistenceStatus(bool playerExists)
        {
            PlayerExistenceLookupStatus playerExistenceLookupStatus = PlayerExistenceLookupStatus.NotFound;

            if (playerExists)
            {
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.Exists;
            }

            return playerExistenceLookupStatus;
        }

        private static LeaderboardLookup CreateFailedLeaderboardLookup()
        {
            LeaderboardLookup leaderboardLookup = new LeaderboardLookup
            {
                WasLeaderboardLookupSuccessful = false,
                Players = new List<RankedPlayer>()
            };

            return leaderboardLookup;
        }

        private static void InsertPlayerWithInitialStatistics(string nickname, string email, string passwordHash)
        {
            using (TLMG_DBEntities context = new TLMG_DBEntities())
            using (DbContextTransaction insertPlayerTransaction = context.Database.BeginTransaction())
            {
                User newUser = new User
                {
                    email = email,
                    nickname = nickname,
                    password_hash = passwordHash,
                    state = ValidationRules.ActiveUserState,
                    profile_image = ValidationRules.DefaultProfileImage,
                    last_time = DateTime.UtcNow
                };

                context.User.Add(newUser);
                context.SaveChanges();

                Player newPlayer = new Player
                {
                    id_user = newUser.id_user,
                    total_points = ValidationRules.InitialStatisticValue,
                    total_wins = ValidationRules.InitialStatisticValue,
                    total_games_played = ValidationRules.InitialStatisticValue
                };

                context.Player.Add(newPlayer);
                context.SaveChanges();

                insertPlayerTransaction.Commit();
            }
        }
    }
}