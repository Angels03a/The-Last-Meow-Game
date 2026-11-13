using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    public static class SessionNotificationHelper
    {
        public static IReadOnlyList<Action> CreatePresenceNotifications(
            IReadOnlyList<IFriendsManagerCallback> recipientCallbacks,
            int userId,
            string nickname,
            bool isOnline)
        {
            List<Action> notifications = new List<Action>();
            Friend friend = new Friend { UserId = userId, Nickname = nickname, IsOnline = isOnline };

            foreach (IFriendsManagerCallback recipientCallback in recipientCallbacks)
            {
                notifications.Add(() => recipientCallback.OnFriendPresenceChanged(friend));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        public static IReadOnlyList<Action> CreateSessionEndNotifications(SessionEnd sessionEnd)
        {
            List<Action> notifications = new List<Action>();

            if (sessionEnd != null)
            {
                notifications.AddRange(CreateRoomNotifications(sessionEnd.RoomDeparture));
                notifications.AddRange(CreatePresenceNotifications(
                    sessionEnd.PresenceRecipientCallbacks,
                    sessionEnd.UserId,
                    sessionEnd.Nickname,
                    false));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private static IReadOnlyList<Action> CreateRoomNotifications(LobbyMembershipChange roomDeparture)
        {
            List<Action> notifications = new List<Action>();

            if (roomDeparture.Room != null)
            {
                LobbyRoom lobbyRoom = LobbyRoomHelper.ConvertToLobbyRoom(roomDeparture.Room);

                foreach (ILobbyManagerCallback recipientCallback in roomDeparture.RecipientCallbacks)
                {
                    notifications.Add(() => recipientCallback.OnLobbyRoomChanged(lobbyRoom));
                }
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }
    }
}