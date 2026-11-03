using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    // Parte del registro que guarda las salas. Todo método que termina en InLock, y los ayudantes que llama,
    // se invoca únicamente con _sessionsLock tomado y solo toca memoria.
    public partial class SessionRegistry
    {
        // Máximo de jugadores por sala (Reglas de The Last Meow, §1.1.1).
        private const int MaximumPlayersPerRoom = 6;
        private const int EmptyCount = 0;

        private readonly Dictionary<int, List<LobbyMember>> _membersByRoomId =
            new Dictionary<int, List<LobbyMember>>();
        private readonly Dictionary<int, int> _roomIdsByUserId = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _userIdsByChannelId = new Dictionary<string, int>();
        private readonly Dictionary<int, HashSet<int>> _authorizedRoomIdsByUserId =
            new Dictionary<int, HashSet<int>>();
        private int _lastRoomId;

        /// <inheritdoc />
        public LobbyMembershipChange CreateRoom(
            string sessionToken,
            string channelId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);

            if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(channelId))
            {
                lock (_sessionsLock)
                {
                    lobbyMembershipChange = CreateRoomInLock(sessionToken, channelId, lobbyCallback);
                }
            }

            return lobbyMembershipChange;
        }

        /// <inheritdoc />
        public LobbyMembershipChange EnterRoom(
            string sessionToken,
            int roomId,
            string channelId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);

            if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(channelId))
            {
                lock (_sessionsLock)
                {
                    lobbyMembershipChange = EnterRoomInLock(sessionToken, roomId, channelId, lobbyCallback);
                }
            }

            return lobbyMembershipChange;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IssueRoomEntryAuthorization(int userId, int roomId)
        {
            bool wasIssued;

            lock (_sessionsLock)
            {
                wasIssued = IssueRoomEntryAuthorizationInLock(userId, roomId);
            }

            return wasIssued;
        }

        private bool IssueRoomEntryAuthorizationInLock(int userId, int roomId)
        {
            bool wasIssued = false;

            if (_tokensByUserId.ContainsKey(userId) && _membersByRoomId.ContainsKey(roomId))
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

        private LobbyMembershipChange CreateRoomInLock(
            string sessionToken,
            string channelId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange;
            PlayerSession playerSession;

            if (!_sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                    LobbyChangeStatus.InvalidSession,
                    LobbyMembershipChange.NoUserId);
            }
            else if (IsUserOrChannelInRoom(playerSession.UserId, channelId))
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.AlreadyInRoom, playerSession.UserId);
            }
            else
            {
                _lastRoomId++;
                List<LobbyMember> members = new List<LobbyMember>();
                _membersByRoomId[_lastRoomId] = members;
                LobbyMember creator = new LobbyMember(playerSession.UserId, channelId, lobbyCallback);
                AddMemberToRoom(_lastRoomId, members, creator);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    playerSession.UserId,
                    _lastRoomId,
                    CreateRoomSnapshot(_lastRoomId, members));
            }

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange EnterRoomInLock(
            string sessionToken,
            int roomId,
            string channelId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange;
            PlayerSession playerSession;
            List<LobbyMember> members;

            if (!_sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                    LobbyChangeStatus.InvalidSession,
                    LobbyMembershipChange.NoUserId);
            }
            else if (IsUserOrChannelInRoom(playerSession.UserId, channelId))
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.AlreadyInRoom, playerSession.UserId);
            }
            else if (!TryFindAuthorizedRoom(playerSession.UserId, roomId, out members))
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotAuthorized, playerSession.UserId);
            }
            else if (members.Count >= MaximumPlayersPerRoom)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.RoomFull, playerSession.UserId);
            }
            else
            {
                IReadOnlyList<ILobbyManagerCallback> otherMembersCallbacks = CopyCallbacks(members);
                LobbyMember newMember = new LobbyMember(playerSession.UserId, channelId, lobbyCallback);
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

                if (members.Count > EmptyCount)
                {
                    lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                        userId,
                        roomId,
                        CreateRoomSnapshot(roomId, members),
                        CopyCallbacks(members));
                }
                else
                {
                    _membersByRoomId.Remove(roomId);
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

            LobbyRoomSnapshot lobbyRoomSnapshot = new LobbyRoomSnapshot(roomId, players.AsReadOnly());

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