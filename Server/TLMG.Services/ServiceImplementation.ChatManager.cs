using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Chat;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IChatManager
    {
        private static readonly ChatService _chatService;

        private bool _isChatChannelObserved;
        private string _chatChannelId;

        public void SubscribeToChat(string sessionToken)
        {
            IChatManagerCallback callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            ObserveChatChannel();

            ResultWithNotification<OperationSubscribeToChatResult> subscribeToChatResultWithNotification =
                _chatService.SubscribeToChat(sessionToken, _chatChannelId, callback);

            SafeNotify(() => callback.SubscribeToChatResult(subscribeToChatResultWithNotification.OperationResult));
        }

        public void SendChatMessage(string sessionToken, OperationSendChatMessageRequest sendChatMessageRequest)
        {
            IChatManagerCallback callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            ObserveChatChannel();

            ResultWithNotification<OperationSendChatMessageResult> sendChatMessageResultWithNotification =
                _chatService.SendChatMessage(sessionToken, _chatChannelId, sendChatMessageRequest);

            if (sendChatMessageResultWithNotification.OperationResult != null)
            {
                SafeNotify(() => callback.SendChatMessageResult(sendChatMessageResultWithNotification.OperationResult));
            }

            SafeNotifyAll(sendChatMessageResultWithNotification.Notifications);
        }

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

        private void OnChatChannelEnded(object sender, EventArgs eventArguments)
        {
            _sessionRegistry.RemoveChatChannel(_chatChannelId);
        }
    }
}