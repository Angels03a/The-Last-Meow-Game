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
        /// Deja constancia de que una salida involuntaria no modificó la partida: el motor solo se toca con la salida
        /// voluntaria hasta que exista la ventana de reconexión de 15 s (C2).
        /// </summary>
        /// <param name="roomDeparture">Salida de sala ya aplicada, con su causa.</param>
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

        /// <summary>Crea un aviso OnMatchViewChanged por cada vista, con la vista de cada destinatario.</summary>
        /// <param name="viewDeliveries">Vistas ya armadas bajo el candado, cada una con su callback.</param>
        /// <returns>Los avisos, sin enviar, en el orden de las vistas.</returns>
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