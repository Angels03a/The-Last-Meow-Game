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
            IReadOnlyList<IFriendsManagerCallback> recipientCallbacks, Friend friend)
        {
            List<Action> notifications = new List<Action>();

            foreach (IFriendsManagerCallback recipientCallback in recipientCallbacks)
            {
                notifications.Add(NotifyPresence);

                void NotifyPresence()
                {
                    recipientCallback.OnFriendPresenceChanged(friend);
                }
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
                Friend friend = new Friend
                {
                    UserId = sessionEnd.UserId,
                    Nickname = sessionEnd.Nickname,
                    IsOnline = false
                };
                notifications.AddRange(CreatePresenceNotifications(sessionEnd.PresenceRecipientCallbacks, friend));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        public static IReadOnlyList<Action> CreateReplacedSessionNotifications(SessionEnd replacedSession)
        {
            IReadOnlyList<Action> notifications = CreateRevokedSessionNotifications(replacedSession);

            return notifications;
        }

        public static IReadOnlyList<Action> CreateRevokedSessionNotifications(SessionEnd sessionEnd)
        {
            List<Action> notifications = new List<Action>(CreateSessionEndNotifications(sessionEnd));
            ICommunicationObject friendsChannel = sessionEnd?.FriendsCallback as ICommunicationObject;

            if (friendsChannel != null)
            {
                notifications.Add(AbortFriendsChannel);
            }

            void AbortFriendsChannel()
            {
                friendsChannel.Abort();
                _log.InfoFormat("Se cerró el canal de amigos de la sesión revocada del jugador {0}.", sessionEnd.UserId);
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        internal static void LogMatchLeftUntouched(LobbyMembershipChange roomDeparture)
        {
            bool leftWithoutTouchingMatch = roomDeparture.LeftAuthorizedMatch
                && roomDeparture.ExitCause != RoomExitCause.Voluntary
                && roomDeparture.ExitCause != RoomExitCause.Sanctioned
                && roomDeparture.ExitCause != RoomExitCause.VotedOut;

            if (leftWithoutTouchingMatch)
            {
                _log.WarnFormat(
                    "El jugador {0} dejó una partida en curso por la causa {1}; el motor de partida no se modificó.",
                    roomDeparture.UserId, roomDeparture.ExitCause);
            }
        }

        internal static IReadOnlyList<Action> CreateMatchViewNotifications(IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            List<Action> notifications = new List<Action>();

            foreach (MatchViewDelivery viewDelivery in viewDeliveries)
            {
                notifications.Add(NotifyMatchView);

                void NotifyMatchView()
                {
                    viewDelivery.Callback.OnMatchViewChanged(viewDelivery.MatchView);
                }
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        internal static IReadOnlyList<Action> CreateRoomNotifications(LobbyMembershipChange lobbyMembershipChange)
        {
            List<Action> notifications = new List<Action>();

            if (lobbyMembershipChange.Room != null)
            {
                LobbyRoom lobbyRoom = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);

                foreach (ILobbyManagerCallback recipientCallback in lobbyMembershipChange.RecipientCallbacks)
                {
                    notifications.Add(NotifyRoom);

                    void NotifyRoom()
                    {
                        recipientCallback.OnLobbyRoomChanged(lobbyRoom);
                    }
                }
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }
    }
}
