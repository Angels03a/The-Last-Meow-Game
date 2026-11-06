using System;
using System.Collections.Generic;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILobbyManager
    {
        private static readonly LobbyService _lobbyService;

        private bool _isLobbyChannelObserved;
        private string _lobbyChannelId;

        /// <summary>Delega la creación de la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        public void CreateRoom(string sessionToken)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOperationResultWithNotification<OperationCreateRoomResult> createRoomResult =
                _lobbyService.CreateRoom(sessionToken, _lobbyChannelId, callback);

            SafeNotify(() => callback.CreateRoomResult(createRoomResult.OperationResult));
            NotifyLobbyMembers(createRoomResult.Notifications);
        }

        /// <summary>Delega la entrada a la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="roomId">Identificador de la sala a la que quiere entrar.</param>
        public void EnterLobby(string sessionToken, int roomId)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOperationResultWithNotification<OperationEnterLobbyResult> enterLobbyResult =
                _lobbyService.EnterLobby(sessionToken, roomId, _lobbyChannelId, callback);

            SafeNotify(() => callback.EnterLobbyResult(enterLobbyResult.OperationResult));
            NotifyLobbyMembers(enterLobbyResult.Notifications);
        }

        /// <summary>Delega la salida de la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        public void LeaveLobby(string sessionToken)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOperationResultWithNotification<OperationLeaveLobbyResult> leaveLobbyResult =
                _lobbyService.LeaveLobby(sessionToken);

            SafeNotify(() => callback.LeaveLobbyResult(leaveLobbyResult.OperationResult));
            NotifyLobbyMembers(leaveLobbyResult.Notifications);
        }

        /// <summary>
        /// Atiende la petición de cambiar el estado Listo delegando al servicio de salas.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="changePlayerReadinessRequest">Datos de la petición.</param>
        public void ChangePlayerReadiness(string sessionToken, OperationChangePlayerReadinessRequest changePlayerReadinessRequest)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult> changePlayerReadinessResult =
                _lobbyService.ChangePlayerReadiness(sessionToken, _lobbyChannelId, changePlayerReadinessRequest);

            SafeNotify(() => callback.ChangePlayerReadinessResult(changePlayerReadinessResult.OperationResult));
            NotifyLobbyMembers(changePlayerReadinessResult.Notifications);
        }

        /// <summary>
        /// Atiende la petición de solicitar el inicio de la partida delegando al servicio de salas.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="matchStartRequest">Datos de la petición.</param>
        public void RequestMatchStart(string sessionToken, OperationMatchStartRequest matchStartRequest)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOperationResultWithNotification<OperationRequestMatchStartResult> requestMatchStartResult =
                _lobbyService.RequestMatchStart(sessionToken, _lobbyChannelId, matchStartRequest);

            SafeNotify(() => callback.RequestMatchStartResult(requestMatchStartResult.OperationResult));
            NotifyLobbyMembers(requestMatchStartResult.Notifications);
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