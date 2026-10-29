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

            LobbyOutcome<OperationCreateRoomResult> lobbyOutcome;

            try
            {
                lobbyOutcome = _lobbyService.CreateRoom(sessionToken, _lobbyChannelId, callback);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al crear una sala.", ex);
                lobbyOutcome = new LobbyOutcome<OperationCreateRoomResult>(
                    new OperationCreateRoomResult { ErrorKey = LobbyService.UnexpectedErrorKey });
            }

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

            LobbyOutcome<OperationEnterLobbyResult> lobbyOutcome;

            try
            {
                lobbyOutcome = _lobbyService.EnterLobby(sessionToken, roomId, _lobbyChannelId, callback);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al entrar a una sala.", ex);
                lobbyOutcome = new LobbyOutcome<OperationEnterLobbyResult>(
                    new OperationEnterLobbyResult { ErrorKey = LobbyService.UnexpectedErrorKey });
            }

            SafeNotify(() => callback.EnterLobbyResult(lobbyOutcome.Result));
            NotifyLobbyMembers(lobbyOutcome.Notifications);
        }

        /// <summary>Delega la salida de la sala al servicio de salas y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        public void LeaveLobby(string sessionToken)
        {
            ILobbyManagerCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            ObserveLobbyChannel();

            LobbyOutcome<OperationLeaveLobbyResult> lobbyOutcome;

            try
            {
                lobbyOutcome = _lobbyService.LeaveLobby(sessionToken);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al salir de una sala.", ex);
                lobbyOutcome = new LobbyOutcome<OperationLeaveLobbyResult>(
                    new OperationLeaveLobbyResult { ErrorKey = LobbyService.UnexpectedErrorKey });
            }

            SafeNotify(() => callback.LeaveLobbyResult(lobbyOutcome.Result));
            NotifyLobbyMembers(lobbyOutcome.Notifications);
        }

        // El canal vive tanto como esta instancia (PerSession), así que sus eventos no se desuscriben: el canal que
        // los publica termina antes que cualquier otro objeto que pudiera quedar referenciado.
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

        // Se ejecuta en un hilo del canal y nadie recibe un resultado: una excepción sin capturar aquí tumbaría al
        // servidor, así que este límite del sistema registra el fallo y no lo relanza.
        private void RemovePlayerOfLobbyChannel()
        {
            try
            {
                NotifyLobbyMembers(_lobbyService.LeaveLobbyByChannel(_lobbyChannelId));
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al sacar de la sala al jugador de un canal de lobby cerrado.", ex);
            }
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