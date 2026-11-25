using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Sessions;

namespace TLMG.Services.Chat
{
    public sealed class ChatService
    {
        private const ChatMessageScope PersistedChatScope = ChatMessageScope.Lobby;

        private static readonly ILog _log = LogManager.GetLogger(typeof(ChatService));

        private readonly ISessionRegistry _sessionRegistry;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ISessionAuthorizer _sessionAuthorizer;

        public ChatService(
            ISessionRegistry sessionRegistry,
            IChatMessageRepository chatMessageRepository,
            ISessionAuthorizer sessionAuthorizer)
        {
            _sessionRegistry = sessionRegistry;
            _chatMessageRepository = chatMessageRepository;
            _sessionAuthorizer = sessionAuthorizer;
        }

        public ResultWithNotification<OperationResult> SubscribeToChat(
            SessionChannelCaller caller,
            IChatManagerCallback callback)
        {
            SessionAuthorization authorization = _sessionAuthorizer.Authorize(
                caller.SessionToken,
                SystemPermission.UseChat);
            string errorKey = authorization.ErrorKey;

            if (errorKey == null)
            {
                errorKey = _sessionRegistry.SubscribeToChat(caller, callback);
            }

            if (errorKey != null && string.IsNullOrWhiteSpace(errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            OperationResult operationSubscribeToChatResult = new OperationResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey == null ? string.Empty : errorKey
            };
            LogChatRejection(errorKey);

            ResultWithNotification<OperationResult> subscribeToChatResult =
                new ResultWithNotification<OperationResult>(operationSubscribeToChatResult);

            return subscribeToChatResult;
        }

        public ResultWithNotification<OperationSendChatMessageResult> SendChatMessage(
            SessionChannelCaller caller,
            OperationSendChatMessageRequest sendChatMessageRequest)
        {
            string text = null;
            int roomId = LobbyMembershipChange.NoRoomId;
            Guid clientMessageId = Guid.Empty;

            if (sendChatMessageRequest != null)
            {
                text = sendChatMessageRequest.Text;
                roomId = sendChatMessageRequest.RoomId;
                clientMessageId = sendChatMessageRequest.ClientMessageId;
            }

            string normalizedText;
            string validationErrorKey = ChatValidator.ValidateText(text, out normalizedText);
            ChatSubmission chatSubmission = new ChatSubmission
            {
                RoomId = roomId,
                ClientMessageId = clientMessageId,
                Text = normalizedText,
                ValidationErrorKey = validationErrorKey
            };

            SessionAuthorization authorization = _sessionAuthorizer.Authorize(
                caller.SessionToken,
                SystemPermission.UseChat);
            ChatDelivery chatDelivery = null;
            string errorKey = authorization.ErrorKey;

            if (errorKey == null)
            {
                errorKey = _sessionRegistry.AcceptChatMessage(caller, chatSubmission, out chatDelivery);
            }

            ResultWithNotification<OperationSendChatMessageResult> sendChatMessageResult;

            if (errorKey == null && chatDelivery == null)
            {
                _log.Warn("La operación de chat aceptada no devolvió una entrega.");
                errorKey = ErrorKeys.Common.Unexpected;
            }
            else if (errorKey != null && string.IsNullOrWhiteSpace(errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            if (errorKey != null)
            {
                LogChatRejection(errorKey);
                sendChatMessageResult = new ResultWithNotification<OperationSendChatMessageResult>(
                    new OperationSendChatMessageResult
                    {
                        ClientMessageId = clientMessageId,
                        ErrorKey = errorKey
                    });
            }
            else
            {
                sendChatMessageResult = new ResultWithNotification<OperationSendChatMessageResult>(
                    null,
                    CreateChatActions(chatDelivery));
            }

            return sendChatMessageResult;
        }

        private static void LogChatRejection(string errorKey)
        {
            if (errorKey != null)
            {
                _log.WarnFormat("Se rechazó una operación de chat: {0}.", errorKey);
            }
        }

        private static List<Action> CreateDeliveryNotifications(ChatDelivery chatDelivery)
        {
            List<Action> notifications = new List<Action>();

            foreach (IChatManagerCallback recipient in chatDelivery.Recipients)
            {
                ChatMessage chatMessage = new ChatMessage
                {
                    RoomId = chatDelivery.RoomId,
                    ClientMessageId = chatDelivery.ClientMessageId,
                    SenderUserId = chatDelivery.Sender.UserId,
                    SenderNickname = chatDelivery.Sender.Nickname,
                    Text = chatDelivery.Text,
                    SentAtUtc = chatDelivery.SentAtUtc
                };
                ChatMessageNotification notification = new ChatMessageNotification(recipient, chatMessage);
                notifications.Add(notification.Send);
            }

            return notifications;
        }

        private static ChatMessageRecord CreateChatMessageRecord(ChatDelivery chatDelivery)
        {
            ChatMessageRecord chatMessageRecord = new ChatMessageRecord
            {
                UserId = chatDelivery.Sender.UserId,
                Scope = PersistedChatScope,
                RoomNumber = chatDelivery.RoomId,
                Text = chatDelivery.Text,
                SentAtUtc = chatDelivery.SentAtUtc
            };

            return chatMessageRecord;
        }

        private IReadOnlyList<Action> CreateChatActions(ChatDelivery chatDelivery)
        {
            List<Action> chatActions = CreateDeliveryNotifications(chatDelivery);
            ChatMessageSave chatMessageSave = new ChatMessageSave(this, CreateChatMessageRecord(chatDelivery));

            chatActions.Add(chatMessageSave.Save);

            IReadOnlyList<Action> readOnlyChatActions = chatActions.AsReadOnly();

            return readOnlyChatActions;
        }

        private void SaveChatMessage(ChatMessageRecord chatMessageRecord)
        {
            ChatMessageSaveStatus chatMessageSaveStatus = _chatMessageRepository.SaveChatMessage(chatMessageRecord);

            if (chatMessageSaveStatus != ChatMessageSaveStatus.Saved)
            {
                _log.WarnFormat(
                    "No se conservó un mensaje de chat del jugador {0}: {1}.",
                    chatMessageRecord.UserId,
                    chatMessageSaveStatus);
            }
        }

        private sealed class ChatMessageNotification
        {
            private readonly IChatManagerCallback _recipient;
            private readonly ChatMessage _chatMessage;

            public ChatMessageNotification(IChatManagerCallback recipient, ChatMessage chatMessage)
            {
                _recipient = recipient;
                _chatMessage = chatMessage;
            }

            public void Send()
            {
                _recipient.OnChatMessageReceived(_chatMessage);
            }
        }

        private sealed class ChatMessageSave
        {
            private readonly ChatService _chatService;
            private readonly ChatMessageRecord _chatMessageRecord;

            public ChatMessageSave(ChatService chatService, ChatMessageRecord chatMessageRecord)
            {
                _chatService = chatService;
                _chatMessageRecord = chatMessageRecord;
            }

            public void Save()
            {
                _chatService.SaveChatMessage(_chatMessageRecord);
            }
        }
    }
}
