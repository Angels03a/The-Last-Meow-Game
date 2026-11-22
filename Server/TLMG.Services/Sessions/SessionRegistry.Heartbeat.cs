using System;
using System.Collections.Generic;
using System.Linq;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    public enum HeartbeatChannel { Friends, Lobby, Chat, Match }

    internal sealed class LinkHealth
    {
        internal LinkHealth(string channelId, DateTime nowUtc)
        {
            ChannelId = channelId;
            EvidenceUtc = nowUtc;
        }

        internal string ChannelId { get; }
        internal DateTime EvidenceUtc { get; set; }
        internal DateTime? LastPulseUtc { get; set; }
        internal int Sequence { get; set; }
    }

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
            var notifications = new List<Action>();
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
                    && nowUtc < link.EvidenceUtc + _linkSilence
                    && sequence > link.Sequence && confirmedSequence >= 0
                    && (confirmedSequence == 0 || confirmedSequence == link.Sequence)
                    && (!link.LastPulseUtc.HasValue || nowUtc - link.LastPulseUtc.Value >= _minimumPulseInterval))
                {
                    if (confirmedSequence != 0)
                    {
                        link.EvidenceUtc = nowUtc;
                    }

                    session.LastActivityUtc = nowUtc;
                    link.LastPulseUtc = nowUtc;
                    link.Sequence = sequence;
                    notifications.Add(() => echo(sequence, string.Empty));
                }
            }

            return notifications.AsReadOnly();
        }

        public IReadOnlyList<Action> EvaluateConnectionDeadlines(bool force = true)
        {
            var notifications = new List<Action>();
            lock (_sessionsLock)
            {
                EvaluateHeartbeatDeadlinesInLock(_readUtcNow(), notifications, force);
            }
            return notifications.AsReadOnly();
        }

        private bool IsCurrentLinkInLock(int userId, HeartbeatChannel channel, string channelId)
        {
            var channelOwners = _matchUserIdsByChannelId;
            switch (channel)
            {
                case HeartbeatChannel.Friends: channelOwners = _userIdsByFriendsChannelId; break;
                case HeartbeatChannel.Lobby: channelOwners = _userIdsByChannelId; break;
                case HeartbeatChannel.Chat: channelOwners = _chatUserIdsByChannelId; break;
            }

            return channelOwners.TryGetValue(channelId, out int owner) && owner == userId;
        }

        private void EvaluateHeartbeatDeadlinesInLock(DateTime nowUtc, List<Action> notifications, bool force = false)
        {
            if (!force && nowUtc < _nextHeartbeatSweepUtc)
            {
                return;
            }

            _nextHeartbeatSweepUtc = nowUtc + _minimumPulseInterval;
            var losses = (from session in _sessionsByToken.Values
                          from entry in session.Links
                          where entry.Value.EvidenceUtc + _linkSilence <= nowUtc
                          orderby entry.Value.EvidenceUtc, entry.Key
                          select new { Session = session, Channel = entry.Key, Link = entry.Value }).ToArray();
            var changedMatches = new HashSet<MatchRecord>();
            foreach (var loss in losses)
            {
                loss.Session.Links.Remove(loss.Channel);
                if (IsCurrentLinkInLock(loss.Session.UserId, loss.Channel, loss.Link.ChannelId))
                {
                    ExpireLinkInLock(loss.Session, loss.Channel, loss.Link.EvidenceUtc + _linkSilence,
                        notifications, changedMatches);
                }
            }

            foreach (MatchRecord match in _matchesById.Values)
            {
                if (ExpireMatchDeadlinesInLock(match, nowUtc))
                {
                    changedMatches.Add(match);
                }
            }

            foreach (MatchRecord match in changedMatches)
            {
                match.Version++;
                notifications.AddRange(SessionNotificationHelper.CreateMatchViewNotifications(
                    CreateViewDeliveriesInLock(match)));
            }

            foreach (PlayerSession session in _sessionsByToken.Values.ToArray())
            {
                bool hasLink = session.Links.Any(entry => IsCurrentLinkInLock(session.UserId, entry.Key,
                    entry.Value.ChannelId));
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
                    notifications.AddRange(SessionNotificationHelper.CreatePresenceNotifications(
                        FindPresenceRecipientsOfUser(userId), userId, session.Nickname, false));
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
