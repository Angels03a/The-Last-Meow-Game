using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Sessions;

namespace TLMG.Services.Chat { 

    public sealed class ChatService
    {
        // TODO: Persist the game (R-12), so that messages are saved in the appropriate scope.
        private const ChatMessageScope PersistedChatScope = ChatMessageScope.Lobby;

        private static readonly ILog _log = LogManager.GetLogger(typeof(ChatService));

        private ISessionRegistry _sessionRegistry;
        private IChatMessageRepository _chatMessageRepository;

        public ChatService(ISessionRegistry sessionRegistry, IChatMessageRepository chatMessageRepository)
        {
            _sessionRegistry = sessionRegistry;
            _chatMessageRepository = chatMessageRepository;
        }

        public ResultWithNotification<OperationSubscribeToChatResult> SubscribeToChat(
            LobbyCaller caller,
            IChatManagerCallback callback)
        {
            string errorKey = _sessionRegistry.SubscribeToChat(caller, callback);
            OperationSubscribeToChatResult operationSubscribeToChatResult = new OperationSubscribeToChatResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey
            };
            LogChatRejection(errorKey);

            ResultWithNotification<OperationSubscribeToChatResult> subscribeToChatResult =
                new ResultWithNotification<OperationSubscribeToChatResult>(operationSubscribeToChatResult);

            return subscribeToChatResult;
        }

        public ResultWithNotification<OperationSendChatMessageResult> SendChatMessage(
            LobbyCaller caller,
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

            ChatDelivery chatDelivery;
            string errorKey = _sessionRegistry.AcceptChatMessage(caller, chatSubmission, out chatDelivery);
            ResultWithNotification<OperationSendChatMessageResult> sendChatMessageResult;

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
                    null, CreateChatActions(chatDelivery));
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

        private IReadOnlyList<Action> CreateChatActions(ChatDelivery chatDelivery)
        {
            List<Action> chatActions = CreateDeliveryNotifications(chatDelivery);
            ChatMessageRecord chatMessageRecord = CreateChatMessageRecord(chatDelivery);

            chatActions.Add(() => SaveChatMessage(chatMessageRecord));

            IReadOnlyList<Action> readOnlyChatActions = chatActions.AsReadOnly();

            return readOnlyChatActions;
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
                notifications.Add(() => recipient.OnChatMessageReceived(chatMessage));
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
    }
}