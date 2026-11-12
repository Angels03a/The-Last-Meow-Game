using System;
using System.Collections.Generic;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Parte del manager que conserva las solicitudes de amistad pendientes y las envía, acepta y rechaza. Como la
    /// lista de amigos, el estado vive aquí aunque ninguna pantalla esté suscrita: las pendientes llegan justo
    /// después del login, antes de que exista FriendsScreen.
    /// </summary>
    internal sealed partial class ServerConnectionManager : IFriendsConnection
    {
        private List<FriendRequest> _pendingFriendRequests;

        /// <inheritdoc />
        public IReadOnlyList<FriendRequest> PendingFriendRequests => _pendingFriendRequests;

        /// <inheritdoc />
        public bool HasCurrentRoom => CurrentRoom != null;

        /// <inheritdoc />
        public bool SendFriendRequest(string friendNickname)
        {
            bool wasSendFriendRequestSent = false;

            if (HasRegisteredSession)
            {
                wasSendFriendRequestSent = TrySend(
                    () => _friendsClient.SendFriendRequest(_sessionToken, friendNickname),
                    "enviar una solicitud de amistad");
            }

            return wasSendFriendRequestSent;
        }

        /// <inheritdoc />
        public bool AcceptFriendRequest(int friendRequestId)
        {
            bool wasAcceptFriendRequestSent = false;

            if (HasRegisteredSession)
            {
                wasAcceptFriendRequestSent = TrySend(
                    () => _friendsClient.AcceptFriendRequest(_sessionToken, friendRequestId),
                    "aceptar una solicitud de amistad");
            }

            return wasAcceptFriendRequestSent;
        }

        /// <inheritdoc />
        public bool DeclineFriendRequest(int friendRequestId)
        {
            bool wasDeclineFriendRequestSent = false;

            if (HasRegisteredSession)
            {
                wasDeclineFriendRequestSent = TrySend(
                    () => _friendsClient.DeclineFriendRequest(_sessionToken, friendRequestId),
                    "rechazar una solicitud de amistad");
            }

            return wasDeclineFriendRequestSent;
        }

        private void StorePendingFriendRequests(FriendRequest[] pendingFriendRequests)
        {
            _pendingFriendRequests = new List<FriendRequest>(pendingFriendRequests ?? Array.Empty<FriendRequest>());
        }

        private void AddPendingFriendRequest(FriendRequest friendRequest)
        {
            if (_pendingFriendRequests != null)
            {
                bool isAlreadyListed = _pendingFriendRequests.Exists(
                    listedRequest => listedRequest.FriendRequestId == friendRequest.FriendRequestId);

                if (!isAlreadyListed)
                {
                    _pendingFriendRequests.Add(friendRequest);
                }
            }
        }

        private void RemovePendingFriendRequest(int friendRequestId)
        {
            if (_pendingFriendRequests != null)
            {
                _pendingFriendRequests.RemoveAll(listedRequest => listedRequest.FriendRequestId == friendRequestId);
            }
        }

        private void RemovePendingFriendRequestsOfUser(int userId)
        {
            if (_pendingFriendRequests != null)
            {
                _pendingFriendRequests.RemoveAll(listedRequest => listedRequest.UserId == userId);
            }
        }

        private void AddFriendToList(Friend newFriend)
        {
            if (FriendList != null)
            {
                bool isAlreadyListed = false;

                foreach (Friend listedFriend in FriendList)
                {
                    isAlreadyListed = isAlreadyListed || listedFriend.UserId == newFriend.UserId;
                }

                if (!isAlreadyListed)
                {
                    List<Friend> updatedFriends = new List<Friend>(FriendList);
                    updatedFriends.Add(newFriend);
                    FriendList = updatedFriends;
                }
            }

            RemovePendingFriendRequestsOfUser(newFriend.UserId);
        }
    }
}
