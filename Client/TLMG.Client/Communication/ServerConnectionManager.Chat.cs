using System;
using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager
    {
        private ChatManagerClient _chatClient;
        private ServerCallback _chatCallback;

        /// <summary>Entrega errores de chat para traducirlos en la interfaz.</summary>
        public event EventHandler<ServerEventArgs<string>> ChatErrorReceived;

        /// <summary>Entrega exclusivamente mensajes confirmados por el servidor.</summary>
        public event EventHandler<ServerEventArgs<ChatMessage>> ChatMessageReceived;

        /// <summary>Avisa que cambió la disponibilidad del chat.</summary>
        public event EventHandler ChatAvailabilityChanged;

        /// <summary>Obtiene si el servidor aceptó la suscripción actual.</summary>
        public bool IsChatSubscribed { get; private set; }

        /// <summary>Envía un mensaje; su eco es la única confirmación de entrega.</summary>
        /// <param name="messageText">Texto solicitado por el jugador.</param>
        /// <returns>Indica si el envío se completó por el canal.</returns>
        public bool SendChatMessage(string messageText)
        {
            bool wasMessageSent = false;
            bool hasChatRoom = CurrentRoom != null && IsChatSubscribed;

            if (hasChatRoom && _chatClient != null)
            {
                OperationSendChatMessageRequest chatRequest = new OperationSendChatMessageRequest
                {
                    ClientMessageId = Guid.NewGuid(),
                    RoomId = CurrentRoom.RoomId,
                    Text = messageText
                };
                wasMessageSent = TrySend(
                    () => _chatClient.SendChatMessage(_sessionToken, chatRequest),
                    "enviar un mensaje de chat");
            }

            return wasMessageSent;
        }

        internal void NotifyChatSubscriptionResult(ServerCallback callback, OperationSubscribeToChatResult chatResult)
        {
            if (IsCurrentChatCallback(callback) && chatResult != null)
            {
                IsChatSubscribed = chatResult.IsSuccess;
                ChatAvailabilityChanged?.Invoke(this, EventArgs.Empty);
                if (!chatResult.IsSuccess)
                {
                    ChatErrorReceived?.Invoke(this, new ServerEventArgs<string>(chatResult.ErrorKey));
                }
            }
        }

        internal void NotifyChatSendResult(ServerCallback callback, OperationSendChatMessageResult chatResult)
        {
            if (IsCurrentChatCallback(callback) && chatResult != null)
            {
                ChatErrorReceived?.Invoke(this, new ServerEventArgs<string>(chatResult.ErrorKey));
            }
        }

        internal void NotifyChatMessageReceived(ServerCallback callback, ChatMessage chatMessage)
        {
            if (IsCurrentChatCallback(callback) && chatMessage != null)
            {
                if (chatMessage.RoomId == CurrentRoom.RoomId)
                {
                    ChatMessageReceived?.Invoke(this, new ServerEventArgs<ChatMessage>(chatMessage));
                }
            }
        }

        private bool IsCurrentChatCallback(ServerCallback callback)
        {
            bool isCurrentCallback = ReferenceEquals(callback, _chatCallback) && _chatClient != null;
            bool hasCurrentRoom = isCurrentCallback && CurrentRoom != null;
            return hasCurrentRoom;
        }

        private void OpenChatForCurrentRoom()
        {
            bool needsChatChannel = _chatClient == null && HasRegisteredSession;
            if (needsChatChannel && CurrentRoom != null)
            {
                _chatCallback = new ServerCallback(this);
                _chatClient = new ChatManagerClient(new InstanceContext(_chatCallback));
                WatchChannel(_chatClient.InnerChannel);
                bool wasChannelOpened = TrySend(_chatClient.Open, "abrir el canal de chat");
                if (wasChannelOpened)
                {
                    TrySend(() => _chatClient.SubscribeToChat(_sessionToken), "suscribirse al chat");
                }
            }
        }

        private void CloseChatChannel()
        {
            ChatManagerClient previousClient = _chatClient;
            _chatClient = null;
            _chatCallback = null;
            IsChatSubscribed = false;
            if (previousClient != null)
            {
                UnwatchChannel(previousClient.InnerChannel);
                CloseClientSafely(previousClient);
            }
            ChatAvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
