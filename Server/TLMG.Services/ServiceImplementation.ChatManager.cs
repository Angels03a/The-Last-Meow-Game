using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IChatManager
    {
        private bool _isChatChannelObserved;
        private string _chatChannelId;

        public void SubscribeToChat(string sessionToken)
        {
            IChatManagerCallback callback = GetChatCallback();

            ResultWithNotification<OperationSubscribeToChatResult> subscribeToChatResultWithNotification =
                _chatService.SubscribeToChat(CreateChatCaller(sessionToken), callback);

            SafeNotify(() =>
                callback.SubscribeToChatResult(subscribeToChatResultWithNotification.OperationResult));
        }

        public void SendChatMessage(string sessionToken, OperationSendChatMessageRequest sendChatMessageRequest)
        {
            IChatManagerCallback callback = GetChatCallback();

            ResultWithNotification<OperationSendChatMessageResult> sendChatMessageResultWithNotification =
                _chatService.SendChatMessage(CreateChatCaller(sessionToken), sendChatMessageRequest);

            if (sendChatMessageResultWithNotification.OperationResult != null)
            {
                SafeNotify(() =>
                    callback.SendChatMessageResult(sendChatMessageResultWithNotification.OperationResult));
            }

            SafeNotifyAll(sendChatMessageResultWithNotification.Notifications);
        }

        private IChatManagerCallback GetChatCallback()
        {
            IChatManagerCallback callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            ObserveChatChannel();

            return callback;
        }

        private LobbyCaller CreateChatCaller(string sessionToken)
        {
            LobbyCaller chatCaller = new LobbyCaller(sessionToken, _chatChannelId);

            return chatCaller;
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