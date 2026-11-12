using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Validators;
using TLMG.Services.Sessions;

namespace TLMG.Services.Chat
{
    /// <summary>Valida mensajes y prepara entregas fuera del candado; no guarda historial.</summary>
    public sealed class ChatService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ChatService));

        private ISessionRegistry _sessionRegistry;

        /// <summary>Crea el servicio con el registro compartido.</summary>
        /// <param name="sessionRegistry">Registro de sesiones, salas y canales.</param>
        public ChatService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>Suscribe el canal del jugador a su sala actual.</summary>
        /// <param name="caller">Token de sesión y canal de chat.</param>
        /// <param name="callback">Callback de este canal.</param>
        /// <returns>Éxito o clave de rechazo, sin datos de otras salas.</returns>
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

        /// <summary>
        /// Valida y reserva el envío; el eco confirma aceptación y solo los rechazos tienen resultado.
        /// </summary>
        /// <param name="caller">Token vigente y canal suscrito que envía.</param>
        /// <param name="sendChatMessageRequest">Sala, correlación y texto; puede ser null.</param>
        /// <returns>Resultado null y entregas si se aceptó; rechazo sin entregas en otro caso.</returns>
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
                    null, CreateChatNotifications(chatDelivery));
            }

            return sendChatMessageResult;
        }

        /// <summary>Registra solo la clave del rechazo, nunca token ni texto.</summary>
        /// <param name="errorKey">Clave conocida, o null si no hay rechazo.</param>
        private static void LogChatRejection(string errorKey)
        {
            if (errorKey != null)
            {
                _log.WarnFormat("Se rechazó una operación de chat: {0}.", errorKey);
            }
        }

        /// <summary>Prepara una captura independiente por destinatario fuera de _sessionsLock.</summary>
        /// <param name="chatDelivery">Copia del envío aceptado.</param>
        /// <returns>Entregas que incluyen el eco al emisor.</returns>
        private static IReadOnlyList<Action> CreateChatNotifications(ChatDelivery chatDelivery)
        {
            // TODO: Conservar el chat durante 24 horas mediante persistencia. (BL-106).
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

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }
    }
}