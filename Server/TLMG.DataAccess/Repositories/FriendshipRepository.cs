using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación del acceso a amistades con Entity Framework 6.
    /// </summary>
    public class FriendshipRepository : IFriendshipRepository
    {
        private const string AcceptedFriendshipState = "FRIEND_ACCEPTED";

        private static readonly ILog _log = LogManager.GetLogger(typeof(FriendshipRepository));

        public FriendListLookup FindAcceptedFriends(int userId)
        {
            FriendListLookup friendListLookup;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    IQueryable<int> sentFriendIds = context.Friendship.AsNoTracking()
                        .Where(friendship => friendship.state == AcceptedFriendshipState
                            && friendship.id_user_sender == userId)
                        .Select(friendship => friendship.id_user_receiver);

                    IQueryable<int> receivedFriendIds = context.Friendship.AsNoTracking()
                        .Where(friendship => friendship.state == AcceptedFriendshipState
                            && friendship.id_user_receiver == userId)
                        .Select(friendship => friendship.id_user_sender);

                    IQueryable<int> friendIds = sentFriendIds.Union(receivedFriendIds);

                    IQueryable<AcceptedFriend> friendsQuery = from user in context.User.AsNoTracking()
                                                              where friendIds.Contains(user.id_user)
                                                              orderby user.nickname
                                                              select new AcceptedFriend
                                                              {
                                                                  UserId = user.id_user,
                                                                  Nickname = user.nickname
                                                              };

                    friendListLookup = new FriendListLookup
                    {
                        WasFriendListLookupSuccessful = true,
                        Friends = friendsQuery.ToList()
                    };
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format("Error de conexiónal consultar los amigos del jugador {0}.", userId), 
                    sqlException);
                friendListLookup = CreateFailedFriendListLookup();
            }
            catch (EntityException entityException)
            {
                _log.Error(string.Format(
                    "La base de datos falló al consultar los amigos del jugador {0}.", userId), entityException);
                friendListLookup = CreateFailedFriendListLookup();
            }

            return friendListLookup;
        }

        public FriendshipLookupStatus FindFriendshipStatus(int userId, int otherUserId)
        {
            FriendshipLookupStatus friendshipLookupStatus;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    bool areAcceptedFriends = context.Friendship.AsNoTracking()
                        .Where(friendship => friendship.state == AcceptedFriendshipState)
                        .Where(friendship => friendship.id_user_sender == userId
                            || friendship.id_user_sender == otherUserId)
                        .Where(friendship => friendship.id_user_receiver == userId
                            || friendship.id_user_receiver == otherUserId)
                        .Any();

                    if (areAcceptedFriends)
                    {
                        friendshipLookupStatus = FriendshipLookupStatus.Accepted;
                    }
                    else
                    {
                        friendshipLookupStatus = FriendshipLookupStatus.NotAccepted;
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format(
                    "Error de conexión al comprobar la amistad entre los jugadores {0} y {1}.",
                    userId,
                    otherUserId), sqlException);
                friendshipLookupStatus = FriendshipLookupStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error(string.Format(
                    "La base de datos falló al comprobar la amistad entre los jugadores {0} y {1}.",
                    userId,
                    otherUserId), entityException);
                friendshipLookupStatus = FriendshipLookupStatus.DataFailure;
            }

            return friendshipLookupStatus;
        }

        private static FriendListLookup CreateFailedFriendListLookup()
        {
            FriendListLookup friendListLookup = new FriendListLookup
            {
                WasFriendListLookupSuccessful = false,
                Friends = new List<AcceptedFriend>()
            };

            return friendListLookup;
        }
    }
}