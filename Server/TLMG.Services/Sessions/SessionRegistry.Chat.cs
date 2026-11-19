using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int MinimumChatMessageIntervalSeconds = 1;

        private readonly Dictionary<int, ChatChannel> _chatChannelsByUserId = new Dictionary<int, ChatChannel>();
        private readonly Dictionary<string, int> _chatUserIdsByChannelId = new Dictionary<string, int>();
        private readonly Dictionary<int, DateTime> _lastChatSentAtUtcByUserId = new Dictionary<int, DateTime>();

        public string SubscribeToChat(LobbyCaller caller, IChatManagerCallback callback)
        {
            string errorKey;

            lock (_sessionsLock)
            {
                PurgeExpiredChatSendTimesInLock(DateTime.UtcNow);
                PlayerSession playerSession;
                errorKey = ValidateChatIdentityInLock(caller, out playerSession);

                if (errorKey == null)
                {
                    if (!_roomIdsByUserId.ContainsKey(playerSession.UserId))
                    {
                        errorKey = ErrorKeys.Lobby.NotInRoom;
                    }
                    else if (callback == null)
                    {
                        errorKey = ErrorKeys.Common.InvalidSession;
                    }
                    else
                    {
                        RemoveChatChannelOfUserInLock(playerSession.UserId);
                        _chatChannelsByUserId[playerSession.UserId] =
                            new ChatChannel(caller.SessionToken, caller.ChannelId, callback);
                        _chatUserIdsByChannelId[caller.ChannelId] = playerSession.UserId;
                        TrackLinkInLock(playerSession, HeartbeatChannel.Chat, caller.ChannelId);
                    }
                }
            }

            return errorKey;
        }

        public string AcceptChatMessage(LobbyCaller caller, ChatSubmission submission, out ChatDelivery chatDelivery)
        {
            string errorKey;
            chatDelivery = null;

            lock (_sessionsLock)
            {
                DateTime sentAtUtc = DateTime.UtcNow;
                PurgeExpiredChatSendTimesInLock(sentAtUtc);
                PlayerSession playerSession;
                errorKey = ValidateChatIdentityInLock(caller, out playerSession);

                if (errorKey == null)
                {
                    errorKey = ValidateChatMembershipInLock(caller, playerSession.UserId, submission.RoomId);
                }

                if (errorKey == null)
                {
                    errorKey = submission.ValidationErrorKey;
                }

                if (errorKey == null)
                {
                    if (_lastChatSentAtUtcByUserId.ContainsKey(playerSession.UserId))
                    {
                        errorKey = ErrorKeys.Lobby.ChatRateLimit;
                    }
                    else
                    {
                        ChatReservation chatReservation =
                            new ChatReservation(sentAtUtc, CopyChatRecipientsInLock(submission.RoomId));
                        chatDelivery = new ChatDelivery(submission, playerSession, chatReservation);
                        _lastChatSentAtUtcByUserId[playerSession.UserId] = sentAtUtc;
                    }
                }
            }

            return errorKey;
        }

        public void RemoveChatChannel(string channelId)
        {
            lock (_sessionsLock)
            {
                PurgeExpiredChatSendTimesInLock(DateTime.UtcNow);
                int userId;

                if (!string.IsNullOrEmpty(channelId) && _chatUserIdsByChannelId.TryGetValue(channelId, out userId))
                {
                    ChatChannel chatChannel = _chatChannelsByUserId[userId];

                    if (chatChannel.ChannelId == channelId)
                    {
                        RemoveChatChannelOfUserInLock(userId);
                    }
                }
            }
        }

        private string ValidateChatIdentityInLock(LobbyCaller caller, out PlayerSession playerSession)
        {
            string errorKey = ErrorKeys.Common.InvalidSession;
            playerSession = null;
            int channelUserId;
            bool hasRegisteredSession = false;

            if (caller.HasSessionAndChannel && _sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                hasRegisteredSession = playerSession.UserId > LobbyMembershipChange.NoUserId;
            }

            if (hasRegisteredSession)
            {
                if (!_chatUserIdsByChannelId.TryGetValue(caller.ChannelId, out channelUserId)
                    || channelUserId == playerSession.UserId)
                {
                    errorKey = null;
                }
            }

            return errorKey;
        }

        private string ValidateChatMembershipInLock(LobbyCaller caller, int userId, int roomId)
        {
            string errorKey = null;
            ChatChannel chatChannel;
            int currentRoomId;

            if (!_chatChannelsByUserId.TryGetValue(userId, out chatChannel))
            {
                errorKey = ErrorKeys.Lobby.ChatNotSubscribed;
            }
            else if (chatChannel.SessionToken != caller.SessionToken || chatChannel.ChannelId != caller.ChannelId)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }
            else if (!_roomIdsByUserId.TryGetValue(userId, out currentRoomId) || currentRoomId != roomId)
            {
                errorKey = ErrorKeys.Lobby.NotInRoom;
            }

            return errorKey;
        }

        private IReadOnlyList<IChatManagerCallback> CopyChatRecipientsInLock(int roomId)
        {
            List<IChatManagerCallback> recipients = new List<IChatManagerCallback>();

            foreach (LobbyMember lobbyMember in _membersByRoomId[roomId])
            {
                ChatChannel chatChannel;

                if (_chatChannelsByUserId.TryGetValue(lobbyMember.UserId, out chatChannel))
                {
                    recipients.Add(chatChannel.Callback);
                }
            }

            IReadOnlyList<IChatManagerCallback> readOnlyRecipients = recipients.AsReadOnly();

            return readOnlyRecipients;
        }

        private void RemoveChatChannelOfUserInLock(int userId)
        {
            ChatChannel chatChannel;

            if (_chatChannelsByUserId.TryGetValue(userId, out chatChannel))
            {
                _chatUserIdsByChannelId.Remove(chatChannel.ChannelId);
                _chatChannelsByUserId.Remove(userId);
            }
        }

        private void PurgeExpiredChatSendTimesInLock(DateTime nowUtc)
        {
            List<int> expiredUserIds = new List<int>();

            foreach (KeyValuePair<int, DateTime> lastSend in _lastChatSentAtUtcByUserId)
            {
                if ((nowUtc - lastSend.Value).TotalSeconds >= MinimumChatMessageIntervalSeconds)
                {
                    expiredUserIds.Add(lastSend.Key);
                }
            }

            foreach (int userId in expiredUserIds)
            {
                _lastChatSentAtUtcByUserId.Remove(userId);
            }
        }
    }
}