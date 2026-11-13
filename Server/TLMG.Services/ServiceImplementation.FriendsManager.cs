using System;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IFriendsManager
    {
        private bool _isFriendsChannelObserved;
        private string _friendsChannelId;

        public void SubscribeToFriends(string sessionToken)
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

        public void InviteFriendToRoom(string sessionToken, int friendUserId)
        {
            IFriendsManagerCallback callback = 
                OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            ResultWithNotification<OperationResult> inviteFriendToRoomResultWithNotification =
                _friendsService.InviteFriendToRoom(sessionToken, friendUserId);

            SafeNotify(() => 
            callback.InviteFriendToRoomResult(inviteFriendToRoomResultWithNotification.OperationResult));
            SafeNotifyAll(inviteFriendToRoomResultWithNotification.Notifications);
        }

        public void AcceptRoomInvitation(string sessionToken, int invitationId)
        {
            IFriendsManagerCallback callback = 
                OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            ResultWithNotification<OperationAcceptRoomInvitationResult> acceptRoomInvitationResultWithNotification =
                _friendsService.AcceptRoomInvitation(sessionToken, invitationId);

            SafeNotify(() => 
            callback.AcceptRoomInvitationResult(acceptRoomInvitationResultWithNotification.OperationResult));
            SafeNotifyAll(acceptRoomInvitationResultWithNotification.Notifications);
        }

        public void SendFriendRequest(string sessionToken, string friendNickname)
        {
            IFriendsManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            ResultWithNotification<OperationSendFriendRequestResult> sendFriendRequestResultWithNotification =
                _friendsService.SendFriendRequest(sessionToken, friendNickname);

            SafeNotify(() =>
                callback.SendFriendRequestResult(sendFriendRequestResultWithNotification.OperationResult));
            SafeNotifyAll(sendFriendRequestResultWithNotification.Notifications);
        }

        public void AcceptFriendRequest(string sessionToken, int friendRequestId)
        {
            IFriendsManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            ResultWithNotification<OperationAcceptFriendRequestResult> acceptFriendRequestResultWithNotification =
                _friendsService.AcceptFriendRequest(sessionToken, friendRequestId);

            SafeNotify(() =>
                callback.AcceptFriendRequestResult(acceptFriendRequestResultWithNotification.OperationResult));
            SafeNotifyAll(acceptFriendRequestResultWithNotification.Notifications);
        }

        public void DeclineFriendRequest(string sessionToken, int friendRequestId)
        {
            IFriendsManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            ResultWithNotification<OperationDeclineFriendRequestResult> declineFriendRequestResultWithNotification =
                _friendsService.DeclineFriendRequest(sessionToken, friendRequestId);

            SafeNotify(() =>
                callback.DeclineFriendRequestResult(declineFriendRequestResultWithNotification.OperationResult));
            SafeNotifyAll(declineFriendRequestResultWithNotification.Notifications);
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