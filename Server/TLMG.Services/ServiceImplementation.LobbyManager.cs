using System;
using System.Collections.Generic;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;
using TLMG.Services.Helpers;

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

        /// <summary>
        /// Atiende la petición de cambiar el estado Listo con red de seguridad en el punto de entrada.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="request">Datos de la petición.</param>
        public void ChangePlayerReadiness(string sessionToken, OperationChangePlayerReadinessRequest request)
        {
            ILobbyManagerCallback callback = null;
            LobbyOutcome<OperationChangePlayerReadinessResult> outcome;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();
                outcome = _lobbyService.ChangePlayerReadiness(sessionToken, _lobbyChannelId, request);
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al cambiar el estado Listo.", exception);
                outcome = new LobbyOutcome<OperationChangePlayerReadinessResult>(
                    new OperationChangePlayerReadinessResult
                    {
                        ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey
                    });
            }

            if (callback != null)
            {
                try
                {
                    SafeNotify(() => callback.ChangePlayerReadinessResult(outcome.Result));
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar el resultado de cambiar el estado Listo.", exception);
                }
            }

            foreach (LobbyNotification notification in outcome.Notifications)
            {
                try
                {
                    SafeNotify(() => notification.Callback.OnLobbyRoomChanged(notification.Room));
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar un aviso de estado de sala.", exception);
                }
            }
        }

        /// <summary>
        /// Atiende la petición de solicitar el inicio de la partida con red de seguridad en el punto de entrada.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="request">Datos de la petición.</param>
        public void RequestMatchStart(string sessionToken, OperationRequestMatchStartRequest request)
        {
            ILobbyManagerCallback callback = null;
            LobbyOutcome<OperationRequestMatchStartResult> outcome;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                ObserveLobbyChannel();
                outcome = _lobbyService.RequestMatchStart(sessionToken, _lobbyChannelId, request);
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al solicitar el inicio de la partida.", exception);
                outcome = new LobbyOutcome<OperationRequestMatchStartResult>(
                    new OperationRequestMatchStartResult
                    {
                        ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey
                    });
            }

            if (callback != null)
            {
                try
                {
                    SafeNotify(() => callback.RequestMatchStartResult(outcome.Result));
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar el resultado de solicitar el inicio de la partida.",
                        exception);
                }
            }

            foreach (LobbyNotification notification in outcome.Notifications)
            {
                try
                {
                    SafeNotify(() => notification.Callback.OnLobbyRoomChanged(notification.Room));
                }
                catch (Exception exception)
                {
                    _log.Error("Error inesperado al entregar un aviso de estado de sala.", exception);
                }
            }
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