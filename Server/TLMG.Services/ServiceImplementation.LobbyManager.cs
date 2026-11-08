using System;
using System.Collections.Generic;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Lobby;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILobbyManager
    {
        private bool _isLobbyChannelObserved;
        private string _lobbyChannelId;

        public void CreateRoom(string sessionToken)
        {
            try
            {
                ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();

                LobbyOperationResultWithNotification<OperationCreateRoomResult> createRoomResult =
                    _lobbyService.CreateRoom(sessionToken, _lobbyChannelId, callback);

                SafeNotify(() => callback.CreateRoomResult(createRoomResult.OperationResult));
                NotifyLobbyMembers(createRoomResult.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante CreateRoom.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void EnterLobby(string sessionToken, int roomId)
        {
            try
            {
                ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();

                LobbyOperationResultWithNotification<OperationEnterLobbyResult> enterLobbyResult =
                    _lobbyService.EnterLobby(sessionToken, roomId, _lobbyChannelId, callback);

                SafeNotify(() => callback.EnterLobbyResult(enterLobbyResult.OperationResult));
                NotifyLobbyMembers(enterLobbyResult.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante EnterLobby.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void LeaveLobby(string sessionToken)
        {
            try
            {
                ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();

                LobbyOperationResultWithNotification<OperationLeaveLobbyResult> leaveLobbyResult =
                    _lobbyService.LeaveLobby(sessionToken);

                SafeNotify(() => callback.LeaveLobbyResult(leaveLobbyResult.OperationResult));
                NotifyLobbyMembers(leaveLobbyResult.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante LeaveLobby.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void ChangePlayerReadiness(string sessionToken, OperationChangePlayerReadinessRequest 
            changePlayerReadinessRequest)
        {
            try
            {
                ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();

                LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult> changePlayerReadinessResult =
                    _lobbyService.ChangePlayerReadiness(sessionToken, _lobbyChannelId, changePlayerReadinessRequest);

                SafeNotify(() => callback.ChangePlayerReadinessResult(changePlayerReadinessResult.OperationResult));
                NotifyLobbyMembers(changePlayerReadinessResult.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante ChangePlayerReadiness.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }

        public void RequestMatchStart(string sessionToken, OperationMatchStartRequest matchStartRequest)
        {
            try
            {
                ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();

                LobbyOperationResultWithNotification<OperationRequestMatchStartResult> requestMatchStartResult =
                    _lobbyService.RequestMatchStart(sessionToken, _lobbyChannelId, matchStartRequest);

                SafeNotify(() => callback.RequestMatchStartResult(requestMatchStartResult.OperationResult));
                NotifyLobbyMembers(requestMatchStartResult.Notifications);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante RequestMatchStart.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
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