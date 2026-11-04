using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Services.Helpers;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int MinimumChatMessageIntervalSeconds = 1;

        private readonly Dictionary<int, ChatChannel> _chatChannelsByUserId = new Dictionary<int, ChatChannel>();
        private readonly Dictionary<string, int> _chatUserIdsByChannelId = new Dictionary<string, int>();
        private readonly Dictionary<int, DateTime> _lastChatSentAtUtcByUserId = new Dictionary<int, DateTime>();

        /// <inheritdoc />
        public string SubscribeToChat(string sessionToken, string channelId, IChatManagerCallback callback)
        {
            string errorKey;

            lock (_sessionsLock)
            {
                PurgeExpiredChatSendTimesInLock(DateTime.UtcNow);
                PlayerSession playerSession;
                errorKey = ValidateChatIdentityInLock(sessionToken, channelId, out playerSession);

                if (errorKey == null)
                {
                    if (!_roomIdsByUserId.ContainsKey(playerSession.UserId))
                    {
                        errorKey = LobbyErrorKeyHelper.NotInRoomErrorKey;
                    }
                    else if (callback == null)
                    {
                        errorKey = CommonErrorKeyHelper.InvalidSessionErrorKey;
                    }
                    else
                    {
                        RemoveChatChannelOfUserInLock(playerSession.UserId);
                        _chatChannelsByUserId[playerSession.UserId] =
                            new ChatChannel(sessionToken, channelId, callback);
                        _chatUserIdsByChannelId[channelId] = playerSession.UserId;
                    }
                }
            }

            return errorKey;
        }

        /// <inheritdoc />
        public string AcceptChatMessage(
            string sessionToken,
            string channelId,
            int roomId,
            Guid clientMessageId,
            string normalizedText,
            string validationErrorKey,
            out ChatDelivery chatDelivery)
        {
            string errorKey;
            chatDelivery = null;

            lock (_sessionsLock)
            {
                DateTime sentAtUtc = DateTime.UtcNow;
                PurgeExpiredChatSendTimesInLock(sentAtUtc);
                PlayerSession playerSession;
                errorKey = ValidateChatIdentityInLock(sessionToken, channelId, out playerSession);

                if (errorKey == null)
                {
                    errorKey = ValidateChatMembershipInLock(playerSession.UserId, sessionToken, channelId, roomId);
                }

                if (errorKey == null)
                {
                    errorKey = validationErrorKey;
                }

                if (errorKey == null)
                {
                    if (_lastChatSentAtUtcByUserId.ContainsKey(playerSession.UserId))
                    {
                        errorKey = ChatValidationHelper.RateLimitErrorKey;
                    }
                    else
                    {
                        IReadOnlyList<IChatManagerCallback> recipients = CopyChatRecipientsInLock(roomId);
                        chatDelivery = new ChatDelivery(
                            roomId, clientMessageId, playerSession, normalizedText, sentAtUtc, recipients);
                        _lastChatSentAtUtcByUserId[playerSession.UserId] = sentAtUtc;
                    }
                }
            }

            return errorKey;
        }

        /// <inheritdoc />
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

        /// <summary>
        /// Valida la sesión registrada y que el canal no pertenezca a otro jugador bajo _sessionsLock.
        /// </summary>
        /// <param name="sessionToken">Token recibido.</param>
        /// <param name="channelId">Canal que hace la petición.</param>
        /// <param name="playerSession">Identidad vigente, o null si no existe.</param>
        /// <returns>null si la identidad es válida; error genérico en otro caso.</returns>
        private string ValidateChatIdentityInLock(
            string sessionToken,
            string channelId,
            out PlayerSession playerSession)
        {
            string errorKey = CommonErrorKeyHelper.InvalidSessionErrorKey;
            playerSession = null;
            int channelUserId;

            bool hasSessionAndChannel = !string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(channelId);
            bool hasRegisteredSession = false;

            if (hasSessionAndChannel && _sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                hasRegisteredSession = playerSession.UserId > LobbyMembershipChange.NoUserId;
            }

            if (hasRegisteredSession)
            {
                if (!_chatUserIdsByChannelId.TryGetValue(channelId, out channelUserId)
                    || channelUserId == playerSession.UserId)
                {
                    errorKey = null;
                }
            }

            return errorKey;
        }

        /// <summary>Valida el canal vigente antes de revelar pertenencia a una sala; requiere _sessionsLock.</summary>
        /// <param name="userId">Jugador registrado.</param>
        /// <param name="sessionToken">Token vigente.</param>
        /// <param name="channelId">Canal que intenta enviar.</param>
        /// <param name="roomId">Sala solicitada.</param>
        /// <returns>null si puede enviar; clave genérica de sesión, de suscripción o de pertenencia si no.</returns>
        private string ValidateChatMembershipInLock(int userId, string sessionToken, string channelId, int roomId)
        {
            string errorKey = null;
            ChatChannel chatChannel;
            int currentRoomId;

            if (!_chatChannelsByUserId.TryGetValue(userId, out chatChannel))
            {
                errorKey = ChatValidationHelper.NotSubscribedErrorKey;
            }
            else if (chatChannel.SessionToken != sessionToken || chatChannel.ChannelId != channelId)
            {
                errorKey = CommonErrorKeyHelper.InvalidSessionErrorKey;
            }
            else if (!_roomIdsByUserId.TryGetValue(userId, out currentRoomId) || currentRoomId != roomId)
            {
                errorKey = LobbyErrorKeyHelper.NotInRoomErrorKey;
            }

            return errorKey;
        }

        /// <summary>Copia solo los callbacks suscritos de los miembros de la sala bajo _sessionsLock.</summary>
        /// <param name="roomId">Sala existente del emisor validado.</param>
        /// <returns>Copia de solo lectura; incluye el callback del emisor.</returns>
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

        /// <summary>
        /// Retira la suscripción al salir o terminar sesión; conserva el intervalo bajo _sessionsLock.
        /// </summary>
        /// <param name="userId">Jugador cuya suscripción termina.</param>
        private void RemoveChatChannelOfUserInLock(int userId)
        {
            ChatChannel chatChannel;

            if (_chatChannelsByUserId.TryGetValue(userId, out chatChannel))
            {
                _chatUserIdsByChannelId.Remove(chatChannel.ChannelId);
                _chatChannelsByUserId.Remove(userId);
            }
        }

        /// <summary>
        /// Purga intervalos cumplidos al atender operaciones; conserva fechas futuras si retrocede UTC.
        /// </summary>
        /// <param name="nowUtc">Reloj del servidor leído dentro de _sessionsLock.</param>
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
