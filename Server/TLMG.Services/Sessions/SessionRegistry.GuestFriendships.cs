using System.Collections.Generic;

using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry : IGuestFriendshipRegistry, IFriendsSessionAccess
    {
        private const int NoIndex = -1;
        private const int GuestFriendshipIdSign = -1;

        private readonly List<GuestFriendship> _guestFriendships = new List<GuestFriendship>();
        private int _lastGuestFriendshipNumber;

        public bool TryFindGuestByNickname(string nickname, out int guestUserId, out string guestNickname)
        {
            bool wasFound = false;
            int guestNumber;
            guestUserId = ValidationRules.NoUserId;
            guestNickname = null;

            if (GuestIdentity.TryParseGuestNumber(nickname, out guestNumber))
            {
                lock (_sessionsLock)
                {
                    PlayerSession guestSession = FindSessionOfUserInLock(GuestIdentity.CreateUserId(guestNumber));

                    if (guestSession != null)
                    {
                        guestUserId = guestSession.UserId;
                        guestNickname = guestSession.Nickname;
                        wasFound = true;
                    }
                }
            }

            return wasFound;
        }

        public GuestFriendshipChange CreateGuestFriendRequest(int senderUserId, int guestUserId)
        {
            GuestFriendshipChange guestFriendshipChange;

            lock (_sessionsLock)
            {
                guestFriendshipChange = CreateGuestFriendRequestInLock(senderUserId, guestUserId);
            }

            return guestFriendshipChange;
        }

        public GuestFriendshipChange AnswerGuestFriendRequest(int requestId, int receiverUserId, bool isAccepted)
        {
            GuestFriendshipChange guestFriendshipChange;

            lock (_sessionsLock)
            {
                guestFriendshipChange = AnswerGuestFriendRequestInLock(requestId, receiverUserId, isAccepted);
            }

            return guestFriendshipChange;
        }

        public bool AreGuestFriends(int userId, int otherUserId)
        {
            bool areFriends;

            lock (_sessionsLock)
            {
                GuestFriendship friendship = FindGuestPairInLock(userId, otherUserId);
                areFriends = friendship != null && friendship.State == GuestFriendshipState.Accepted;
            }

            return areFriends;
        }

        public IReadOnlyList<GuestFriendship> FindGuestFriendships(int userId)
        {
            List<GuestFriendship> friendships = new List<GuestFriendship>();

            lock (_sessionsLock)
            {
                foreach (GuestFriendship friendship in _guestFriendships)
                {
                    if (friendship.SenderUserId == userId || friendship.ReceiverUserId == userId)
                    {
                        friendships.Add(friendship);
                    }
                }
            }

            IReadOnlyList<GuestFriendship> readOnlyFriendships = friendships.AsReadOnly();

            return readOnlyFriendships;
        }

        private PlayerSession FindSessionOfUserInLock(int userId)
        {
            PlayerSession playerSession = null;
            string sessionToken;

            if (_tokensByUserId.TryGetValue(userId, out sessionToken))
            {
                playerSession = _sessionsByToken[sessionToken];
            }

            return playerSession;
        }

        private GuestFriendship FindGuestPairInLock(int userId, int otherUserId)
        {
            GuestFriendship pairFriendship = null;

            foreach (GuestFriendship friendship in _guestFriendships)
            {
                bool isDirect = friendship.SenderUserId == userId && friendship.ReceiverUserId == otherUserId;
                bool isReverse = friendship.SenderUserId == otherUserId && friendship.ReceiverUserId == userId;

                if (isDirect || isReverse)
                {
                    pairFriendship = friendship;
                }
            }

            return pairFriendship;
        }

        private int FindGuestFriendshipIndexInLock(int requestId)
        {
            int foundIndex = NoIndex;

            for (int friendshipIndex = 0; friendshipIndex < _guestFriendships.Count; friendshipIndex++)
            {
                if (_guestFriendships[friendshipIndex].FriendshipId == requestId)
                {
                    foundIndex = friendshipIndex;
                }
            }

            return foundIndex;
        }

        private GuestFriendshipChange CreateGuestFriendRequestInLock(int senderUserId, int guestUserId)
        {
            GuestFriendshipChange guestFriendshipChange;
            bool isValidPair = !GuestIdentity.IsGuestUserId(senderUserId) && GuestIdentity.IsGuestUserId(guestUserId);
            PlayerSession senderSession = FindSessionOfUserInLock(senderUserId);
            PlayerSession guestSession = FindSessionOfUserInLock(guestUserId);
            GuestFriendship existingFriendship = FindGuestPairInLock(senderUserId, guestUserId);

            if (!isValidPair)
            {
                guestFriendshipChange = GuestFriendshipChange.CreateRejected(
                    GuestFriendshipChangeStatus.NotValid,
                    null);
            }
            else if (senderSession == null || guestSession == null)
            {
                guestFriendshipChange = GuestFriendshipChange.CreateRejected(
                    GuestFriendshipChangeStatus.GuestNotFound,
                    null);
            }
            else if (existingFriendship != null)
            {
                guestFriendshipChange = GuestFriendshipChange.CreateRejected(
                    GuestFriendshipChangeStatus.AlreadyExists,
                    existingFriendship);
            }
            else
            {
                _lastGuestFriendshipNumber++;
                GuestFriendship newFriendship = new GuestFriendship
                {
                    FriendshipId = _lastGuestFriendshipNumber * GuestFriendshipIdSign,
                    SenderUserId = senderUserId,
                    ReceiverUserId = guestUserId,
                    SenderNickname = senderSession.Nickname,
                    ReceiverNickname = guestSession.Nickname,
                    State = GuestFriendshipState.Pending
                };
                _guestFriendships.Add(newFriendship);
                guestFriendshipChange = GuestFriendshipChange.CreateApplied(newFriendship);
            }

            return guestFriendshipChange;
        }

        private GuestFriendshipChange AnswerGuestFriendRequestInLock(
            int requestId,
            int receiverUserId,
            bool isAccepted)
        {
            GuestFriendshipChange guestFriendshipChange;
            int friendshipIndex = FindGuestFriendshipIndexInLock(requestId);
            GuestFriendship pendingFriendship = null;

            if (friendshipIndex != NoIndex)
            {
                pendingFriendship = _guestFriendships[friendshipIndex];
            }

            bool isAnswerable = pendingFriendship != null
                && pendingFriendship.State == GuestFriendshipState.Pending
                && pendingFriendship.ReceiverUserId == receiverUserId;

            if (!isAnswerable)
            {
                guestFriendshipChange = GuestFriendshipChange.CreateRejected(
                    GuestFriendshipChangeStatus.NotValid,
                    null);
            }
            else if (isAccepted)
            {
                GuestFriendship acceptedFriendship = pendingFriendship.CreateAccepted();
                _guestFriendships[friendshipIndex] = acceptedFriendship;
                guestFriendshipChange = GuestFriendshipChange.CreateApplied(acceptedFriendship);
            }
            else
            {
                _guestFriendships.RemoveAt(friendshipIndex);
                guestFriendshipChange = GuestFriendshipChange.CreateApplied(pendingFriendship);
            }

            return guestFriendshipChange;
        }

        private void RemoveGuestFriendshipsOfUserInLock(int userId)
        {
            for (int friendshipIndex = _guestFriendships.Count - 1; friendshipIndex >= 0; friendshipIndex--)
            {
                GuestFriendship friendship = _guestFriendships[friendshipIndex];

                if (friendship.SenderUserId == userId || friendship.ReceiverUserId == userId)
                {
                    _guestFriendships.RemoveAt(friendshipIndex);
                }
            }
        }
    }
}