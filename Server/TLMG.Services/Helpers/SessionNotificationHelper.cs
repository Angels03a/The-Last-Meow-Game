using System;
using System.Collections.Generic;
using System.ServiceModel;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    public static class SessionNotificationHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SessionNotificationHelper));

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
                if (sessionEnd.MatchViews != null)
                {
                    notifications.AddRange(CreateMatchViewNotifications(sessionEnd.MatchViews));
                }
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
        /// Arma los avisos de una sesión reemplazada por un nuevo inicio y, al final, el cierre inmediato de su canal
        /// de amigos para que el cliente viejo caiga en ConnectionLost. Se usa Abort y no Close porque no espera al
        /// cliente y no lanza excepciones; si el callback no es un canal (por ejemplo, un stub), no agrega nada.
        /// </summary>
        /// <param name="replacedSession">Lo que devolvió el registro al reemplazar la sesión; puede ser null.</param>
        /// <returns>
        /// Las invocaciones por entregar fuera de cualquier candado; vacía si no había sesión anterior.
        /// </returns>
        public static IReadOnlyList<Action> CreateReplacedSessionNotifications(SessionEnd replacedSession)
        {
            List<Action> notifications = new List<Action>(CreateSessionEndNotifications(replacedSession));
            ICommunicationObject replacedChannel = replacedSession?.FriendsCallback as ICommunicationObject;

            if (replacedChannel != null)
            {
                notifications.Add(() => AbortFriendsChannel(replacedChannel, replacedSession.UserId));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private static void AbortFriendsChannel(ICommunicationObject friendsChannel, int userId)
        {
            friendsChannel.Abort();
            _log.InfoFormat("Se cerró el canal de amigos de la sesión anterior del jugador {0}.", userId);
        }

        /// <summary>
        /// Records an involuntary room exit separately from an immediate voluntary match exit. Disconnection uses the
        /// 15-second reconnection window (C2).
        /// </summary>
        /// <param name="roomDeparture">Room exit already applied, including its cause.</param>
        internal static void LogMatchLeftUntouched(LobbyMembershipChange roomDeparture)
        {
            bool leftWithoutTouchingMatch = roomDeparture.LeftAuthorizedMatch
                && roomDeparture.ExitCause != RoomExitCause.Voluntary;

            if (leftWithoutTouchingMatch)
            {
                _log.WarnFormat(
                    "El jugador {0} dejó una partida en curso por la causa {1}; el motor de partida no se modificó.",
                    roomDeparture.UserId,
                    roomDeparture.ExitCause);
            }
        }

        /// <summary>Creates one OnMatchViewChanged notification per recipient with that player's view.</summary>
        /// <param name="viewDeliveries">Views built under the lock, each with its callback.</param>
        /// <returns>Unsent notifications in view order.</returns>
        internal static IReadOnlyList<Action> CreateMatchViewNotifications(
            IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            List<Action> notifications = new List<Action>();

            foreach (MatchViewDelivery viewDelivery in viewDeliveries)
            {
                notifications.Add(() => viewDelivery.Callback.OnMatchViewChanged(viewDelivery.MatchView));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        /// <summary>
        /// Creates one OnLobbyRoomChanged notification per recipient. A rejection or an empty room has no room snapshot
        /// and produces no notifications.
        /// </summary>
        /// <param name="lobbyMembershipChange">Room change with its snapshot and recipients.</param>
        /// <returns>Unsent notifications in recipient order.</returns>
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