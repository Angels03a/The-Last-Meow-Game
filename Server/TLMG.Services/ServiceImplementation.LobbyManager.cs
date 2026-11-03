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
        // Se asigna en el constructor estático de ServiceImplementation.cs porque depende del registro de sesiones.
        private static readonly LobbyService _lobbyService;

        private bool _isLobbyChannelObserved;
        private string _lobbyChannelId;

        /// <summary>Delega la creación de la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        public void CreateRoom(string sessionToken)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOutcome<OperationCreateRoomResult> lobbyOutcome =
                _lobbyService.CreateRoom(sessionToken, _lobbyChannelId, callback);

            SafeNotify(() => callback.CreateRoomResult(lobbyOutcome.Result));
            NotifyLobbyMembers(lobbyOutcome.Notifications);
        }

        /// <summary>Delega la entrada a la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="roomId">Identificador de la sala a la que quiere entrar.</param>
        public void EnterLobby(string sessionToken, int roomId)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOutcome<OperationEnterLobbyResult> lobbyOutcome =
                _lobbyService.EnterLobby(sessionToken, roomId, _lobbyChannelId, callback);

            SafeNotify(() => callback.EnterLobbyResult(lobbyOutcome.Result));
            NotifyLobbyMembers(lobbyOutcome.Notifications);
        }

        /// <summary>Delega la salida de la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        public void LeaveLobby(string sessionToken)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOutcome<OperationLeaveLobbyResult> lobbyOutcome = _lobbyService.LeaveLobby(sessionToken);

            SafeNotify(() => callback.LeaveLobbyResult(lobbyOutcome.Result));
            NotifyLobbyMembers(lobbyOutcome.Notifications);
        }

        // No se desuscriben porque el canal (quien publica los eventos) y esta instancia (quien los atiende) viven
        // lo mismo: ambos terminan con la sesión (PerSession).
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

        private void NotifyLobbyMembers(IReadOnlyList<LobbyNotification> lobbyNotifications)
        {
            foreach (LobbyNotification lobbyNotification in lobbyNotifications)
            {
                SafeNotify(() => lobbyNotification.Callback.OnLobbyRoomChanged(lobbyNotification.Room));
            }
        }
    }
}