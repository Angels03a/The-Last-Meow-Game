using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación del acceso a amistades con Entity Framework 6.
    /// </summary>
    public class FriendshipRepository : IFriendshipRepository
    {
        private const string AcceptedFriendshipState = "FRIEND_ACCEPTED";
        private const int UniqueConstraintViolationNumber = 2627;
        private const int UniqueIndexViolationNumber = 2601;
        private const int StateColumnLength = 30;
        private const int SingleAffectedRow = 1;

        // Cada UPDATE lleva todos sus valores como parámetros; la condición revisa el estado esperado.
        private const string AnswerFriendRequestSql = @"UPDATE dbo.Friendship
SET state = @newState
WHERE id_friendship = @friendshipId
  AND id_user_receiver = @receiverUserId
  AND state = @pendingState";

        private const string ReactivateFriendRequestSql = @"UPDATE dbo.Friendship
SET state = @pendingState,
    id_user_sender = @senderUserId,
    id_user_receiver = @receiverUserId,
    request_date = @requestDate
WHERE id_friendship = @friendshipId
  AND state = @rejectedState";

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

        /// <inheritdoc />
        public FriendCandidateLookup FindFriendCandidateByNickname(string nickname)
        {
            FriendCandidateLookup friendCandidateLookup = new FriendCandidateLookup
            {
                Status = FriendLookupStatus.NotFound
            };

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    AcceptedFriend candidate = context.User.AsNoTracking()
                        .Where(user => user.nickname == nickname)
                        .Select(user => new AcceptedFriend { UserId = user.id_user, Nickname = user.nickname })
                        .FirstOrDefault();

                    if (candidate != null)
                    {
                        friendCandidateLookup.Status = FriendLookupStatus.Found;
                        friendCandidateLookup.UserId = candidate.UserId;
                        friendCandidateLookup.Nickname = candidate.Nickname;
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al buscar a un jugador por nickname.", sqlException);
                friendCandidateLookup.Status = FriendLookupStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos falló al buscar a un jugador por nickname.", entityException);
                friendCandidateLookup.Status = FriendLookupStatus.DataFailure;
            }

            return friendCandidateLookup;
        }

        /// <inheritdoc />
        public FriendshipRecordLookup FindFriendshipRecord(int userId, int otherUserId)
        {
            FriendshipRecordLookup friendshipRecordLookup;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    FriendshipRow row = CreateFriendshipRowQuery(context)
                        .Where(candidateRow =>
                            (candidateRow.SenderUserId == userId && candidateRow.ReceiverUserId == otherUserId)
                            || (candidateRow.SenderUserId == otherUserId && candidateRow.ReceiverUserId == userId))
                        .FirstOrDefault();
                    friendshipRecordLookup = CreateRecordLookup(row);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format(
                    "Error de conexión al buscar la amistad entre los jugadores {0} y {1}.",
                    userId,
                    otherUserId), sqlException);
                friendshipRecordLookup = CreateFailedRecordLookup();
            }
            catch (EntityException entityException)
            {
                _log.Error(string.Format(
                    "La base de datos falló al buscar la amistad entre los jugadores {0} y {1}.",
                    userId,
                    otherUserId), entityException);
                friendshipRecordLookup = CreateFailedRecordLookup();
            }

            return friendshipRecordLookup;
        }

        /// <inheritdoc />
        public FriendshipRecordLookup FindFriendshipRecordById(int friendshipId)
        {
            FriendshipRecordLookup friendshipRecordLookup;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    FriendshipRow row = CreateFriendshipRowQuery(context)
                        .Where(candidateRow => candidateRow.FriendshipId == friendshipId)
                        .FirstOrDefault();
                    friendshipRecordLookup = CreateRecordLookup(row);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format(
                    "Error de conexión al buscar la solicitud de amistad {0}.", friendshipId), sqlException);
                friendshipRecordLookup = CreateFailedRecordLookup();
            }
            catch (EntityException entityException)
            {
                _log.Error(string.Format(
                    "La base de datos falló al buscar la solicitud de amistad {0}.", friendshipId), entityException);
                friendshipRecordLookup = CreateFailedRecordLookup();
            }

            return friendshipRecordLookup;
        }

        /// <inheritdoc />
        public PendingFriendRequestsLookup FindPendingFriendRequests(int userId)
        {
            PendingFriendRequestsLookup pendingFriendRequestsLookup;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    List<FriendshipRow> pendingRows = CreateFriendshipRowQuery(context)
                        .Where(row => row.State == ValidationRules.PendingFriendshipState
                            && (row.SenderUserId == userId || row.ReceiverUserId == userId))
                        .OrderBy(row => row.RequestDate)
                        .ToList();

                    pendingFriendRequestsLookup = new PendingFriendRequestsLookup
                    {
                        WasLookupSuccessful = true,
                        Requests = ConvertToPendingRequests(pendingRows, userId)
                    };
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format(
                    "Error de conexión al consultar las solicitudes pendientes del jugador {0}.", userId),
                    sqlException);
                pendingFriendRequestsLookup = CreateFailedPendingLookup();
            }
            catch (EntityException entityException)
            {
                _log.Error(string.Format(
                    "La base de datos falló al consultar las solicitudes pendientes del jugador {0}.", userId),
                    entityException);
                pendingFriendRequestsLookup = CreateFailedPendingLookup();
            }

            return pendingFriendRequestsLookup;
        }

        /// <inheritdoc />
        public FriendshipWriteResult CreateFriendRequest(int senderUserId, int receiverUserId)
        {
            FriendshipWriteResult friendshipWriteResult = new FriendshipWriteResult
            {
                Status = FriendshipWriteStatus.DataFailure
            };

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    Friendship friendship = new Friendship
                    {
                        id_user_sender = senderUserId,
                        id_user_receiver = receiverUserId,
                        state = ValidationRules.PendingFriendshipState,
                        request_date = DateTime.UtcNow
                    };
                    context.Friendship.Add(friendship);
                    context.SaveChanges();

                    friendshipWriteResult.Status = FriendshipWriteStatus.Applied;
                    friendshipWriteResult.FriendshipId = friendship.id_friendship;
                }
            }
            catch (DbUpdateException dbUpdateException) when (IsDuplicateKeyViolation(dbUpdateException))
            {
                _log.Warn(string.Format(
                    "La pareja de jugadores {0} y {1} ya tenía una solicitud de amistad.",
                    senderUserId,
                    receiverUserId), dbUpdateException);
                friendshipWriteResult.Status = FriendshipWriteStatus.DuplicatePair;
            }
            catch (DbUpdateException dbUpdateException)
            {
                _log.Error(string.Format(
                    "No se pudo guardar la solicitud de amistad del jugador {0} al jugador {1}.",
                    senderUserId,
                    receiverUserId), dbUpdateException);
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al guardar una solicitud de amistad.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos falló al guardar una solicitud de amistad.", entityException);
            }

            return friendshipWriteResult;
        }

        /// <inheritdoc />
        public FriendshipWriteStatus ReactivateFriendRequest(int friendshipId, int senderUserId, int receiverUserId)
        {
            SqlParameter[] parameters =
            {
                CreateStateParameter("@pendingState", ValidationRules.PendingFriendshipState),
                new SqlParameter("@senderUserId", SqlDbType.Int) { Value = senderUserId },
                new SqlParameter("@receiverUserId", SqlDbType.Int) { Value = receiverUserId },
                new SqlParameter("@requestDate", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
                new SqlParameter("@friendshipId", SqlDbType.Int) { Value = friendshipId },
                CreateStateParameter("@rejectedState", ValidationRules.RejectedFriendshipState)
            };

            FriendshipWriteStatus friendshipWriteStatus = ExecuteFriendshipUpdate(
                ReactivateFriendRequestSql,
                parameters,
                string.Format("reactivar la solicitud de amistad {0}", friendshipId));

            return friendshipWriteStatus;
        }

        /// <inheritdoc />
        public FriendshipWriteStatus AnswerFriendRequest(
            int friendshipId,
            int receiverUserId,
            FriendshipState newState)
        {
            SqlParameter[] parameters =
            {
                CreateStateParameter("@newState", ConvertToStateText(newState)),
                new SqlParameter("@friendshipId", SqlDbType.Int) { Value = friendshipId },
                new SqlParameter("@receiverUserId", SqlDbType.Int) { Value = receiverUserId },
                CreateStateParameter("@pendingState", ValidationRules.PendingFriendshipState)
            };

            FriendshipWriteStatus friendshipWriteStatus = ExecuteFriendshipUpdate(
                AnswerFriendRequestSql,
                parameters,
                string.Format("responder la solicitud de amistad {0}", friendshipId));

            return friendshipWriteStatus;
        }

        private static FriendshipWriteStatus ExecuteFriendshipUpdate(
            string sql,
            SqlParameter[] parameters,
            string operationDescription)
        {
            FriendshipWriteStatus friendshipWriteStatus;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    int affectedRows = context.Database.ExecuteSqlCommand(sql, parameters);

                    if (affectedRows == SingleAffectedRow)
                    {
                        friendshipWriteStatus = FriendshipWriteStatus.Applied;
                    }
                    else
                    {
                        friendshipWriteStatus = FriendshipWriteStatus.StateChanged;
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(string.Format("Error de conexión al {0}.", operationDescription), sqlException);
                friendshipWriteStatus = FriendshipWriteStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error(string.Format("La base de datos falló al {0}.", operationDescription), entityException);
                friendshipWriteStatus = FriendshipWriteStatus.DataFailure;
            }

            return friendshipWriteStatus;
        }

        private static IQueryable<FriendshipRow> CreateFriendshipRowQuery(TLMG_DBEntities context)
        {
            IQueryable<FriendshipRow> friendshipRows =
                from friendship in context.Friendship.AsNoTracking()
                join sender in context.User.AsNoTracking() on friendship.id_user_sender equals sender.id_user
                join receiver in context.User.AsNoTracking() on friendship.id_user_receiver equals receiver.id_user
                select new FriendshipRow
                {
                    FriendshipId = friendship.id_friendship,
                    SenderUserId = friendship.id_user_sender,
                    ReceiverUserId = friendship.id_user_receiver,
                    SenderNickname = sender.nickname,
                    ReceiverNickname = receiver.nickname,
                    State = friendship.state,
                    RequestDate = friendship.request_date
                };

            return friendshipRows;
        }

        private static FriendshipRecordLookup CreateRecordLookup(FriendshipRow row)
        {
            FriendshipRecordLookup friendshipRecordLookup = new FriendshipRecordLookup
            {
                Status = FriendLookupStatus.NotFound
            };

            if (row != null)
            {
                friendshipRecordLookup.Status = FriendLookupStatus.Found;
                friendshipRecordLookup.Record = new FriendshipRecord
                {
                    FriendshipId = row.FriendshipId,
                    SenderUserId = row.SenderUserId,
                    ReceiverUserId = row.ReceiverUserId,
                    SenderNickname = row.SenderNickname,
                    ReceiverNickname = row.ReceiverNickname,
                    State = ConvertToFriendshipState(row.State)
                };
            }

            return friendshipRecordLookup;
        }

        private static FriendshipRecordLookup CreateFailedRecordLookup()
        {
            FriendshipRecordLookup friendshipRecordLookup = new FriendshipRecordLookup
            {
                Status = FriendLookupStatus.DataFailure
            };

            return friendshipRecordLookup;
        }

        private static PendingFriendRequestsLookup CreateFailedPendingLookup()
        {
            PendingFriendRequestsLookup pendingFriendRequestsLookup = new PendingFriendRequestsLookup
            {
                WasLookupSuccessful = false,
                Requests = new List<PendingFriendRequest>()
            };

            return pendingFriendRequestsLookup;
        }

        private static List<PendingFriendRequest> ConvertToPendingRequests(List<FriendshipRow> rows, int userId)
        {
            List<PendingFriendRequest> pendingRequests = new List<PendingFriendRequest>();

            foreach (FriendshipRow row in rows)
            {
                bool isIncoming = row.ReceiverUserId == userId;

                pendingRequests.Add(new PendingFriendRequest
                {
                    FriendshipId = row.FriendshipId,
                    OtherUserId = isIncoming ? row.SenderUserId : row.ReceiverUserId,
                    OtherNickname = isIncoming ? row.SenderNickname : row.ReceiverNickname,
                    IsIncoming = isIncoming
                });
            }

            return pendingRequests;
        }

        private static SqlParameter CreateStateParameter(string parameterName, string stateText)
        {
            SqlParameter stateParameter = new SqlParameter(parameterName, SqlDbType.VarChar, StateColumnLength)
            {
                Value = stateText
            };

            return stateParameter;
        }

        private static string ConvertToStateText(FriendshipState friendshipState)
        {
            string stateText = ValidationRules.PendingFriendshipState;

            if (friendshipState == FriendshipState.Accepted)
            {
                stateText = ValidationRules.AcceptedFriendshipState;
            }
            else if (friendshipState == FriendshipState.Rejected)
            {
                stateText = ValidationRules.RejectedFriendshipState;
            }

            return stateText;
        }

        private static FriendshipState ConvertToFriendshipState(string stateText)
        {
            FriendshipState friendshipState = FriendshipState.Pending;

            if (stateText == ValidationRules.AcceptedFriendshipState)
            {
                friendshipState = FriendshipState.Accepted;
            }
            else if (stateText == ValidationRules.RejectedFriendshipState)
            {
                friendshipState = FriendshipState.Rejected;
            }

            return friendshipState;
        }

        private static bool IsDuplicateKeyViolation(Exception exception)
        {
            bool isDuplicateKey = false;
            Exception currentException = exception;

            while (currentException != null && !isDuplicateKey)
            {
                SqlException sqlException = currentException as SqlException;
                isDuplicateKey = sqlException != null
                    && (sqlException.Number == UniqueConstraintViolationNumber
                        || sqlException.Number == UniqueIndexViolationNumber);
                currentException = currentException.InnerException;
            }

            return isDuplicateKey;
        }

        private sealed class FriendshipRow
        {
            public int FriendshipId { get; set; }

            public int SenderUserId { get; set; }

            public int ReceiverUserId { get; set; }

            public string SenderNickname { get; set; }

            public string ReceiverNickname { get; set; }

            public string State { get; set; }

            public DateTime RequestDate { get; set; }
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