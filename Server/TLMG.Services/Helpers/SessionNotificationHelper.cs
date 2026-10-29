using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Arma los avisos de presencia y de salida de sala que siguen a un cambio de sesión; no guarda estado ni
    /// notifica: devuelve invocaciones listas para que quien las recibe las entregue fuera de cualquier candado.
    /// </summary>
    public static class SessionNotificationHelper
    {
        /// <summary>Arma el aviso de presencia de un jugador para cada amigo en línea.</summary>
        /// <param name="recipientCallbacks">Callbacks de los amigos en línea a quienes avisar.</param>
        /// <param name="userId">Id de la cuenta del jugador cuya presencia cambió.</param>
        /// <param name="nickname">Nickname del jugador.</param>
        /// <param name="isOnline">true si el jugador pasó a estar en línea; false si dejó de estarlo.</param>
        /// <returns>Una invocación por amigo; vacía si no hay a quién avisar. Nunca es null.</returns>
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

        /// <summary>Arma los avisos que siguen a terminar una sesión: la sala que deja y su presencia.</summary>
        /// <param name="sessionEnd">Lo que devolvió el registro al terminar la sesión; puede ser null.</param>
        /// <returns>Las invocaciones para los demás jugadores; vacía si sessionEnd es null. Nunca es null.</returns>
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