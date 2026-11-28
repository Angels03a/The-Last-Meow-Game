using System;
using System.Collections.Generic;
using System.Linq;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private static readonly TimeSpan _linkSilence = TimeSpan.FromSeconds(9);
        private static readonly TimeSpan _sessionSilence = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _minimumPulseInterval = TimeSpan.FromSeconds(1);
        private DateTime _nextHeartbeatSweepUtc;

        private void TrackLinkInLock(PlayerSession session, HeartbeatChannel channel, string channelId)
        {
            DateTime nowUtc = _readUtcNow();
            session.LastActivityUtc = nowUtc;
            if (!session.Links.TryGetValue(channel, out LinkHealth link) || link.ChannelId != channelId)
            {
                session.Links[channel] = new LinkHealth(channelId, nowUtc);
            }
        }

        public IReadOnlyList<Action> ProcessHeartbeat(string token, string channelId, HeartbeatChannel channel,
            int sequence, int confirmedSequence, Action<int, string> echo)
        {
            List<Action> notifications = new List<Action>();
            lock (_sessionsLock)
            {
                DateTime nowUtc = _readUtcNow();
                EvaluateHeartbeatDeadlinesInLock(nowUtc, notifications);
                if (string.IsNullOrEmpty(token) || !_sessionsByToken.TryGetValue(token, out PlayerSession session))
                {
                    notifications.Add(() => echo(sequence, ErrorKeys.Common.InvalidSession));
                }
                else if (session.Links.TryGetValue(channel, out LinkHealth link)
                    && link.ChannelId == channelId && IsCurrentLinkInLock(session.UserId, channel, channelId)
                    && HasValidHeartbeatSequence(link, sequence, confirmedSequence)
                    && HasValidHeartbeatTime(link, nowUtc))
                {
                    if (confirmedSequence != 0)
                    {
                        link.EvidenceUtc = nowUtc;
                    }

                    session.LastActivityUtc = nowUtc;
                    link.LastPulseUtc = nowUtc;
                    link.Sequence = sequence;
                    notifications.Add(() => echo(sequence, null));
                }
            }

            PersistPendingMatchFinalizations();
            PersistPendingKickVotings();
            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        public IReadOnlyList<Action> EvaluateConnectionDeadlines(bool force = true)
        {
            List<Action> notifications = new List<Action>();
            lock (_sessionsLock)
            {
                EvaluateHeartbeatDeadlinesInLock(_readUtcNow(), notifications, force);
            }

            PersistPendingMatchFinalizations();
            PersistPendingKickVotings();
            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private static bool HasValidHeartbeatSequence(LinkHealth link, int sequence, int confirmedSequence)
        {
            const int noConfirmedSequence = 0;
            bool hasValidSequence = sequence > link.Sequence && confirmedSequence >= noConfirmedSequence
                && (confirmedSequence == noConfirmedSequence || confirmedSequence == link.Sequence);

            return hasValidSequence;
        }

        private static bool HasValidHeartbeatTime(LinkHealth link, DateTime nowUtc)
        {
            bool hasValidTime = nowUtc < link.EvidenceUtc + _linkSilence
                && (!link.LastPulseUtc.HasValue || nowUtc - link.LastPulseUtc.Value >= _minimumPulseInterval);

            return hasValidTime;
        }

        private bool IsCurrentLinkInLock(int userId, HeartbeatChannel channel, string channelId)
        {
            Dictionary<string, int> channelOwners = _matchUserIdsByChannelId;
            switch (channel)
            {
                case HeartbeatChannel.Friends: channelOwners = _userIdsByFriendsChannelId; break;
                case HeartbeatChannel.Lobby: channelOwners = _userIdsByChannelId; break;
                case HeartbeatChannel.Chat: channelOwners = _chatUserIdsByChannelId; break;
            }

            bool isCurrentLink = channelOwners.TryGetValue(channelId, out int owner) && owner == userId;

            return isCurrentLink;
        }

        private void EvaluateHeartbeatDeadlinesInLock(DateTime nowUtc, List<Action> notifications, bool force = false)
        {
            if (force || nowUtc >= _nextHeartbeatSweepUtc)
            {
                _nextHeartbeatSweepUtc = nowUtc + _minimumPulseInterval;
                HashSet<MatchRecord> changedMatches = new HashSet<MatchRecord>();
                ExpireSilentLinksInLock(nowUtc, notifications, changedMatches);
                CollectChangedMatchNotificationsInLock(nowUtc, notifications, changedMatches);
                ExpireInactiveSessionsInLock(nowUtc, notifications);
            }
        }

        private void ExpireSilentLinksInLock(DateTime nowUtc, List<Action> notifications,
            HashSet<MatchRecord> changedMatches)
        {
            var expiredLinks = (from session in _sessionsByToken.Values
                                from entry in session.Links
                                where entry.Value.EvidenceUtc + _linkSilence <= nowUtc
                                orderby entry.Value.EvidenceUtc, entry.Key
                                select new { Session = session, Channel = entry.Key, Link = entry.Value }).ToArray();
            foreach (var expiredLink in expiredLinks)
            {
                expiredLink.Session.Links.Remove(expiredLink.Channel);
                if (IsCurrentLinkInLock(expiredLink.Session.UserId, expiredLink.Channel, expiredLink.Link.ChannelId))
                {
                    ExpireLinkInLock(expiredLink.Session, expiredLink.Channel, expiredLink.Link.EvidenceUtc + _linkSilence,
                        notifications, changedMatches);
                }
            }
        }

        private void CollectChangedMatchNotificationsInLock(DateTime nowUtc, List<Action> notifications,
            HashSet<MatchRecord> changedMatches)
        {
            foreach (MatchRecord match in _matchesById.Values)
            {
                if (ExpireMatchDeadlinesInLock(match, nowUtc))
                {
                    changedMatches.Add(match);
                }

                QueueMatchFinalizationInLock(match);
            }

            foreach (MatchRecord match in changedMatches)
            {
                match.Version++;
                notifications.AddRange(SessionNotificationHelper.CreateMatchViewNotifications(
                    CreateViewDeliveriesInLock(match)));
            }
        }

        private void ExpireInactiveSessionsInLock(DateTime nowUtc, List<Action> notifications)
        {
            foreach (PlayerSession session in _sessionsByToken.Values.ToArray())
            {
                bool hasLink = (from entry in session.Links
                                where IsCurrentLinkInLock(session.UserId, entry.Key, entry.Value.ChannelId)
                                select entry).Any();
                if (!hasLink && nowUtc >= session.LastActivityUtc + _sessionSilence
                    && !HasActiveParticipationInLock(session.UserId))
                {
                    notifications.AddRange(SessionNotificationHelper.CreateSessionEndNotifications(
                        EndSessionInLock(session, RoomExitCause.SessionEnded)));
                }
            }
        }

        private void ExpireLinkInLock(PlayerSession session, HeartbeatChannel channel, DateTime detectedUtc,
            List<Action> notifications, HashSet<MatchRecord> changedMatches)
        {
            int userId = session.UserId;
            switch (channel)
            {
                case HeartbeatChannel.Match:
                    MatchChannel matchLink = _matchChannelsByUserId[userId];
                    RemoveMatchChannelOfUserInLock(userId);
                    if (_matchesById.TryGetValue(matchLink.MatchId, out MatchRecord match))
                    {
                        if (ExpireMatchDeadlinesInLock(match, detectedUtc) | OpenMatchGraceInLock(match, userId,
                            detectedUtc))
                        {
                            changedMatches.Add(match);
                        }
                    }
                    break;
                case HeartbeatChannel.Friends:
                    Friend disconnectedFriend = new Friend
                    {
                        UserId = userId,
                        Nickname = session.Nickname,
                        IsOnline = false
                    };
                    notifications.AddRange(SessionNotificationHelper.CreatePresenceNotifications(
                        FindPresenceRecipientsOfUser(userId), disconnectedFriend));
                    RemoveFriendsChannelOfUser(userId);
                    break;
                case HeartbeatChannel.Chat:
                    RemoveChatChannelOfUserInLock(userId);
                    break;
                case HeartbeatChannel.Lobby:
                    if (!DetachStartedLobbyChannelInLock(userId))
                    {
                        notifications.AddRange(SessionNotificationHelper.CreateRoomNotifications(
                            RemoveUserFromRoom(userId, RoomExitCause.LobbyChannelClosed, out _)));
                    }
                    break;
            }
        }
    }
}