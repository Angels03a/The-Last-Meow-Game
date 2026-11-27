using System.ServiceModel;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ServerCallback : IFriendsManagerCallback, ILobbyManagerCallback, IChatManagerCallback,
        IMatchManagerCallback, ILeaderboardManagerCallback
    {
        private readonly ServerConnectionManager _serverConnection;

        internal long? HeartbeatDue { get; set; }
        internal long LastHeartbeatEchoAt { get; set; }
        internal int HeartbeatSequence { get; set; }
        internal int PendingHeartbeat { get; set; }
        internal int ConfirmedHeartbeat { get; set; }

        public void FriendsHeartbeatEcho(int sequence, string errorKey)
        {
            HeartbeatEcho(sequence, errorKey);
        }

        public void LobbyHeartbeatEcho(int sequence, string errorKey)
        {
            HeartbeatEcho(sequence, errorKey);
        }

        public void ChatHeartbeatEcho(int sequence, string errorKey)
        {
            HeartbeatEcho(sequence, errorKey);
        }

        public void MatchHeartbeatEcho(int sequence, string errorKey)
        {
            HeartbeatEcho(sequence, errorKey);
        }

        private void HeartbeatEcho(int sequence, string errorKey)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyHeartbeatEcho(this, sequence, errorKey);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        internal ServerCallback(ServerConnectionManager serverConnection)
        {
            _serverConnection = serverConnection;
        }

        public void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyLeaderboardResultReceived(this, operationGetLeaderboardResult);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void SubscribeToChatResult(OperationResult chatResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyChatSubscriptionResult(this, chatResult);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void SendChatMessageResult(OperationSendChatMessageResult chatResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyChatSendResult(this, chatResult);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void OnChatMessageReceived(ChatMessage chatResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyChatMessageReceived(this, chatResult);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyMatchSubscriptionResult(this, operationSubscribeToMatchResult);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void TryCatchFugitiveKittenResult(OperationResult result)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyFugitiveKittenResult(this, result, true);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void CheckFugitiveKittenDeadlineResult(OperationResult result)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyFugitiveKittenResult(this, result, false);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void PlaceCardResult(OperationResult operationPlaceCardResult)
        {
            DispatchMatchActionResult(operationPlaceCardResult);
        }

        public void PlaceBetResult(OperationResult operationPlaceBetResult)
        {
            DispatchMatchActionResult(operationPlaceBetResult);
        }

        public void WithdrawFromBetResult(OperationResult operationWithdrawFromBetResult)
        {
            DispatchMatchActionResult(operationWithdrawFromBetResult);
        }

        public void RevealCardResult(OperationResult operationRevealCardResult)
        {
            DispatchMatchActionResult(operationRevealCardResult);
        }

        public void ChoosePunishmentCardResult(OperationResult operationChoosePunishmentCardResult)
        {
            DispatchMatchActionResult(operationChoosePunishmentCardResult);
        }

        private void DispatchMatchActionResult(OperationResult result)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyMatchActionResult(this, result);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void OnMatchViewChanged(MatchView matchView)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyMatchViewReceived(this, matchView);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void SubscribeToFriendsResult(OperationSubscribeToFriendsResult operationSubscribeToFriendsResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyFriendListReceived(operationSubscribeToFriendsResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void InviteFriendToRoomResult(OperationResult operationInviteFriendToRoomResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyInviteFriendResultReceived(operationInviteFriendToRoomResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void AcceptRoomInvitationResult(OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyAcceptInvitationResultReceived(operationAcceptRoomInvitationResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void OnFriendPresenceChanged(Friend friend)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyFriendPresenceChanged(friend);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void OnRoomInvitationReceived(RoomInvitation roomInvitation)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyRoomInvitationReceived(roomInvitation);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void OnFriendRequestReceived(FriendRequest friendRequest)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyFriendRequestReceived(friendRequest);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void OnFriendAdded(Friend friend)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyFriendAdded(friend);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void SendFriendRequestResult(
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifySendFriendRequestResultReceived(operationSendFriendRequestResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void AcceptFriendRequestResult(
            OperationAcceptFriendRequestResult operationAcceptFriendRequestResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyAcceptFriendRequestResultReceived(operationAcceptFriendRequestResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void DeclineFriendRequestResult(
            OperationDeclineFriendRequestResult operationDeclineFriendRequestResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyDeclineFriendRequestResultReceived(operationDeclineFriendRequestResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void ChangeCardsPerPlayerResult(OperationLobbyRoomResult result)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyChangeCardsPerPlayerResultReceived(result);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void CreateRoomResult(OperationLobbyRoomResult operationCreateRoomResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyCreateRoomResultReceived(operationCreateRoomResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void EnterLobbyResult(OperationLobbyRoomResult operationEnterLobbyResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyEnterLobbyResultReceived(operationEnterLobbyResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void LeaveLobbyResult(OperationResult operationLeaveLobbyResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyLeaveLobbyResultReceived(operationLeaveLobbyResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void OnPlayerExpelled(int roomId)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyPlayerExpelled(this, roomId);
            }

            _serverConnection.RunOnInterface(DeliverNotification);
        }

        public void OnLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyLobbyRoomChanged(lobbyRoom);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void ChangePlayerReadinessResult(OperationLobbyRoomResult changePlayerReadinessResult)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyReadinessResultReceived(changePlayerReadinessResult);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }

        public void RequestMatchStartResult(OperationRequestMatchStartResult result)
        {
            void DeliverNotification()
            {
                _serverConnection.NotifyMatchStartResultReceived(result);
            }

            _serverConnection.RunOnInterfaceIfCurrent(this, DeliverNotification);
        }
    }
}