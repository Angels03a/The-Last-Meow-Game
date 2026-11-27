using System.Collections.Generic;
using System.Collections.ObjectModel;

using log4net;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    internal static class FriendsListProjection
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FriendsListProjection));

        internal static bool PopulateFriends(IFriendsConnection connection, ObservableCollection<FriendEntry> entries)
        {
            IReadOnlyList<Friend> friends = connection.FriendList;
            bool hasSnapshot = friends != null;
            entries.Clear();

            if (hasSnapshot)
            {
                foreach (Friend friend in friends)
                {
                    if (friend == null)
                    {
                        hasSnapshot = false;
                    }
                    else
                    {
                        entries.Add(new FriendEntry
                        {
                            UserId = friend.UserId,
                            Nickname = DisplayNameHelper.Resolve(friend.UserId, friend.Nickname),
                            IsOnline = friend.IsOnline,
                            CanBeInvited = friend.IsOnline && connection.HasCurrentRoom
                        });
                    }
                }

                if (!hasSnapshot)
                {
                    entries.Clear();
                    _log.Warn("The friends snapshot contains an invalid entry.");
                }
            }

            return hasSnapshot;
        }

        internal static bool PopulatePending(IFriendsConnection connection,
            ObservableCollection<FriendRequestEntry> entries)
        {
            IReadOnlyList<FriendRequest> requests = connection.PendingFriendRequests;
            bool hasSnapshot = requests != null;
            entries.Clear();

            if (hasSnapshot)
            {
                foreach (FriendRequest request in requests)
                {
                    if (request == null)
                    {
                        hasSnapshot = false;
                    }
                    else
                    {
                        entries.Add(new FriendRequestEntry
                        {
                            FriendRequestId = request.FriendRequestId,
                            Nickname = DisplayNameHelper.Resolve(request.UserId, request.Nickname),
                            IsIncoming = request.IsIncoming
                        });
                    }
                }

                if (!hasSnapshot)
                {
                    entries.Clear();
                    _log.Warn("The pending-friendship snapshot contains an invalid entry.");
                }
            }

            return hasSnapshot;
        }
    }
}