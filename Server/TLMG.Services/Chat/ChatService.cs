using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Chat
{
    /// <summary>Valida mensajes y prepara entregas fuera del candado; no guarda historial.</summary>
    public sealed class ChatService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ChatService));

        private readonly ISessionRegistry _sessionRegistry;

        /// <summary>Crea el servicio con el registro compartido.</summary>
        /// <param name="sessionRegistry">Registro de sesiones, salas y canales.</param>
        public ChatService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>Suscribe el canal del jugador a su sala actual.</summary>
        /// <param name="sessionToken">Token de sesión.</param>
        /// <param name="channelId">Canal de chat.</param>
        /// <param name="callback">Callback de este canal.</param>
        /// <returns>Éxito o clave de rechazo, sin datos de otras salas.</returns>
        public SessionOutcome<OperationSubscribeToChatResult> SubscribeToChat(
            string sessionToken,
            string channelId,
            IChatManagerCallback callback)
        {
            string errorKey = _sessionRegistry.SubscribeToChat(sessionToken, channelId, callback);
            OperationSubscribeToChatResult operationSubscribeToChatResult = new OperationSubscribeToChatResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey
            };
            LogChatRejection(errorKey);
            SessionOutcome<OperationSubscribeToChatResult> sessionOutcome =
                new SessionOutcome<OperationSubscribeToChatResult>(operationSubscribeToChatResult);

            return sessionOutcome;
        }

        /// <summary>
        /// Valida y reserva el envío; el eco confirma aceptación y solo los rechazos tienen resultado.
        /// </summary>
        /// <param name="sessionToken">Token vigente.</param>
        /// <param name="channelId">Canal suscrito que envía.</param>
        /// <param name="request">Sala, correlación y texto; puede ser null.</param>
        /// <returns>Resultado null y entregas si se aceptó; rechazo sin entregas en otro caso.</returns>
        public SessionOutcome<OperationSendChatMessageResult> SendChatMessage(
            string sessionToken,
            string channelId,
            OperationSendChatMessageRequest request)
        {
            string text = null;
            int roomId = LobbyMembershipChange.NoRoomId;
            Guid clientMessageId = Guid.Empty;

            if (request != null)
            {
                text = request.Text;
                roomId = request.RoomId;
                clientMessageId = request.ClientMessageId;
            }

            string normalizedText;
            string validationErrorKey = ChatValidationHelper.ValidateText(text, out normalizedText);
            ChatDelivery chatDelivery;
            string errorKey = _sessionRegistry.AcceptChatMessage(
                sessionToken, channelId, roomId, clientMessageId, normalizedText, validationErrorKey, out chatDelivery);
            SessionOutcome<OperationSendChatMessageResult> sessionOutcome;

            if (errorKey != null)
            {
                LogChatRejection(errorKey);
                sessionOutcome = new SessionOutcome<OperationSendChatMessageResult>(
                    new OperationSendChatMessageResult
                    {
                        ClientMessageId = clientMessageId,
                        ErrorKey = errorKey
                    });
            }
            else
            {
                sessionOutcome = new SessionOutcome<OperationSendChatMessageResult>(
                    null, CreateChatNotifications(chatDelivery));
            }

            return sessionOutcome;
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
