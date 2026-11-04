using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Chat;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IChatManager
    {
        private static readonly ChatService _chatService;

        private bool _isChatChannelObserved;
        private string _chatChannelId;

        /// <inheritdoc />
        public void SubscribeToChat(string sessionToken)
        {
            IChatManagerCallback callback = null;
            SessionOutcome<OperationSubscribeToChatResult> sessionOutcome;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
                ObserveChatChannel();
                sessionOutcome = _chatService.SubscribeToChat(sessionToken, _chatChannelId, callback);
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al suscribir el canal de chat.", exception);
                sessionOutcome = new SessionOutcome<OperationSubscribeToChatResult>(
                    new OperationSubscribeToChatResult
                    {
                        ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey
                    });
            }

            if (callback != null)
            {
                try
                {
                    SafeNotify(() => callback.SubscribeToChatResult(sessionOutcome.Result));
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar el resultado de suscripción al chat.", exception);
                }
            }
        }

        /// <inheritdoc />
        public void SendChatMessage(string sessionToken, OperationSendChatMessageRequest request)
        {
            IChatManagerCallback callback = null;
            SessionOutcome<OperationSendChatMessageResult> sessionOutcome;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
                ObserveChatChannel();
                sessionOutcome = _chatService.SendChatMessage(sessionToken, _chatChannelId, request);
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al enviar un mensaje al chat.", exception);
                Guid clientMessageId = Guid.Empty;

                if (request != null)
                {
                    clientMessageId = request.ClientMessageId;
                }

                sessionOutcome = new SessionOutcome<OperationSendChatMessageResult>(
                    new OperationSendChatMessageResult
                    {
                        ClientMessageId = clientMessageId,
                        ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey
                    });
            }

            if (callback != null && sessionOutcome.Result != null)
            {
                try
                {
                    SafeNotify(() => callback.SendChatMessageResult(sessionOutcome.Result));
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar el rechazo del mensaje de chat.", exception);
                }
            }

            foreach (Action notification in sessionOutcome.Notifications)
            {
                try
                {
                    SafeNotify(notification);
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar un mensaje de chat a un destinatario.", exception);
                }
            }
        }

        /// <summary>
        /// Observa el ciclo de vida del canal una sola vez; su vida coincide con esta instancia PerSession.
        /// </summary>
        private void ObserveChatChannel()
        {
            if (!_isChatChannelObserved)
            {
                IContextChannel chatChannel = OperationContext.Current.Channel;
                _chatChannelId = chatChannel.SessionId;
                chatChannel.Closed += OnChatChannelEnded;
                chatChannel.Faulted += OnChatChannelEnded;
                _isChatChannelObserved = true;
            }
        }

        /// <summary>Retira una suscripción cerrada o fallida sin afectar al canal que la haya reemplazado.</summary>
        /// <param name="sender">Canal que terminó.</param>
        /// <param name="eventArguments">Datos del evento de cierre o fallo.</param>
        private void OnChatChannelEnded(object sender, EventArgs eventArguments)
        {
            try
            {
                _sessionRegistry.RemoveChatChannel(_chatChannelId);
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al retirar el canal de chat terminado.", exception);
            }
        }
    }
}
