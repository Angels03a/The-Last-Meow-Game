using System;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IFriendsManager
    {
        private bool _isFriendsChannelObserved;
        private string _friendsChannelId;

        public void SubscribeToFriends(string sessionToken)
        {
            try
            {
                IFriendsManagerCallback callback = 
                    OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();
                ObserveFriendsChannel();

                ResultWithNotification<OperationSubscribeToFriendsResult> subscribeToFriendsResultWithNotification =
                    _friendsService.SubscribeToFriends(sessionToken, _friendsChannelId, callback);

                SafeNotify(() => 
                callback.SubscribeToFriendsResult(subscribeToFriendsResultWithNotification.OperationResult));
                SafeNotifyAll(subscribeToFriendsResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante SubscribeToFriends.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void InviteFriendToRoom(string sessionToken, int friendUserId)
        {
            try
            {
                IFriendsManagerCallback callback = 
                    OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

                ResultWithNotification<OperationInviteFriendToRoomResult> inviteFriendToRoomResultWithNotification =
                    _friendsService.InviteFriendToRoom(sessionToken, friendUserId);

                SafeNotify(() => 
                callback.InviteFriendToRoomResult(inviteFriendToRoomResultWithNotification.OperationResult));
                SafeNotifyAll(inviteFriendToRoomResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante InviteFriendToRoom.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void AcceptRoomInvitation(string sessionToken, int invitationId)
        {
            try
            {
                IFriendsManagerCallback callback = 
                    OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

                ResultWithNotification<OperationAcceptRoomInvitationResult> acceptRoomInvitationResultWithNotification =
                    _friendsService.AcceptRoomInvitation(sessionToken, invitationId);

                SafeNotify(() => 
                callback.AcceptRoomInvitationResult(acceptRoomInvitationResultWithNotification.OperationResult));
                SafeNotifyAll(acceptRoomInvitationResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante AcceptRoomInvitation.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void SendFriendRequest(string sessionToken, string friendNickname)
        {
            try
            {
                IFriendsManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

                ResultWithNotification<OperationSendFriendRequestResult> sendFriendRequestResultWithNotification =
                    _friendsService.SendFriendRequest(sessionToken, friendNickname);

                SafeNotify(() =>
                    callback.SendFriendRequestResult(sendFriendRequestResultWithNotification.OperationResult));
                SafeNotifyAll(sendFriendRequestResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante SendFriendRequest.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void AcceptFriendRequest(string sessionToken, int friendRequestId)
        {
            try
            {
                IFriendsManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

                ResultWithNotification<OperationAcceptFriendRequestResult> acceptFriendRequestResultWithNotification =
                    _friendsService.AcceptFriendRequest(sessionToken, friendRequestId);

                SafeNotify(() =>
                    callback.AcceptFriendRequestResult(acceptFriendRequestResultWithNotification.OperationResult));
                SafeNotifyAll(acceptFriendRequestResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante AcceptFriendRequest.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void DeclineFriendRequest(string sessionToken, int friendRequestId)
        {
            try
            {
                IFriendsManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

                ResultWithNotification<OperationDeclineFriendRequestResult> declineFriendRequestResultWithNotification =
                    _friendsService.DeclineFriendRequest(sessionToken, friendRequestId);

                SafeNotify(() =>
                    callback.DeclineFriendRequestResult(declineFriendRequestResultWithNotification.OperationResult));
                SafeNotifyAll(declineFriendRequestResultWithNotification.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante DeclineFriendRequest.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        private void ObserveFriendsChannel()
        {
            if (!_isFriendsChannelObserved)
            {
                IContextChannel friendsChannel = OperationContext.Current.Channel;
                _friendsChannelId = friendsChannel.SessionId;
                friendsChannel.Closed += OnFriendsChannelClosed;
                friendsChannel.Faulted += OnFriendsChannelFaulted;
                _isFriendsChannelObserved = true;
            }
        }

        private void OnFriendsChannelClosed(object sender, EventArgs eventArguments)
        {
            EndSessionOfFriendsChannel();
        }

        private void OnFriendsChannelFaulted(object sender, EventArgs eventArguments)
        {
            EndSessionOfFriendsChannel();
        }

        private void EndSessionOfFriendsChannel()
        {
            SafeNotifyAll(_friendsService.EndSessionByFriendsChannel(_friendsChannelId));
        }
    }
}