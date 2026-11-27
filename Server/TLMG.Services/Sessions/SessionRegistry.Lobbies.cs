using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int EmptyCount = 0;

        private static readonly IReadOnlyList<ILobbyManagerCallback> _noLobbyCallbacks =
            new List<ILobbyManagerCallback>().AsReadOnly();

        private readonly Dictionary<int, List<LobbyMember>> _membersByRoomId =
            new Dictionary<int, List<LobbyMember>>();
        private readonly Dictionary<int, int> _roomIdsByUserId = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _userIdsByChannelId = new Dictionary<string, int>();
        private readonly Dictionary<int, HashSet<int>> _authorizedRoomIdsByUserId =
            new Dictionary<int, HashSet<int>>();
        private int _lastRoomId;

        public LobbyMembershipChange CreateRoom(SessionChannelCaller caller, ILobbyManagerCallback lobbyCallback)
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

        public LobbyMembershipChange EnterRoom(SessionChannelCaller caller, int roomId, ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.InvalidSession);

            if (_blockPolicy == null)
            {
                _log.Error("La política de bloqueos no está configurada al ingresar a una sala.");
                lobbyMembershipChange = CreateRejectedWithoutUser(LobbyChangeStatus.DataFailure);
            }
            else if (caller.HasSessionAndChannel)
            {
                lock (_blockPolicy.SynchronizationRoot)
                {
                    BlockListLookup blocks = FindSessionBlocks(caller.SessionToken);

                    lock (_sessionsLock)
                    {
                        LobbyChangeStatus blockValidation = ValidateRoomEntryBlocksInLock(caller, roomId, blocks);

                        if (blockValidation == LobbyChangeStatus.Applied)
                        {
                            lobbyMembershipChange = EnterRoomInLock(caller, roomId, lobbyCallback);
                        }
                        else
                        {
                            lobbyMembershipChange = CreateRejectedWithoutUser(blockValidation);
                        }

                        PlayerSession playerSession;

                        if (_sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
                        {
                            ConsumeRoomEntryAuthorization(playerSession.UserId, roomId);
                        }
                    }
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange LeaveRoom(
            string sessionToken,
            out IReadOnlyList<MatchViewDelivery> matchViewDeliveries)
        {
            matchViewDeliveries = _noMatchViewDeliveries;
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    PlayerSession playerSession;

                    if (_sessionsByToken.TryGetValue(sessionToken, out playerSession))
                    {
                        lobbyMembershipChange = RemoveUserFromRoom(
                            playerSession.UserId,
                            RoomExitCause.Voluntary,
                            out matchViewDeliveries);
                    }
                }
            }

            PersistPendingMatchFinalizations();
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
                        if (!DetachStartedLobbyChannelInLock(userId))
                        {
                            lobbyMembershipChange = RemoveUserFromRoom(userId, RoomExitCause.LobbyChannelClosed, out _);
                        }
                    }
                }
            }

            PersistPendingMatchFinalizations();
            return lobbyMembershipChange;
        }

        private bool DetachStartedLobbyChannelInLock(int userId)
        {
            bool wasDetached = _roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId.HasValue;

            if (wasDetached)
            {
                List<LobbyMember> members = _membersByRoomId[roomId];
                LobbyMember member = (from lobbyMember in members
                                      where lobbyMember.UserId == userId
                                      select lobbyMember).First();
                if (member.ChannelId != null)
                {
                    _userIdsByChannelId.Remove(member.ChannelId);
                    members[members.IndexOf(member)] = new LobbyMember(userId, null, null);
                }
            }

            return wasDetached;
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

            if (_tokensByUserId.ContainsKey(userId)
                && !_roomIdsByUserId.ContainsKey(userId)
                && _membersByRoomId.ContainsKey(roomId)
                && !_startsByRoomId[roomId].MatchId.HasValue
                && !IsExcludedFromRoomInLock(userId, roomId))
            {
                _authorizedRoomIdsByUserId[userId] = new HashSet<int>
                {
                    roomId
                };
                wasIssued = true;
            }

            return wasIssued;
        }

        private LobbyMembershipChange CreateRoomInLock(SessionChannelCaller caller, ILobbyManagerCallback lobbyCallback)
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
                _startsByRoomId[_lastRoomId] = new LobbyStartState
                {
                    RoomScopeId = Guid.NewGuid(),
                    LobbyOpenedAtUtc = _readUtcNow()
                };
                LobbyMember creator = new LobbyMember(playerSession.UserId, caller.ChannelId, lobbyCallback);
                AddMemberToRoom(_lastRoomId, members, creator);
                _authorizedRoomIdsByUserId.Remove(playerSession.UserId);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    playerSession.UserId,
                    new LobbyRoomUpdate(_lastRoomId, CreateRoomSnapshot(_lastRoomId, members)));
            }

            return lobbyMembershipChange;
        }

        private bool CanReconnectToLobbyInLock(int userId, int roomId, string channelId)
        {
            bool canReconnect = false;

            if (_roomIdsByUserId.TryGetValue(userId, out int currentRoomId) && currentRoomId == roomId)
            {
                MatchRecord recoverableMatch = FindRecoverableMatchInLock(userId);
                if (recoverableMatch != null && HasValidLobbyReconnectDeadline(recoverableMatch, userId)
                    && !_userIdsByChannelId.ContainsKey(channelId))
                {
                    canReconnect = (from member in _membersByRoomId[roomId]
                                    where member.UserId == userId && member.ChannelId == null
                                    select member).Any();
                }
            }

            return canReconnect;
        }

        private bool HasValidLobbyReconnectDeadline(MatchRecord match, int userId)
        {
            bool hasValidDeadline = match.MatchState.Phase == TLMG.Domain.Engine.RoundPhase.MatchFinished
                || !match.Disconnections.TryGetValue(userId, out TLMG.Domain.Engine.PlayerExit exit)
                || _readUtcNow() < exit.EffectiveAtUtc;

            return hasValidDeadline;
        }

        private LobbyMembershipChange EnterRoomInLock(
            SessionChannelCaller caller,
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
            else if (CanReconnectToLobbyInLock(playerSession.UserId, roomId, caller.ChannelId))
            {
                members = _membersByRoomId[roomId];
                LobbyMember reconnectingMember = (from member in members
                                                  where member.UserId == playerSession.UserId
                                                  select member).First();
                int memberIndex = members.IndexOf(reconnectingMember);
                members[memberIndex] = new LobbyMember(playerSession.UserId, caller.ChannelId, lobbyCallback);
                _userIdsByChannelId.Add(caller.ChannelId, playerSession.UserId);
                TrackLinkInLock(playerSession, HeartbeatChannel.Lobby, caller.ChannelId);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    playerSession.UserId,
                    new LobbyRoomUpdate(roomId, CreateRoomSnapshot(roomId, members), _noLobbyCallbacks));
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
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    playerSession.UserId,
                    new LobbyRoomUpdate(roomId, CreateRoomSnapshot(roomId, members), otherMembersCallbacks));
            }

            return lobbyMembershipChange;
        }

        private LobbyMembershipChange RemoveUserFromRoom(
            int userId,
            RoomExitCause exitCause,
            out IReadOnlyList<MatchViewDelivery> matchViewDeliveries)
        {
            bool leftAuthorizedMatch = _roomIdsByUserId.TryGetValue(userId, out int roomId)
                && _startsByRoomId[roomId].MatchId.HasValue;
            LobbyDeparture departure = new LobbyDeparture(userId, exitCause, leftAuthorizedMatch);
            LobbyDepartureDelivery delivery = RemoveUserFromRoom(departure, _readUtcNow());
            matchViewDeliveries = delivery.MatchViews;

            return delivery.Membership;
        }

        private LobbyDepartureDelivery RemoveUserFromRoom(LobbyDeparture departure, DateTime nowUtc)
        {
            LobbyMembershipChange membership;
            int userId = departure.UserId;
            IReadOnlyList<MatchViewDelivery> matchViewDeliveries = _noMatchViewDeliveries;
            RemoveChatChannelOfUserInLock(userId);
            RemoveMatchChannelOfUserInLock(userId);
            int roomId;

            if (_roomIdsByUserId.TryGetValue(userId, out roomId))
            {
                InvalidatePendingMatchStartInLock(roomId);
                Guid? matchId = _startsByRoomId[roomId].MatchId;

                if (matchId.HasValue && IsImmediateMatchExit(departure.ExitCause))
                {
                    PlayerExit exit = CreateImmediatePlayerExit(departure.ExitCause, nowUtc);
                    matchViewDeliveries = ApplyImmediateMatchExitInLock(userId, matchId.Value, exit);
                }

                membership = RemoveMemberFromRoomInLock(roomId, departure);
            }
            else
            {
                membership = LobbyMembershipChange.CreateRejected(LobbyChangeStatus.NotInRoom, userId);
            }

            LobbyDepartureDelivery delivery = new LobbyDepartureDelivery(membership, matchViewDeliveries);

            return delivery;
        }

        private static bool IsImmediateMatchExit(RoomExitCause exitCause)
        {
            bool isImmediate = exitCause == RoomExitCause.Voluntary || exitCause == RoomExitCause.Sanctioned
                || exitCause == RoomExitCause.VotedOut;

            return isImmediate;
        }

        private static PlayerExit CreateImmediatePlayerExit(RoomExitCause exitCause, DateTime nowUtc)
        {
            PlayerExit exit;

            if (exitCause == RoomExitCause.Sanctioned)
            {
                exit = PlayerExit.CreateSanctioned(nowUtc);
            }
            else if (exitCause == RoomExitCause.VotedOut)
            {
                exit = PlayerExit.CreateVotedOut(nowUtc);
            }
            else
            {
                exit = PlayerExit.CreateVoluntary(nowUtc);
            }

            return exit;
        }

        private LobbyMembershipChange RemoveMemberFromRoomInLock(int roomId, LobbyDeparture departure)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int userId = departure.UserId;

            List<LobbyMember> members = _membersByRoomId[roomId];
            LobbyMember leavingMember = (from member in members
                                         where member.UserId == userId
                                         select member).First();
            members.Remove(leavingMember);
            _roomIdsByUserId.Remove(userId);
            _authorizedRoomIdsByUserId.Remove(userId);
            RemoveInvitationsOfUser(userId);
            if (leavingMember.ChannelId != null)
            {
                _userIdsByChannelId.Remove(leavingMember.ChannelId);
            }
            _startsByRoomId[roomId].ReadyUserIds.Remove(userId);
            _startsByRoomId[roomId].KickVoting.ResetVotes();

            if (members.Count > EmptyCount)
            {
                RefreshLobbyCountdownInLock(roomId);
                LobbyRoomUpdate roomUpdate = new LobbyRoomUpdate(
                    roomId, CreateRoomSnapshot(roomId, members), CopyCallbacks(members));
                lobbyMembershipChange = LobbyMembershipChange.CreateDeparture(departure, roomUpdate);
            }
            else
            {
                ReleaseMatchOfRemovedRoomInLock(_startsByRoomId[roomId]);
                _membersByRoomId.Remove(roomId);
                _startsByRoomId.Remove(roomId);
                RemoveRoomEntryAuthorizations(roomId);
                RemoveInvitationsOfRoom(roomId);
                LobbyRoomUpdate roomUpdate = new LobbyRoomUpdate(roomId, null);
                lobbyMembershipChange = LobbyMembershipChange.CreateDeparture(departure, roomUpdate);
            }

            return lobbyMembershipChange;
        }

        private SessionEnd RemoveSessionStateOfUser(PlayerSession endedSession, RoomExitCause exitCause)
        {
            IReadOnlyList<MatchViewDelivery> matchViewDeliveries;
            LobbyMembershipChange roomDeparture = RemoveUserFromRoom(
                endedSession.UserId, exitCause, out matchViewDeliveries);
            _authorizedRoomIdsByUserId.Remove(endedSession.UserId);
            IReadOnlyList<IFriendsManagerCallback> presenceRecipients =
                FindPresenceRecipientsOfUser(endedSession.UserId);
            FriendsChannel friendsChannel;
            _friendsChannelsByUserId.TryGetValue(endedSession.UserId, out friendsChannel);
            IFriendsManagerCallback friendsCallback = FindCallbackOfChannel(friendsChannel);
            RemoveFriendsChannelOfUser(endedSession.UserId);
            RemoveInvitationsOfUser(endedSession.UserId);
            SessionEnd sessionEnd =
                new SessionEnd(
                    endedSession.UserId,
                    endedSession.Nickname,
                    roomDeparture,
                    friendsCallback,
                    presenceRecipients);
            sessionEnd.MatchViews = matchViewDeliveries;

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
                isAuthorized = _membersByRoomId.TryGetValue(roomId, out members)
                    && !IsExcludedFromRoomInLock(userId, roomId);
            }

            return isAuthorized;
        }

        private void AddMemberToRoom(int roomId, List<LobbyMember> members, LobbyMember member)
        {
            members.Add(member);
            RemoveInvitationsOfUser(member.UserId);
            _startsByRoomId[roomId].KickVoting.ResetVotes();
            _roomIdsByUserId[member.UserId] = roomId;
            _userIdsByChannelId[member.ChannelId] = member.UserId;
            TrackLinkInLock(_sessionsByToken[_tokensByUserId[member.UserId]], HeartbeatChannel.Lobby, member.ChannelId);
            RefreshLobbyCountdownInLock(roomId);
        }

        private void ConsumeRoomEntryAuthorization(int userId, int roomId)
        {
            HashSet<int> authorizedRoomIds;

            if (_authorizedRoomIdsByUserId.TryGetValue(userId, out authorizedRoomIds))
            {
                authorizedRoomIds.Remove(roomId);

                if (authorizedRoomIds.Count == EmptyCount)
                {
                    _authorizedRoomIdsByUserId.Remove(userId);
                }
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
            LobbyRoomSnapshot roomSnapshot = CreateRoomSnapshot(roomId, members, _startsByRoomId[roomId]);

            return roomSnapshot;
        }

        private LobbyRoomSnapshot CreateRoomSnapshot(
            int roomId,
            List<LobbyMember> members,
            LobbyStartState lobbyStartState)
        {
            List<PlayerSession> players = new List<PlayerSession>();

            foreach (LobbyMember member in members)
            {
                players.Add(_sessionsByToken[_tokensByUserId[member.UserId]]);
            }

            LobbyRoomSnapshot lobbyRoomSnapshot =
                new LobbyRoomSnapshot(roomId, players.AsReadOnly(), CreateStartSnapshotInLock(lobbyStartState));

            return lobbyRoomSnapshot;
        }

        private static IReadOnlyList<ILobbyManagerCallback> CopyCallbacks(List<LobbyMember> members)
        {
            List<ILobbyManagerCallback> callbacks = new List<ILobbyManagerCallback>();

            foreach (LobbyMember member in members)
            {
                if (member.Callback != null)
                {
                    callbacks.Add(member.Callback);
                }
            }

            IReadOnlyList<ILobbyManagerCallback> readOnlyCallbacks = callbacks.AsReadOnly();

            return readOnlyCallbacks;
        }
    }
}