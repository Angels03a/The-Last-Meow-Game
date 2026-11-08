using System;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IChatManager
    {
        private bool _isChatChannelObserved;
        private string _chatChannelId;

        public void SubscribeToChat(string sessionToken)
        {
            try
            {
                IChatManagerCallback callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
                ObserveChatChannel();

                ResultWithNotification<OperationSubscribeToChatResult> subscribeToChatResultWithNotification =
                    _chatService.SubscribeToChat(sessionToken, _chatChannelId, callback);

                SafeNotify(() => 
                callback.SubscribeToChatResult(subscribeToChatResultWithNotification.OperationResult));
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante SubscribeToChat.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void SendChatMessage(string sessionToken, OperationSendChatMessageRequest sendChatMessageRequest)
        {
            try
            {
                IChatManagerCallback callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
                ObserveChatChannel();

                ResultWithNotification<OperationSendChatMessageResult> sendChatMessageResultWithNotification =
                    _chatService.SendChatMessage(sessionToken, _chatChannelId, sendChatMessageRequest);

                if (sendChatMessageResultWithNotification.OperationResult != null)
                {
                    SafeNotify(() => 
                    callback.SendChatMessageResult(sendChatMessageResultWithNotification.OperationResult));
                }

                SafeNotifyAll(sendChatMessageResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante SendChatMessage.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
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