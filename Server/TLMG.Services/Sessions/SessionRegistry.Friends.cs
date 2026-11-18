using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    // Parte del registro que guarda los canales de amigos (quien tiene uno vivo está en línea) y las invitaciones
    // a sala. Todo método que termina en InLock, y los ayudantes que llama, se invoca únicamente con _sessionsLock
    // tomado y solo toca memoria: la amistad se comprueba en la base de datos antes de llamar a este registro.
    public partial class SessionRegistry
    {
        private static readonly IReadOnlyList<IFriendsManagerCallback> _noFriendsCallbacks =
            new List<IFriendsManagerCallback>().AsReadOnly();

        private readonly Dictionary<int, FriendsChannel> _friendsChannelsByUserId =
            new Dictionary<int, FriendsChannel>();
        private readonly Dictionary<string, int> _userIdsByFriendsChannelId = new Dictionary<string, int>();
        private readonly List<RoomInvitationRecord> _roomInvitations = new List<RoomInvitationRecord>();
        private int _lastInvitationId;

        /// <inheritdoc />
        public FriendsSubscription SubscribeFriendsChannel(
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback,
            IReadOnlyList<int> friendUserIds)
        {
            FriendsSubscription friendsSubscription =
                FriendsSubscription.CreateRejected(FriendsChangeStatus.InvalidSession);

            if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(channelId))
            {
                lock (_sessionsLock)
                {
                    friendsSubscription =
                        SubscribeFriendsChannelInLock(sessionToken, channelId, friendsCallback, friendUserIds);
                }
            }

            return friendsSubscription;
        }

        /// <inheritdoc />
        public bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd)
        {
            sessionEnd = null;

            if (!string.IsNullOrEmpty(channelId))
            {
                lock (_sessionsLock)
                {
                    int userId;

                    // Es seguro: al reemplazar la sesión, el canal viejo ya no está en este mapa.
                    if (_userIdsByFriendsChannelId.TryGetValue(channelId, out userId))
                    {
                        sessionEnd = EndSessionInLock(
                            _sessionsByToken[_tokensByUserId[userId]],
                            RoomExitCause.SessionEnded);
                    }
                }
            }

            bool wasEnded = sessionEnd != null;

            if (wasEnded)
            {
                _log.InfoFormat(
                    "Sesión cerrada para el jugador {0} porque su canal de amigos terminó.",
                    sessionEnd.UserId);
                LogDepartureOfEndedSession(sessionEnd);
            }

            return wasEnded;
        }

        /// <inheritdoc />
        public RoomInvitationCreation CreateRoomInvitation(string sessionToken, int friendUserId)
        {
            RoomInvitationCreation roomInvitationCreation =
                RoomInvitationCreation.CreateRejected(FriendsChangeStatus.InvalidSession);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    roomInvitationCreation = CreateRoomInvitationInLock(sessionToken, friendUserId);
                }
            }

            return roomInvitationCreation;
        }

        /// <inheritdoc />
        public RoomInvitationAcceptance AcceptRoomInvitation(string sessionToken, int invitationId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                FriendsChangeStatus.InvalidSession,
                LobbyMembershipChange.NoUserId);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    roomInvitationAcceptance = AcceptRoomInvitationInLock(sessionToken, invitationId);
                }
            }

            return roomInvitationAcceptance;
        }

        /// <inheritdoc />
        public IFriendsManagerCallback FindFriendsCallbackOfUser(int userId)
        {
            IFriendsManagerCallback friendsCallback = null;

            lock (_sessionsLock)
            {
                FriendsChannel friendsChannel;

                if (_friendsChannelsByUserId.TryGetValue(userId, out friendsChannel))
                {
                    friendsCallback = friendsChannel.Callback;
                }
            }

            return friendsCallback;
        }

        /// <inheritdoc />
        public FriendsAddition AddFriendshipToOnlineChannels(int firstUserId, int secondUserId)
        {
            FriendsAddition friendsAddition;

            lock (_sessionsLock)
            {
                FriendsChannel firstChannel = AddFriendToChannelInLock(firstUserId, secondUserId);
                FriendsChannel secondChannel = AddFriendToChannelInLock(secondUserId, firstUserId);

                friendsAddition = new FriendsAddition(
                    firstChannel != null,
                    FindCallbackOfChannel(firstChannel),
                    secondChannel != null,
                    FindCallbackOfChannel(secondChannel));
            }

            return friendsAddition;
        }

        /// <summary>
        /// Reemplaza el canal de amigos del jugador por otro con el amigo nuevo en su lista; requiere _sessionsLock.
        /// </summary>
        /// <param name="ownerUserId">Jugador dueño del canal.</param>
        /// <param name="newFriendUserId">Amigo que se suma a la lista guardada.</param>
        /// <returns>El canal actualizado, o null si el jugador no tiene canal de amigos.</returns>
        private FriendsChannel AddFriendToChannelInLock(int ownerUserId, int newFriendUserId)
        {
            FriendsChannel updatedChannel = null;
            FriendsChannel currentChannel;

            if (_friendsChannelsByUserId.TryGetValue(ownerUserId, out currentChannel))
            {
                List<int> friendUserIds = new List<int>(currentChannel.FriendUserIds);

                if (!friendUserIds.Contains(newFriendUserId))
                {
                    friendUserIds.Add(newFriendUserId);
                }

                updatedChannel = new FriendsChannel(
                    currentChannel.UserId,
                    currentChannel.ChannelId,
                    currentChannel.Callback,
                    friendUserIds.AsReadOnly());
                _friendsChannelsByUserId[ownerUserId] = updatedChannel;
            }

            return updatedChannel;
        }

        private static IFriendsManagerCallback FindCallbackOfChannel(FriendsChannel friendsChannel)
        {
            IFriendsManagerCallback friendsCallback = null;

            if (friendsChannel != null)
            {
                friendsCallback = friendsChannel.Callback;
            }

            return friendsCallback;
        }

        private FriendsSubscription SubscribeFriendsChannelInLock(
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback,
            IReadOnlyList<int> friendUserIds)
        {
            FriendsSubscription friendsSubscription;
            PlayerSession playerSession;

            if (!_sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                friendsSubscription = FriendsSubscription.CreateRejected(FriendsChangeStatus.InvalidSession);
            }
            else if (IsFriendsChannelOfAnotherUser(channelId, playerSession.UserId))
            {
                friendsSubscription = FriendsSubscription.CreateRejected(FriendsChangeStatus.InvalidSession);
            }
            else
            {
                // El último canal gana: si la sesión ya tenía uno, se reemplaza y no cambia su presencia.
                IReadOnlyList<IFriendsManagerCallback> presenceRecipients = _noFriendsCallbacks;

                if (!_friendsChannelsByUserId.ContainsKey(playerSession.UserId))
                {
                    presenceRecipients = FindOnlineCallbacks(friendUserIds);
                }

                RemoveFriendsChannelOfUser(playerSession.UserId);
                _friendsChannelsByUserId[playerSession.UserId] = new FriendsChannel(
                    playerSession.UserId,
                    channelId,
                    friendsCallback,
                    new List<int>(friendUserIds).AsReadOnly());
                _userIdsByFriendsChannelId[channelId] = playerSession.UserId;
                friendsSubscription = FriendsSubscription.CreateApplied(
                    playerSession.UserId,
                    playerSession.Nickname,
                    FindOnlineUserIds(friendUserIds),
                    presenceRecipients);
            }

            return friendsSubscription;
        }

        private RoomInvitationCreation CreateRoomInvitationInLock(string sessionToken, int friendUserId)
        {
            RoomInvitationCreation roomInvitationCreation;
            PlayerSession inviterSession;
            FriendsChannel inviteeChannel;
            int roomId;

            if (!_sessionsByToken.TryGetValue(sessionToken, out inviterSession))
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.InvalidSession);
            }
            else if (!_roomIdsByUserId.TryGetValue(inviterSession.UserId, out roomId))
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.NotInRoom);
            }
            else if (_startsByRoomId[roomId].MatchId.HasValue)
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.CannotInvite);
            }
            else if (_membersByRoomId[roomId].Count >= ValidationRules.MaximumPlayersPerMatchCount)
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.RoomFull);
            }
            else if (!TryFindInvitableChannel(inviterSession.UserId, friendUserId, out inviteeChannel))
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.CannotInvite);
            }
            else
            {
                roomInvitationCreation =
                    CreateOrFindInvitation(inviterSession, friendUserId, roomId, inviteeChannel.Callback);
            }

            return roomInvitationCreation;
        }

        private RoomInvitationAcceptance AcceptRoomInvitationInLock(string sessionToken, int invitationId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance;
            PlayerSession playerSession;
            RoomInvitationRecord invitation = null;

            if (_sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                invitation = _roomInvitations.Find(pending =>
                    pending.InvitationId == invitationId && pending.InviteeUserId == playerSession.UserId);
            }

            if (playerSession == null)
            {
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                    FriendsChangeStatus.InvalidSession,
                    LobbyMembershipChange.NoUserId);
            }
            else if (invitation == null)
            {
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                    FriendsChangeStatus.InvitationNotValid,
                    playerSession.UserId);
            }
            else
            {
                _roomInvitations.Remove(invitation);
                roomInvitationAcceptance =
                    AuthorizeInvitedRoomInLock(playerSession.UserId, invitation.RoomId);
            }

            return roomInvitationAcceptance;
        }

        private RoomInvitationAcceptance AuthorizeInvitedRoomInLock(int userId, int roomId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance;

            if (IssueRoomEntryAuthorizationInLock(userId, roomId))
            {
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateApplied(userId, roomId);
            }
            else
            {
                roomInvitationAcceptance =
                    RoomInvitationAcceptance.CreateRejected(FriendsChangeStatus.InvitationNotValid, userId);
            }

            return roomInvitationAcceptance;
        }

        // Una invitación repetida al mismo jugador para la misma sala no crea otra: se devuelve la que ya existe.
        private RoomInvitationCreation CreateOrFindInvitation(
            PlayerSession inviterSession,
            int friendUserId,
            int roomId,
            IFriendsManagerCallback inviteeCallback)
        {
            RoomInvitationRecord invitation = _roomInvitations.Find(pending =>
                pending.InviteeUserId == friendUserId && pending.RoomId == roomId);
            bool isNewInvitation = invitation == null;

            if (isNewInvitation)
            {
                _lastInvitationId++;
                invitation = new RoomInvitationRecord(_lastInvitationId, inviterSession.UserId, friendUserId, roomId);
                _roomInvitations.Add(invitation);
            }

            RoomInvitationCreation roomInvitationCreation = RoomInvitationCreation.CreateApplied(
                invitation.InvitationId,
                inviterSession.Nickname,
                isNewInvitation,
                inviteeCallback);

            return roomInvitationCreation;
        }

        private bool TryFindInvitableChannel(int inviterUserId, int friendUserId, out FriendsChannel inviteeChannel)
        {
            bool isInvitable = false;
            inviteeChannel = null;

            if (friendUserId != inviterUserId && !_roomIdsByUserId.ContainsKey(friendUserId))
            {
                isInvitable = _friendsChannelsByUserId.TryGetValue(friendUserId, out inviteeChannel);
            }

            return isInvitable;
        }

        private bool IsFriendsChannelOfAnotherUser(string channelId, int userId)
        {
            int boundUserId;
            bool isOfAnotherUser = _userIdsByFriendsChannelId.TryGetValue(channelId, out boundUserId)
                && boundUserId != userId;

            return isOfAnotherUser;
        }

        private IReadOnlyList<int> FindOnlineUserIds(IReadOnlyList<int> userIds)
        {
            List<int> onlineUserIds = new List<int>();

            foreach (int userId in userIds)
            {
                if (_friendsChannelsByUserId.ContainsKey(userId))
                {
                    onlineUserIds.Add(userId);
                }
            }

            IReadOnlyList<int> readOnlyUserIds = onlineUserIds.AsReadOnly();

            return readOnlyUserIds;
        }

        private IReadOnlyList<IFriendsManagerCallback> FindOnlineCallbacks(IReadOnlyList<int> userIds)
        {
            List<IFriendsManagerCallback> callbacks = new List<IFriendsManagerCallback>();
            FriendsChannel friendsChannel;

            foreach (int userId in userIds)
            {
                if (_friendsChannelsByUserId.TryGetValue(userId, out friendsChannel))
                {
                    callbacks.Add(friendsChannel.Callback);
                }
            }

            IReadOnlyList<IFriendsManagerCallback> readOnlyCallbacks = callbacks.AsReadOnly();

            return readOnlyCallbacks;
        }

        private IReadOnlyList<IFriendsManagerCallback> FindPresenceRecipientsOfUser(int userId)
        {
            IReadOnlyList<IFriendsManagerCallback> presenceRecipients = _noFriendsCallbacks;
            FriendsChannel friendsChannel;

            if (_friendsChannelsByUserId.TryGetValue(userId, out friendsChannel))
            {
                presenceRecipients = FindOnlineCallbacks(friendsChannel.FriendUserIds);
            }

            return presenceRecipients;
        }

        private void RemoveFriendsChannelOfUser(int userId)
        {
            FriendsChannel friendsChannel;

            if (_friendsChannelsByUserId.TryGetValue(userId, out friendsChannel))
            {
                _friendsChannelsByUserId.Remove(userId);
                _userIdsByFriendsChannelId.Remove(friendsChannel.ChannelId);
            }
        }

        private void RemoveInvitationsOfRoom(int roomId)
        {
            _roomInvitations.RemoveAll(pending => pending.RoomId == roomId);
        }

        private void RemoveInvitationsOfUser(int userId)
        {
            _roomInvitations.RemoveAll(pending => pending.InviterUserId == userId || pending.InviteeUserId == userId);
        }
    }
}