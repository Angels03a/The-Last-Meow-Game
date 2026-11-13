using System;
using System.Collections.Generic;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILobbyManager
    {
        private bool _isLobbyChannelObserved;
        private string _lobbyChannelId;

        public void CreateRoom(string sessionToken)
        {
            ILobbyManagerCallback callback = GetLobbyCallback();

            LobbyOperationResultWithNotification<OperationLobbyRoomResult> createRoomResult =
                _lobbyService.CreateRoom(CreateLobbyCaller(sessionToken), callback);

            SafeNotify(() => callback.CreateRoomResult(createRoomResult.OperationResult));
            NotifyLobbyMembers(createRoomResult.Notifications);
        }

        public void EnterLobby(string sessionToken, int roomId)
        {
            ILobbyManagerCallback callback = GetLobbyCallback();

            LobbyOperationResultWithNotification<OperationLobbyRoomResult> enterLobbyResult =
                _lobbyService.EnterLobby(CreateLobbyCaller(sessionToken), roomId, callback);

            SafeNotify(() => callback.EnterLobbyResult(enterLobbyResult.OperationResult));
            NotifyLobbyMembers(enterLobbyResult.Notifications);
        }

        public void LeaveLobby(string sessionToken)
        {
            ILobbyManagerCallback callback = GetLobbyCallback();

            LobbyOperationResultWithNotification<OperationResult> leaveLobbyResult =
                _lobbyService.LeaveLobby(sessionToken);

            SafeNotify(() => callback.LeaveLobbyResult(leaveLobbyResult.OperationResult));
            NotifyLobbyMembers(leaveLobbyResult.Notifications);
        }

        public void ChangePlayerReadiness(
            string sessionToken,
            OperationChangePlayerReadinessRequest changePlayerReadinessRequest)
        {
            ILobbyManagerCallback callback = GetLobbyCallback();

            LobbyOperationResultWithNotification<OperationLobbyRoomResult> changePlayerReadinessResult =
                _lobbyService.ChangePlayerReadiness(CreateLobbyCaller(sessionToken), changePlayerReadinessRequest);

            SafeNotify(() => callback.ChangePlayerReadinessResult(changePlayerReadinessResult.OperationResult));
            NotifyLobbyMembers(changePlayerReadinessResult.Notifications);
        }

        public void RequestMatchStart(string sessionToken, OperationMatchStartRequest matchStartRequest)
        {
            ILobbyManagerCallback callback = GetLobbyCallback();

            LobbyOperationResultWithNotification<OperationRequestMatchStartResult> requestMatchStartResult =
                _lobbyService.RequestMatchStart(CreateLobbyCaller(sessionToken), matchStartRequest);

            SafeNotify(() => callback.RequestMatchStartResult(requestMatchStartResult.OperationResult));
            NotifyLobbyMembers(requestMatchStartResult.Notifications);
        }

        public void ChangeCardsPerPlayer(
            string sessionToken,
            OperationChangeCardsPerPlayerRequest changeCardsPerPlayerRequest)
        {
            ILobbyManagerCallback callback = GetLobbyCallback();

            LobbyOperationResultWithNotification<OperationLobbyRoomResult>
                changeCardsPerPlayerResult = _lobbyService.ChangeCardsPerPlayer(
                    CreateLobbyCaller(sessionToken), changeCardsPerPlayerRequest);

            SafeNotify(() => callback.ChangeCardsPerPlayerResult(changeCardsPerPlayerResult.OperationResult));
            NotifyLobbyMembers(changeCardsPerPlayerResult.Notifications);
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
            RemovePlayerOfLobbyChannel();
        }

        private void OnLobbyChannelFaulted(object sender, EventArgs eventArguments)
        {
            RemovePlayerOfLobbyChannel();
        }

        private void RemovePlayerOfLobbyChannel()
        {
            NotifyLobbyMembers(_lobbyService.LeaveLobbyByChannel(_lobbyChannelId));
        }

        private void NotifyLobbyMembers(IReadOnlyList<LobbyChangeNotification> lobbyNotifications)
        {
            foreach (LobbyChangeNotification lobbyNotification in lobbyNotifications)
            {
                SafeNotify(() => lobbyNotification.Callback.OnLobbyRoomChanged(lobbyNotification.LobbyRoom));
            }
        }
    }
}