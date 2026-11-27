using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
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
                    bool playerExists = (from user in context.User where user.email == email select user.id_user).Any();
                    playerExistenceLookupStatus = ConvertToExistenceStatus(playerExists);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Database connection failed while checking email availability.", sqlException);
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.DataFailure;
            }
            catch (DataException dataException)
            {
                _log.Error("Data operation failed while checking email availability.", dataException);
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
                    bool playerExists = (from user in context.User where user.nickname == nickname select user.id_user).Any();
                    playerExistenceLookupStatus = ConvertToExistenceStatus(playerExists);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Database connection failed while checking nickname availability.", sqlException);
                playerExistenceLookupStatus = PlayerExistenceLookupStatus.DataFailure;
            }
            catch (DataException dataException)
            {
                _log.Error("Data operation failed while checking nickname availability.", dataException);
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
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Database connection failed while registering {0}.", nickname),
                    sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error(
                    string.Format("Data operation failed while registering {0}.", nickname),
                    dataException);
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
                    playerCredentials = (from user in context.User.AsNoTracking()
                                         where user.email == email
                                         select new PlayerCredentials
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
                _log.Error("Database connection failed while reading credentials.", sqlException);
                playerCredentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };
            }
            catch (DataException dataException)
            {
                _log.Error(
                    "Data operation failed while reading credentials.",
                    dataException);
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
                    User existingUser = (from user in context.User where user.id_user == userId select user).FirstOrDefault();

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
                    string.Format("Database connection failed while updating login time for player {0}.", userId),
                    sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error(
                    string.Format("Data operation failed while updating login time for player {0}.", userId),
                    dataException);
            }

            return wasLastLogInTimeUpdated;
        }

        public LeaderboardLookup FindTopRankedPlayers(int maximumPlayerCount, int? registeredUserId = null)
        {
            LeaderboardLookup leaderboardLookup = new LeaderboardLookup
            {
                Players = new List<RankedPlayer>()
            };

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    IQueryable<RankedPlayer> rankedPlayersQuery =
                        from player in context.Player.AsNoTracking()
                        join user in context.User.AsNoTracking() on player.id_user equals user.id_user
                        where user.state == ValidationRules.ActiveUserState
                        orderby player.total_points descending, player.points_reached_at_utc, player.id_player
                        select new RankedPlayer
                        {
                            Nickname = user.nickname,
                            ProfileImage = user.profile_image,
                            TotalPoints = player.total_points,
                            TotalWins = player.total_wins,
                            TotalGamesPlayed = player.total_games_played
                        };

                    LeaderboardLookup successfulLookup = new LeaderboardLookup
                    {
                        Players = rankedPlayersQuery.Take(maximumPlayerCount).ToList()
                    };

                    if (registeredUserId.HasValue)
                    {
                        PopulatePersonalStanding(context, successfulLookup, registeredUserId.Value);
                    }

                    transaction.Commit();
                    successfulLookup.WasLeaderboardLookupSuccessful = true;
                    leaderboardLookup = successfulLookup;
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Leaderboard database operation failed.", sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error("Leaderboard data could not be read.", dataException);
            }

            return leaderboardLookup;
        }

        private static void PopulatePersonalStanding(
            TLMG_DBEntities context, LeaderboardLookup leaderboardLookup, int registeredUserId)
        {
            IQueryable<Player> activePlayers = PlayerRankingQuery.FindActivePlayers(context);
            Player currentPlayer = (from player in activePlayers
                                    where player.id_user == registeredUserId
                                    select player).FirstOrDefault();

            if (currentPlayer != null)
            {
                leaderboardLookup.PersonalRank = PlayerRankingQuery.FindRank(activePlayers, currentPlayer);
                leaderboardLookup.PersonalPoints = currentPlayer.total_points;
            }
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
                    total_games_played = ValidationRules.InitialStatisticValue,
                    points_reached_at_utc = DateTime.UtcNow
                };

                context.Player.Add(newPlayer);
                context.SaveChanges();

                insertPlayerTransaction.Commit();
            }
        }
    }
}