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
            LogChannelOperation("Chat", "SubscribeToChat", sessionToken);
            IChatManagerCallback callback = GetChatCallback();

            ResultWithNotification<OperationResult> subscribeToChatResultWithNotification =
                _chatService.SubscribeToChat(CreateChatCaller(sessionToken), callback);

            SafeNotify(() =>
                callback.SubscribeToChatResult(subscribeToChatResultWithNotification.OperationResult));
        }

        public void SendChatMessage(string sessionToken, OperationSendChatMessageRequest sendChatMessageRequest)
        {
            LogChannelOperation("Chat", "SendChatMessage", sessionToken);
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
                chatChannel.Closed += OnChatChannelClosed;
                chatChannel.Faulted += OnChatChannelFaulted;
                _isChatChannelObserved = true;
            }
        }

        private void OnChatChannelClosed(object sender, EventArgs eventArguments)
        {
            LogChannelEnd("Chat", "Closed", sender);
            OnChatChannelEnded(sender, eventArguments);
        }

        private void OnChatChannelFaulted(object sender, EventArgs eventArguments)
        {
            LogChannelEnd("Chat", "Faulted", sender);
            OnChatChannelEnded(sender, eventArguments);
        }

        private void OnChatChannelEnded(object sender, EventArgs eventArguments)
        {
            _sessionRegistry.RemoveChatChannel(_chatChannelId);
        }
        void IChatManager.ChatHeartbeat(string sessionToken, int sequence, int confirmedSequence)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            SafeNotifyAll(_sessionRegistry.ProcessHeartbeat(sessionToken, _chatChannelId, HeartbeatChannel.Chat,
                sequence, confirmedSequence, callback.ChatHeartbeatEcho));
        }

    }
}