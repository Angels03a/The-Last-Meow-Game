using System;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager
    {
        /// <summary>Entrega el resultado de pedir un cambio de preparación.</summary>
        public event EventHandler<ServerEventArgs<OperationChangePlayerReadinessResult>> ReadinessResultReceived;

        /// <summary>Entrega el resultado de comprobar el inicio; IsSuccess no autoriza navegar.</summary>
        public event EventHandler<ServerEventArgs<OperationRequestMatchStartResult>> MatchStartResultReceived;

        /// <summary>Cuenta proyectada desde el plazo del servidor; vive mientras la sesión está abierta.</summary>
        public LobbyCountdown Countdown { get; } = new LobbyCountdown();

        /// <summary>Pide el estado deseado sin modificar la foto ni distinguir jugadores por tipo.</summary>
        /// <param name="isReadyToStartMatch">Estado Listo deseado.</param>
        /// <returns>true si se envió la petición por el canal actual.</returns>
        public bool ChangePlayerReadiness(bool isReadyToStartMatch)
        {
            bool wasChangePlayerReadinessSent = false;
            bool hasLobbyChannel = CurrentRoom != null && _lobbyClient != null;

            if (hasLobbyChannel)
            {
                OperationChangePlayerReadinessRequest changePlayerReadinessRequest = new OperationChangePlayerReadinessRequest
                {
                    RoomId = CurrentRoom.RoomId,
                    IsReady = isReadyToStartMatch
                };
                wasChangePlayerReadinessSent = TrySend(
                    () => _lobbyClient.ChangePlayerReadiness(_sessionToken, changePlayerReadinessRequest),
                    "cambiar el estado Listo");
            }

            return wasChangePlayerReadinessSent;
        }

        /// <summary>Publica el resultado y canaliza su foto por el mismo filtro autoritativo de versiones.</summary>
        /// <param name="changePlayerReadinessResult">Resultado del servidor.</param>
        internal void NotifyReadinessResultReceived(OperationChangePlayerReadinessResult changePlayerReadinessResult)
        {
            if (changePlayerReadinessResult != null)
            {
                NotifyLobbyRoomChanged(changePlayerReadinessResult.Room);
                ReadinessResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationChangePlayerReadinessResult>(changePlayerReadinessResult));
            }
        }

        /// <summary>Publica la foto y programa el único reintento permitido por generación.</summary>
        /// <param name="requestMatchStartResult">Resultado del servidor; puede contener un plazo aún no vencido.</param>
        internal void NotifyMatchStartResultReceived(OperationRequestMatchStartResult requestMatchStartResult)
        {
            if (requestMatchStartResult != null)
            {
                NotifyLobbyRoomChanged(requestMatchStartResult.Room);
                Countdown.ApplyMatchStartResult(requestMatchStartResult);
                MatchStartResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationRequestMatchStartResult>(requestMatchStartResult));
            }
        }

        private void OnCountdownStartRequested(object sender, ServerEventArgs<long> eventArguments)
        {
            bool hasLobbyChannel = CurrentRoom != null && _lobbyClient != null;

            if (hasLobbyChannel)
            {
                bool isCurrentCountdown = CurrentRoom.CountdownId == eventArguments.Payload;

                if (isCurrentCountdown && !CurrentRoom.InGame)
                {
                    OperationRequestMatchStartRequest requestMatchStartResult = new OperationRequestMatchStartRequest
                    {
                        RoomId = CurrentRoom.RoomId,
                        CountdownId = eventArguments.Payload
                    };
                    TrySend(
                        () => _lobbyClient.RequestMatchStart(_sessionToken, requestMatchStartResult),
                        "solicitar el inicio autorizado de partida");
                }
            }
        }

        private bool IsNewerRoomSnapshot(LobbyRoom lobbyRoom)
        {
            bool isNewer = lobbyRoom != null;

            if (IsSameRoom(CurrentRoom, lobbyRoom))
            {
                isNewer = lobbyRoom.Version > CurrentRoom.Version;
            }

            return isNewer;
        }

        private void ApplyRoomSnapshot(LobbyRoom lobbyRoom)
        {
            if (IsNewerRoomSnapshot(lobbyRoom))
            {
                CurrentRoom = lobbyRoom;
                Countdown.ApplyRoom(lobbyRoom);
                OpenChatForCurrentRoom();
                LobbyRoomChanged?.Invoke(this, new ServerEventArgs<LobbyRoom>(lobbyRoom));
            }
        }
    }
}
