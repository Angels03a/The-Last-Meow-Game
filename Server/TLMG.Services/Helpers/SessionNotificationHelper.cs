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

        /// <summary>
        /// Crea los avisos OnLobbyRoomChanged de un cambio de sala, uno por cada destinatario del cambio. Si el cambio
        /// no trae sala (rechazado o sala vacía), no hay avisos.
        /// </summary>
        /// <param name="lobbyMembershipChange">Cambio de la sala con la foto y los destinatarios.</param>
        /// <returns>Los avisos, sin enviar, en el orden de los destinatarios.</returns>
        internal static IReadOnlyList<Action> CreateRoomNotifications(LobbyMembershipChange lobbyMembershipChange)
        {
            List<Action> notifications = new List<Action>();

            if (lobbyMembershipChange.Room != null)
            {
                LobbyRoom lobbyRoom = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);

                foreach (ILobbyManagerCallback recipientCallback in lobbyMembershipChange.RecipientCallbacks)
                {
                    notifications.Add(() => recipientCallback.OnLobbyRoomChanged(lobbyRoom));
                }
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }
    }
}