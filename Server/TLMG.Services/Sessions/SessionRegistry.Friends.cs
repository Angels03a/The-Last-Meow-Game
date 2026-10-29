using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    // Parte del registro que guarda los canales de amigos (quien tiene uno vivo está en línea) y las invitaciones
    // a sala. Todo método que termina en InLock, y los ayudantes que llama, se invoca únicamente con _sessionsLock
    // tomado y solo toca memoria: la amistad se comprueba en la base de datos antes de llamar a este registro.
    public partial class SessionRegistry
    {
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
                // El último canal gana: si la sesión ya tenía uno, se reemplaza.
                RemoveFriendsChannelOfUser(playerSession.UserId);
                _friendsChannelsByUserId[playerSession.UserId] =
                    new FriendsChannel(playerSession.UserId, channelId, friendsCallback);
                _userIdsByFriendsChannelId[channelId] = playerSession.UserId;
                friendsSubscription =
                    FriendsSubscription.CreateApplied(playerSession.UserId, FindOnlineUserIds(friendUserIds));
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
            else if (_membersByRoomId[roomId].Count >= MaximumPlayersPerRoom)
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