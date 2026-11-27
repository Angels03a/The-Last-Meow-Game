using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Moderation;

namespace TLMG.Services.Sessions
{

    public partial class SessionRegistry
    {
        private static readonly IReadOnlyList<IFriendsManagerCallback> _noFriendsCallbacks =
            new List<IFriendsManagerCallback>().AsReadOnly();

        private static readonly TimeSpan _invitationRenotifyInterval = TimeSpan.FromSeconds(5);

        private readonly Dictionary<int, FriendsChannel> _friendsChannelsByUserId =
            new Dictionary<int, FriendsChannel>();
        private readonly Dictionary<string, int> _userIdsByFriendsChannelId = new Dictionary<string, int>();
        private readonly List<RoomInvitationRecord> _roomInvitations = new List<RoomInvitationRecord>();
        private int _lastInvitationId;

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

        public bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd)
        {
            sessionEnd = null;

            if (!string.IsNullOrEmpty(channelId))
            {
                lock (_sessionsLock)
                {
                    int userId;

                    if (_userIdsByFriendsChannelId.TryGetValue(channelId, out userId))
                    {
                        PlayerSession session = _sessionsByToken[_tokensByUserId[userId]];
                        sessionEnd = new SessionEnd(userId, session.Nickname,
                            LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotInRoom, userId),
                            _friendsChannelsByUserId[userId].Callback, FindPresenceRecipientsOfUser(userId));
                        RemoveFriendsChannelOfUser(userId);
                    }
                }
            }

            bool wasEnded = sessionEnd != null;

            if (wasEnded)
            {
                _log.InfoFormat(
                    "Canal de amigos desvinculado para el jugador {0}.",
                    sessionEnd.UserId);
                LogDepartureOfEndedSession(sessionEnd);
            }

            return wasEnded;
        }

        public RoomInvitationCreation CreateRoomInvitation(string sessionToken, int friendUserId)
        {
            RoomInvitationCreation roomInvitationCreation =
                RoomInvitationCreation.CreateRejected(FriendsChangeStatus.InvalidSession);

            if (_blockPolicy == null)
            {
                _log.Error("La política de bloqueos no está configurada al crear una invitación.");
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.DataFailure);
            }
            else if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_blockPolicy.SynchronizationRoot)
                {
                    BlockListLookup blocks = FindSessionBlocks(sessionToken);

                    lock (_sessionsLock)
                    {
                        roomInvitationCreation = CreateRoomInvitationInLock(sessionToken, friendUserId, blocks);
                    }
                }
            }

            return roomInvitationCreation;
        }

        public RoomInvitationAcceptance AcceptRoomInvitation(string sessionToken, int invitationId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                FriendsChangeStatus.InvalidSession,
                LobbyMembershipChange.NoUserId);

            if (_blockPolicy == null)
            {
                _log.Error("La política de bloqueos no está configurada al aceptar una invitación.");
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                    FriendsChangeStatus.DataFailure, LobbyMembershipChange.NoUserId);
            }
            else if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_blockPolicy.SynchronizationRoot)
                {
                    BlockListLookup blocks = FindSessionBlocks(sessionToken);

                    lock (_sessionsLock)
                    {
                        roomInvitationAcceptance = AcceptRoomInvitationInLock(sessionToken, invitationId, blocks);
                    }
                }
            }

            return roomInvitationAcceptance;
        }

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
                TrackLinkInLock(playerSession, HeartbeatChannel.Friends, channelId);
                friendsSubscription = FriendsSubscription.CreateApplied(
                    playerSession.UserId,
                    playerSession.Nickname,
                    FindOnlineUserIds(friendUserIds),
                    presenceRecipients);
            }

            return friendsSubscription;
        }

        private RoomInvitationCreation CreateRoomInvitationInLock(
            string sessionToken, int friendUserId, BlockListLookup blocks)
        {
            RoomInvitationCreation roomInvitationCreation;
            PlayerSession inviterSession;
            FriendsChannel inviteeChannel;
            int roomId;

            if (!_sessionsByToken.TryGetValue(sessionToken, out inviterSession))
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.InvalidSession);
            }
            else if (!blocks.WasLookupSuccessful)
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.DataFailure);
            }
            else if (!BlockPolicy.CanInteract(blocks, inviterSession.UserId, friendUserId))
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.CannotInvite);
            }
            else if (!_roomIdsByUserId.TryGetValue(inviterSession.UserId, out roomId))
            {
                roomInvitationCreation = RoomInvitationCreation.CreateRejected(FriendsChangeStatus.NotInRoom);
            }
            else if (_startsByRoomId[roomId].MatchId.HasValue
                || IsExcludedFromRoomInLock(friendUserId, roomId))
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

        private RoomInvitationAcceptance AcceptRoomInvitationInLock(
            string sessionToken, int invitationId, BlockListLookup blocks)
        {
            RoomInvitationAcceptance roomInvitationAcceptance;
            PlayerSession playerSession;

            if (!_sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                    FriendsChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);
            }
            else if (!blocks.WasLookupSuccessful)
            {
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                    FriendsChangeStatus.DataFailure, playerSession.UserId);
            }
            else
            {
                RoomInvitationRecord invitation = _roomInvitations.Find(IsMatchingInvitation);

                bool IsMatchingInvitation(RoomInvitationRecord pendingInvitation)
                {
                    bool isMatching = pendingInvitation.InvitationId == invitationId
                        && pendingInvitation.InviteeUserId == playerSession.UserId;

                    return isMatching;
                }

                if (invitation == null)
                {
                    roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(
                        FriendsChangeStatus.InvitationNotValid, playerSession.UserId);
                }
                else
                {
                    _roomInvitations.Remove(invitation);
                    roomInvitationAcceptance =
                        AuthorizeInvitedRoomInLock(playerSession.UserId, invitation, blocks);
                }
            }

            return roomInvitationAcceptance;
        }

        private RoomInvitationAcceptance AuthorizeInvitedRoomInLock(
            int userId, RoomInvitationRecord invitation, BlockListLookup blocks)
        {
            FriendsChangeStatus status = ValidateInvitationRoomInLock(userId, invitation, blocks);
            RoomInvitationAcceptance roomInvitationAcceptance;

            if (status == FriendsChangeStatus.Applied
                && IssueRoomEntryAuthorizationInLock(userId, invitation.RoomId))
            {
                roomInvitationAcceptance = RoomInvitationAcceptance.CreateApplied(userId, invitation.RoomId);
            }
            else
            {
                if (status == FriendsChangeStatus.Applied)
                {
                    status = FriendsChangeStatus.RoomAccessDenied;
                }

                roomInvitationAcceptance = RoomInvitationAcceptance.CreateRejected(status, userId);
            }

            return roomInvitationAcceptance;
        }

        private FriendsChangeStatus ValidateInvitationRoomInLock(
            int userId, RoomInvitationRecord invitation, BlockListLookup blocks)
        {
            FriendsChangeStatus status = FriendsChangeStatus.InvitationNotValid;
            int roomId = invitation.RoomId;
            int inviterRoomId;
            List<LobbyMember> members;

            if (_roomIdsByUserId.ContainsKey(userId))
            {
                status = FriendsChangeStatus.AlreadyInRoom;
            }
            else if (_roomIdsByUserId.TryGetValue(invitation.InviterUserId, out inviterRoomId)
                && inviterRoomId == roomId && _membersByRoomId.TryGetValue(roomId, out members))
            {
                if (!blocks.WasLookupSuccessful)
                {
                    status = FriendsChangeStatus.DataFailure;
                }
                else if (!IsRoomCompatibleWithBlocksInLock(roomId, blocks)
                    || IsExcludedFromRoomInLock(userId, roomId))
                {
                    status = FriendsChangeStatus.RoomAccessDenied;
                }
                else if (_startsByRoomId[roomId].MatchId.HasValue)
                {
                    status = FriendsChangeStatus.MatchAlreadyStarted;
                }
                else if (members.Count >= ValidationRules.MaximumPlayersPerMatchCount)
                {
                    status = FriendsChangeStatus.RoomFull;
                }
                else
                {
                    status = FriendsChangeStatus.Applied;
                }
            }

            return status;
        }

        private RoomInvitationCreation CreateOrFindInvitation(
            PlayerSession inviterSession,
            int friendUserId,
            int roomId,
            IFriendsManagerCallback inviteeCallback)
        {
            RoomInvitationRecord invitation = _roomInvitations.Find(IsMatchingInvitation);

            bool IsMatchingInvitation(RoomInvitationRecord pending)
            {
                bool isMatching = pending.InviterUserId == inviterSession.UserId
                    && pending.InviteeUserId == friendUserId && pending.RoomId == roomId;

                return isMatching;
            }
            DateTime nowUtc = _readUtcNow();
            bool shouldNotifyInvitee = invitation == null
                || nowUtc - invitation.LastNotifiedUtc >= _invitationRenotifyInterval;

            if (invitation == null)
            {
                _lastInvitationId++;
                invitation = new RoomInvitationRecord(
                    _lastInvitationId, inviterSession.UserId, friendUserId, roomId, nowUtc);
                _roomInvitations.Add(invitation);
            }
            else if (shouldNotifyInvitee)
            {
                invitation.LastNotifiedUtc = nowUtc;
            }

            RoomInvitationCreation roomInvitationCreation = RoomInvitationCreation.CreateApplied(
                invitation.InvitationId,
                inviterSession.Nickname,
                shouldNotifyInvitee,
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