using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int EmptyCount = 0;

        private readonly Dictionary<int, List<LobbyMember>> _membersByRoomId =
            new Dictionary<int, List<LobbyMember>>();
        private readonly Dictionary<int, int> _roomIdsByUserId = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _userIdsByChannelId = new Dictionary<string, int>();
        private readonly Dictionary<int, HashSet<int>> _authorizedRoomIdsByUserId =
            new Dictionary<int, HashSet<int>>();
        private int _lastRoomId;

        public LobbyMembershipChange CreateRoom(LobbyCaller caller, ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.InvalidSession);

            if (caller.HasSessionAndChannel)
            {
                lock (_sessionsLock)
                {
                    lobbyMembershipChange = CreateRoomInLock(caller, lobbyCallback);
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange EnterRoom(LobbyCaller caller, int roomId, ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.InvalidSession);

            if (caller.HasSessionAndChannel)
            {
                lock (_sessionsLock)
                {
                    lobbyMembershipChange = EnterRoomInLock(caller, roomId, lobbyCallback);
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange LeaveRoom(string sessionToken)
        {
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    PlayerSession playerSession;

                    if (_sessionsByToken.TryGetValue(sessionToken, out playerSession))
                    {
                        lobbyMembershipChange = RemoveUserFromRoom(playerSession.UserId);
                    }
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange LeaveRoomByChannel(string channelId)
        {
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotInRoom, LobbyMembershipChange.NoUserId);

            if (!string.IsNullOrEmpty(channelId))
            {
                lock (_sessionsLock)
                {
                    int userId;

                    if (_userIdsByChannelId.TryGetValue(channelId, out userId))
                    {
                        lobbyMembershipChange = RemoveUserFromRoom(userId);
                    }
                }
            }

            return lobbyMembershipChange;
        }

        private static LobbyMembershipChange CreateRejectedWithoutUser(LobbyChangeStatus lobbyChangeStatus)
        {
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(lobbyChangeStatus, LobbyMembershipChange.NoUserId);

            return lobbyMembershipChange;
        }

        private bool IssueRoomEntryAuthorizationInLock(int userId, int roomId)
        {
            bool wasIssued = false;

            if (_tokensByUserId.ContainsKey(userId) && _membersByRoomId.ContainsKey(roomId)
                && !_startsByRoomId[roomId].MatchId.HasValue)
            {
                HashSet<int> authorizedRoomIds;

                if (!_authorizedRoomIdsByUserId.TryGetValue(userId, out authorizedRoomIds))
                {
                    authorizedRoomIds = new HashSet<int>();
                    _authorizedRoomIdsByUserId[userId] = authorizedRoomIds;
                }

                authorizedRoomIds.Add(roomId);
                wasIssued = true;
            }

            return wasIssued;
        }

        private LobbyMembershipChange CreateRoomInLock(LobbyCaller caller, ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange;
            PlayerSession playerSession;

            if (!_sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.InvalidSession);
            }
            else if (IsUserOrChannelInRoom(playerSession.UserId, caller.ChannelId))
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.AlreadyInRoom, playerSession.UserId);
            }
            else
            {
                _lastRoomId++;
                List<LobbyMember> members = new List<LobbyMember>();
                _membersByRoomId[_lastRoomId] = members;
                _startsByRoomId[_lastRoomId] = new LobbyStartState();
                LobbyMember creator = new LobbyMember(playerSession.UserId, caller.ChannelId, lobbyCallback);
                AddMemberToRoom(_lastRoomId, members, creator);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    playerSession.UserId,
                    _lastRoomId,
                    CreateRoomSnapshot(_lastRoomId, members));
            }

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange EnterRoomInLock(
            LobbyCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange;
            PlayerSession playerSession;
            List<LobbyMember> members;

            if (!_sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.InvalidSession);
            }
            else if (IsUserOrChannelInRoom(playerSession.UserId, caller.ChannelId))
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.AlreadyInRoom, playerSession.UserId);
            }
            else if (!TryFindAuthorizedRoom(playerSession.UserId, roomId, out members))
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotAuthorized, playerSession.UserId);
            }
            else if (_startsByRoomId[roomId].MatchId.HasValue)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotAuthorized, playerSession.UserId);
            }
            else if (members.Count >= ValidationRules.MaximumPlayersPerMatchCount)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.RoomFull, playerSession.UserId);
            }
            else
            {
                IReadOnlyList<ILobbyManagerCallback> otherMembersCallbacks = CopyCallbacks(members);
                LobbyMember newMember = new LobbyMember(playerSession.UserId, caller.ChannelId, lobbyCallback);
                AddMemberToRoom(roomId, members, newMember);
                ConsumeRoomEntryAuthorization(playerSession.UserId, roomId);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    playerSession.UserId,
                    roomId,
                    CreateRoomSnapshot(roomId, members),
                    otherMembersCallbacks);
            }

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange RemoveUserFromRoom(int userId)
        {
            LobbyMembershipChange lobbyMembershipChange;
            RemoveChatChannelOfUserInLock(userId);
            int roomId;

            if (!_roomIdsByUserId.TryGetValue(userId, out roomId))
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotInRoom, userId);
            }
            else
            {
                List<LobbyMember> members = _membersByRoomId[roomId];
                LobbyMember leavingMember = members.Find(member => member.UserId == userId);
                members.Remove(leavingMember);
                _roomIdsByUserId.Remove(userId);
                _userIdsByChannelId.Remove(leavingMember.ChannelId);
                _startsByRoomId[roomId].ReadyUserIds.Remove(userId);

                if (members.Count > EmptyCount)
                {
                    RefreshLobbyCountdownInLock(roomId);
                    lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                        userId,
                        roomId,
                        CreateRoomSnapshot(roomId, members),
                        CopyCallbacks(members));
                }
                else
                {
                    _membersByRoomId.Remove(roomId);
                    _startsByRoomId.Remove(roomId);
                    RemoveRoomEntryAuthorizations(roomId);
                    RemoveInvitationsOfRoom(roomId);
                    lobbyMembershipChange = LobbyMembershipChange.CreateApplied(userId, roomId, null);
                }
            }

            return lobbyMembershipChange;
        }

        private SessionEnd RemoveSessionStateOfUser(PlayerSession endedSession)
        {
            LobbyMembershipChange roomDeparture = RemoveUserFromRoom(endedSession.UserId);
            _authorizedRoomIdsByUserId.Remove(endedSession.UserId);
            IReadOnlyList<IFriendsManagerCallback> presenceRecipients =
                FindPresenceRecipientsOfUser(endedSession.UserId);
            RemoveFriendsChannelOfUser(endedSession.UserId);
            RemoveInvitationsOfUser(endedSession.UserId);
            SessionEnd sessionEnd =
                new SessionEnd(endedSession.UserId, endedSession.Nickname, roomDeparture, presenceRecipients);

            return sessionEnd;
        }

        private bool IsUserOrChannelInRoom(int userId, string channelId)
        {
            bool isInRoom = _roomIdsByUserId.ContainsKey(userId) || _userIdsByChannelId.ContainsKey(channelId);

            return isInRoom;
        }

        private bool TryFindAuthorizedRoom(int userId, int roomId, out List<LobbyMember> members)
        {
            bool isAuthorized = false;
            HashSet<int> authorizedRoomIds;
            members = null;

            if (_authorizedRoomIdsByUserId.TryGetValue(userId, out authorizedRoomIds)
                && authorizedRoomIds.Contains(roomId))
            {
                isAuthorized = _membersByRoomId.TryGetValue(roomId, out members);
            }

            return isAuthorized;
        }

        private void AddMemberToRoom(int roomId, List<LobbyMember> members, LobbyMember member)
        {
            members.Add(member);
            _roomIdsByUserId[member.UserId] = roomId;
            _userIdsByChannelId[member.ChannelId] = member.UserId;
            RefreshLobbyCountdownInLock(roomId);
        }

        private void ConsumeRoomEntryAuthorization(int userId, int roomId)
        {
            HashSet<int> authorizedRoomIds = _authorizedRoomIdsByUserId[userId];
            authorizedRoomIds.Remove(roomId);

            if (authorizedRoomIds.Count == EmptyCount)
            {
                _authorizedRoomIdsByUserId.Remove(userId);
            }
        }

        private void RemoveRoomEntryAuthorizations(int roomId)
        {
            List<int> usersWithoutAuthorizations = new List<int>();

            foreach (KeyValuePair<int, HashSet<int>> pendingAuthorizations in _authorizedRoomIdsByUserId)
            {
                pendingAuthorizations.Value.Remove(roomId);

                if (pendingAuthorizations.Value.Count == EmptyCount)
                {
                    usersWithoutAuthorizations.Add(pendingAuthorizations.Key);
                }
            }

            foreach (int userId in usersWithoutAuthorizations)
            {
                _authorizedRoomIdsByUserId.Remove(userId);
            }
        }

        private LobbyRoomSnapshot CreateRoomSnapshot(int roomId, List<LobbyMember> members)
        {
            List<PlayerSession> players = new List<PlayerSession>();

            foreach (LobbyMember member in members)
            {
                players.Add(_sessionsByToken[_tokensByUserId[member.UserId]]);
            }

            LobbyRoomSnapshot lobbyRoomSnapshot =
                new LobbyRoomSnapshot(roomId, players.AsReadOnly(), CreateStartSnapshotInLock(roomId));

            return lobbyRoomSnapshot;
        }

        private static IReadOnlyList<ILobbyManagerCallback> CopyCallbacks(List<LobbyMember> members)
        {
            List<ILobbyManagerCallback> callbacks = new List<ILobbyManagerCallback>();

            foreach (LobbyMember member in members)
            {
                callbacks.Add(member.Callback);
            }

            IReadOnlyList<ILobbyManagerCallback> readOnlyCallbacks = callbacks.AsReadOnly();

            return readOnlyCallbacks;
        }
    }
}