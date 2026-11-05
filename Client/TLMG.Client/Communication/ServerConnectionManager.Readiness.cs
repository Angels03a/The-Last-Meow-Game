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
        /// <param name="isReady">Estado Listo deseado.</param>
        /// <returns>true si se envió la petición por el canal actual.</returns>
        public bool ChangePlayerReadiness(bool isReady)
        {
            bool wasSent = false;
            bool hasLobbyChannel = CurrentRoom != null && _lobbyClient != null;

            if (hasLobbyChannel)
            {
                OperationChangePlayerReadinessRequest request = new OperationChangePlayerReadinessRequest
                {
                    RoomId = CurrentRoom.RoomId,
                    IsReady = isReady
                };
                wasSent = TrySend(
                    () => _lobbyClient.ChangePlayerReadiness(_sessionToken, request),
                    "cambiar el estado Listo");
            }

            return wasSent;
        }

        /// <summary>Publica el resultado y canaliza su foto por el mismo filtro autoritativo de versiones.</summary>
        /// <param name="result">Resultado del servidor.</param>
        internal void NotifyReadinessResultReceived(OperationChangePlayerReadinessResult result)
        {
            if (result != null)
            {
                NotifyLobbyRoomChanged(result.Room);
                ReadinessResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationChangePlayerReadinessResult>(result));
            }
        }

        /// <summary>Publica la foto y programa el único reintento permitido por generación.</summary>
        /// <param name="result">Resultado del servidor; puede contener un plazo aún no vencido.</param>
        internal void NotifyMatchStartResultReceived(OperationRequestMatchStartResult result)
        {
            if (result != null)
            {
                NotifyLobbyRoomChanged(result.Room);
                Countdown.ApplyStartResult(result);
                MatchStartResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationRequestMatchStartResult>(result));
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
                    OperationRequestMatchStartRequest request = new OperationRequestMatchStartRequest
                    {
                        RoomId = CurrentRoom.RoomId,
                        CountdownId = eventArguments.Payload
                    };
                    TrySend(
                        () => _lobbyClient.RequestMatchStart(_sessionToken, request),
                        "solicitar el inicio autorizado de partida");
                }
            }
        }

        private bool IsNewerRoomSnapshot(LobbyRoom room)
        {
            bool isNewer = room != null;

            if (IsSameRoom(CurrentRoom, room))
            {
                isNewer = room.Version > CurrentRoom.Version;
            }

            return isNewer;
        }

        private void ApplyRoomSnapshot(LobbyRoom room)
        {
            if (IsNewerRoomSnapshot(room))
            {
                CurrentRoom = room;
                Countdown.ApplyRoom(room);
                LobbyRoomChanged?.Invoke(this, new ServerEventArgs<LobbyRoom>(room));
            }
        }
    }
}
