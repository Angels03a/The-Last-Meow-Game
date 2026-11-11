using System;
using System.Collections.Generic;

using TLMG.Domain.Repositories;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Repositorio de amistades en memoria que imita las reglas de la base de datos: una fila por pareja y
    /// cambios condicionados por estado.
    /// </summary>
    internal sealed class FriendshipRepositoryStub : IFriendshipRepository
    {
        private readonly Dictionary<int, string> _nicknamesByUserId = new Dictionary<int, string>();

        private int _lastFriendshipId;

        internal List<StoredFriendship> Rows { get; } = new List<StoredFriendship>();

        internal bool ThrowOnAnyCall { get; set; }

        internal bool FailPendingLookup { get; set; }

        internal bool ForceDuplicateOnCreate { get; set; }

        internal bool ForceStateChangedOnWrite { get; set; }

        internal void AddUser(int userId, string nickname)
        {
            _nicknamesByUserId[userId] = nickname;
        }

        internal int Seed(int senderUserId, int receiverUserId, FriendshipState state)
        {
            _lastFriendshipId++;
            Rows.Add(new StoredFriendship
            {
                FriendshipId = _lastFriendshipId,
                SenderUserId = senderUserId,
                ReceiverUserId = receiverUserId,
                State = state
            });

            return _lastFriendshipId;
        }

        internal StoredFriendship Find(int friendshipId)
        {
            StoredFriendship found = Rows.Find(row => row.FriendshipId == friendshipId);

            return found;
        }

        public FriendListLookup FindAcceptedFriends(int userId)
        {
            CheckForSimulatedFailure();
            List<AcceptedFriend> friends = new List<AcceptedFriend>();

            foreach (StoredFriendship row in Rows)
            {
                bool isOfUser = row.SenderUserId == userId || row.ReceiverUserId == userId;

                if (isOfUser && row.State == FriendshipState.Accepted)
                {
                    int friendUserId = row.SenderUserId == userId ? row.ReceiverUserId : row.SenderUserId;
                    friends.Add(new AcceptedFriend
                    {
                        UserId = friendUserId,
                        Nickname = _nicknamesByUserId[friendUserId]
                    });
                }
            }

            return new FriendListLookup { WasFriendListLookupSuccessful = true, Friends = friends };
        }

        public FriendshipLookupStatus FindFriendshipStatus(int userId, int otherUserId)
        {
            CheckForSimulatedFailure();
            FriendshipRecordLookup lookup = FindFriendshipRecord(userId, otherUserId);
            FriendshipLookupStatus status = FriendshipLookupStatus.NotAccepted;

            if (lookup.Status == FriendLookupStatus.Found && lookup.Record.State == FriendshipState.Accepted)
            {
                status = FriendshipLookupStatus.Accepted;
            }

            return status;
        }

        public FriendCandidateLookup FindFriendCandidateByNickname(string nickname)
        {
            CheckForSimulatedFailure();
            FriendCandidateLookup lookup = new FriendCandidateLookup { Status = FriendLookupStatus.NotFound };

            foreach (KeyValuePair<int, string> user in _nicknamesByUserId)
            {
                if (string.Equals(user.Value, nickname, StringComparison.OrdinalIgnoreCase))
                {
                    lookup.Status = FriendLookupStatus.Found;
                    lookup.UserId = user.Key;
                    lookup.Nickname = user.Value;
                }
            }

            return lookup;
        }

        public FriendshipRecordLookup FindFriendshipRecord(int userId, int otherUserId)
        {
            CheckForSimulatedFailure();
            StoredFriendship row = Rows.Find(candidate =>
                (candidate.SenderUserId == userId && candidate.ReceiverUserId == otherUserId)
                || (candidate.SenderUserId == otherUserId && candidate.ReceiverUserId == userId));

            return CreateRecordLookup(row);
        }

        public FriendshipRecordLookup FindFriendshipRecordById(int friendshipId)
        {
            CheckForSimulatedFailure();

            return CreateRecordLookup(Find(friendshipId));
        }

        public PendingFriendRequestsLookup FindPendingFriendRequests(int userId)
        {
            CheckForSimulatedFailure();
            PendingFriendRequestsLookup lookup = new PendingFriendRequestsLookup
            {
                WasLookupSuccessful = !FailPendingLookup,
                Requests = new List<PendingFriendRequest>()
            };

            foreach (StoredFriendship row in Rows)
            {
                bool isIncoming = row.ReceiverUserId == userId;
                bool isOfUser = isIncoming || row.SenderUserId == userId;

                if (isOfUser && row.State == FriendshipState.Pending && !FailPendingLookup)
                {
                    int otherUserId = isIncoming ? row.SenderUserId : row.ReceiverUserId;
                    lookup.Requests.Add(new PendingFriendRequest
                    {
                        FriendshipId = row.FriendshipId,
                        OtherUserId = otherUserId,
                        OtherNickname = _nicknamesByUserId[otherUserId],
                        IsIncoming = isIncoming
                    });
                }
            }

            return lookup;
        }

        public FriendshipWriteResult CreateFriendRequest(int senderUserId, int receiverUserId)
        {
            CheckForSimulatedFailure();
            FriendshipWriteResult result = new FriendshipWriteResult { Status = FriendshipWriteStatus.DuplicatePair };
            bool pairExists = FindFriendshipRecord(senderUserId, receiverUserId).Status == FriendLookupStatus.Found;

            if (!pairExists && !ForceDuplicateOnCreate)
            {
                result.Status = FriendshipWriteStatus.Applied;
                result.FriendshipId = Seed(senderUserId, receiverUserId, FriendshipState.Pending);
            }

            return result;
        }

        public FriendshipWriteStatus ReactivateFriendRequest(int friendshipId, int senderUserId, int receiverUserId)
        {
            CheckForSimulatedFailure();
            StoredFriendship row = Find(friendshipId);
            FriendshipWriteStatus status = FriendshipWriteStatus.StateChanged;

            if (row != null && row.State == FriendshipState.Rejected && !ForceStateChangedOnWrite)
            {
                row.State = FriendshipState.Pending;
                row.SenderUserId = senderUserId;
                row.ReceiverUserId = receiverUserId;
                status = FriendshipWriteStatus.Applied;
            }

            return status;
        }

        public FriendshipWriteStatus AnswerFriendRequest(int friendshipId, int receiverUserId, FriendshipState newState)
        {
            CheckForSimulatedFailure();
            StoredFriendship row = Find(friendshipId);
            FriendshipWriteStatus status = FriendshipWriteStatus.StateChanged;

            if (row != null && row.ReceiverUserId == receiverUserId && row.State == FriendshipState.Pending
                && !ForceStateChangedOnWrite)
            {
                row.State = newState;
                status = FriendshipWriteStatus.Applied;
            }

            return status;
        }

        private void CheckForSimulatedFailure()
        {
            if (ThrowOnAnyCall)
            {
                throw new InvalidOperationException("Fallo simulado del repositorio de amistades.");
            }
        }

        private FriendshipRecordLookup CreateRecordLookup(StoredFriendship row)
        {
            FriendshipRecordLookup lookup = new FriendshipRecordLookup { Status = FriendLookupStatus.NotFound };

            if (row != null)
            {
                lookup.Status = FriendLookupStatus.Found;
                lookup.Record = new FriendshipRecord
                {
                    FriendshipId = row.FriendshipId,
                    SenderUserId = row.SenderUserId,
                    ReceiverUserId = row.ReceiverUserId,
                    SenderNickname = _nicknamesByUserId[row.SenderUserId],
                    ReceiverNickname = _nicknamesByUserId[row.ReceiverUserId],
                    State = row.State
                };
            }

            return lookup;
        }

        internal sealed class StoredFriendship
        {
            internal int FriendshipId { get; set; }

            internal int SenderUserId { get; set; }

            internal int ReceiverUserId { get; set; }

            internal FriendshipState State { get; set; }
        }
    }
}
