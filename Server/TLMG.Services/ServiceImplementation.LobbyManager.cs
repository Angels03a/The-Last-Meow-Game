using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILobbyManager
    {
        private bool _isLobbyChannelObserved;
        private string _lobbyChannelId;

        public void CreateRoom(string sessionToken)
        {
            LogChannelOperation("Lobby", "CreateRoom", sessionToken);
            ILobbyManagerCallback callback = GetLobbyCallback();

            ResultWithNotification<OperationLobbyRoomResult> createRoomResult =
                _lobbyService.CreateRoom(CreateLobbyCaller(sessionToken), callback);

            SafeNotify(() => callback.CreateRoomResult(createRoomResult.OperationResult));
            SafeNotifyAll(createRoomResult.Notifications);
        }

        public void EnterLobby(string sessionToken, int roomId)
        {
            SafeNotifyAll(_sessionRegistry.EvaluateConnectionDeadlines());
            LogChannelOperation("Lobby", "EnterLobby", sessionToken);
            ILobbyManagerCallback callback = GetLobbyCallback();

            ResultWithNotification<OperationLobbyRoomResult> enterLobbyResult =
                _lobbyService.EnterLobby(CreateLobbyCaller(sessionToken), roomId, callback);

            SafeNotify(() => callback.EnterLobbyResult(enterLobbyResult.OperationResult));
            SafeNotifyAll(enterLobbyResult.Notifications);
        }

        public void LeaveLobby(string sessionToken)
        {
            LogChannelOperation("Lobby", "LeaveLobby", sessionToken);
            ILobbyManagerCallback callback = GetLobbyCallback();

            ResultWithNotification<OperationResult> leaveLobbyResult =
                _lobbyService.LeaveLobby(sessionToken);

            SafeNotify(() => callback.LeaveLobbyResult(leaveLobbyResult.OperationResult));
            SafeNotifyAll(leaveLobbyResult.Notifications);
        }

        public void ChangePlayerReadiness(
            string sessionToken,
            OperationChangePlayerReadinessRequest changePlayerReadinessRequest)
        {
            LogChannelOperation("Lobby", "ChangePlayerReadiness", sessionToken);
            ILobbyManagerCallback callback = GetLobbyCallback();

            ResultWithNotification<OperationLobbyRoomResult> changePlayerReadinessResult =
                _lobbyService.ChangePlayerReadiness(CreateLobbyCaller(sessionToken), changePlayerReadinessRequest);

            SafeNotify(() => callback.ChangePlayerReadinessResult(changePlayerReadinessResult.OperationResult));
            SafeNotifyAll(changePlayerReadinessResult.Notifications);
        }

        public void RequestMatchStart(string sessionToken, OperationMatchStartRequest matchStartRequest)
        {
            LogChannelOperation("Lobby", "RequestMatchStart", sessionToken);
            ILobbyManagerCallback callback = GetLobbyCallback();

            ResultWithNotification<OperationRequestMatchStartResult> requestMatchStartResult =
                _lobbyService.RequestMatchStart(CreateLobbyCaller(sessionToken), matchStartRequest);

            SafeNotify(() => callback.RequestMatchStartResult(requestMatchStartResult.OperationResult));
            SafeNotifyAll(requestMatchStartResult.Notifications);
        }

        public void ChangeCardsPerPlayer(
            string sessionToken,
            OperationChangeCardsPerPlayerRequest changeCardsPerPlayerRequest)
        {
            LogChannelOperation("Lobby", "ChangeCardsPerPlayer", sessionToken);
            ILobbyManagerCallback callback = GetLobbyCallback();

            ResultWithNotification<OperationLobbyRoomResult>
                changeCardsPerPlayerResult = _lobbyService.ChangeCardsPerPlayer(
                    CreateLobbyCaller(sessionToken), changeCardsPerPlayerRequest);

            SafeNotify(() => callback.ChangeCardsPerPlayerResult(changeCardsPerPlayerResult.OperationResult));
            SafeNotifyAll(changeCardsPerPlayerResult.Notifications);
        }

        private ILobbyManagerCallback GetLobbyCallback()
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            return callback;
        }

        private LobbyCaller CreateLobbyCaller(string sessionToken)
        {
            LobbyCaller lobbyCaller = new LobbyCaller(sessionToken, _lobbyChannelId);

            return lobbyCaller;
        }

        private void ObserveLobbyChannel()
        {
            if (!_isLobbyChannelObserved)
            {
                IContextChannel lobbyChannel = OperationContext.Current.Channel;
                _lobbyChannelId = lobbyChannel.SessionId;
                lobbyChannel.Closed += OnLobbyChannelClosed;
                lobbyChannel.Faulted += OnLobbyChannelFaulted;
                _isLobbyChannelObserved = true;
            }
        }

        private void OnLobbyChannelClosed(object sender, EventArgs eventArguments)
        {
            LogChannelEnd("Lobby", "Closed", sender);
            OnLobbyChannelEnded(sender, eventArguments);
        }

        private void OnLobbyChannelFaulted(object sender, EventArgs eventArguments)
        {
            LogChannelEnd("Lobby", "Faulted", sender);
            OnLobbyChannelEnded(sender, eventArguments);
        }

        private void OnLobbyChannelEnded(object sender, EventArgs eventArguments)
        {
            RemovePlayerOfLobbyChannel();
        }

        private void RemovePlayerOfLobbyChannel()
        {
            SafeNotifyAll(_lobbyService.LeaveLobbyByChannel(_lobbyChannelId));
        }
        void ILobbyManager.LobbyHeartbeat(string sessionToken, int sequence, int confirmedSequence)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            SafeNotifyAll(_sessionRegistry.ProcessHeartbeat(sessionToken, _lobbyChannelId, HeartbeatChannel.Lobby,
                sequence, confirmedSequence, callback.LobbyHeartbeatEcho));
        }

    }
}