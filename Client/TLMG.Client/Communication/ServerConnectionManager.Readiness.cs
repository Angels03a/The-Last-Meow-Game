using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager
    {
        /// <summary>Delivers the result of a readiness change request.</summary>
        public event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> ReadinessResultReceived;

        /// <summary>Delivers the match start check. IsSuccess alone does not allow navigation.</summary>
        public event EventHandler<ServerEventArgs<OperationRequestMatchStartResult>> MatchStartResultReceived;

        /// <summary>Displays the countdown from the server deadline while the session is open.</summary>
        public LobbyCountdown Countdown { get; } = new LobbyCountdown();

        /// <summary>
        /// Requests the desired ready state without changing the snapshot or treating player types differently.
        /// </summary>
        /// <param name="isReadyToStartMatch">Requested ready state.</param>
        /// <returns>True if the request was sent through the current channel.</returns>
        public bool ChangePlayerReadiness(bool isReadyToStartMatch)
        {
            bool wasChangePlayerReadinessSent = false;
            bool hasLobbyChannel = CurrentRoom != null && _lobbyClient != null;

            if (hasLobbyChannel)
            {
                OperationChangePlayerReadinessRequest changePlayerReadinessRequest =
                    new OperationChangePlayerReadinessRequest
                    {
                        RoomId = CurrentRoom.RoomId,
                        IsReady = isReadyToStartMatch
                    };
                wasChangePlayerReadinessSent = TrySend(
                    () => _lobbyClient.ChangePlayerReadiness(_sessionToken, changePlayerReadinessRequest),
                    "cambiar el estado Listo", CloseLobbyChannel);
            }

            return wasChangePlayerReadinessSent;
        }

        /// <summary>Publishes the result and checks its snapshot with the same server version filter.</summary>
        /// <param name="changePlayerReadinessResult">Server result.</param>
        internal void NotifyReadinessResultReceived(OperationLobbyRoomResult changePlayerReadinessResult)
        {
            if (changePlayerReadinessResult != null)
            {
                NotifyLobbyRoomChanged(changePlayerReadinessResult.Room);
                ReadinessResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationLobbyRoomResult>(changePlayerReadinessResult));
            }
        }

        /// <summary>Publishes the snapshot and schedules the single retry allowed per generation.</summary>
        /// <param name="requestMatchStartResult">
        /// Server result, which may include a deadline that has not passed.
        /// </param>
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
                    OperationMatchStartRequest requestMatchStartResult = new OperationMatchStartRequest
                    {
                        RoomId = CurrentRoom.RoomId,
                        CountdownId = eventArguments.Payload
                    };
                    TrySend(
                        () => _lobbyClient.RequestMatchStart(_sessionToken, requestMatchStartResult),
                        "solicitar el inicio autorizado de partida", CloseLobbyChannel);
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

        private void ApplyRoomSnapshot(LobbyRoom lobbyRoom, bool publish = true)
        {
            if (IsNewerRoomSnapshot(lobbyRoom))
            {
                if (!IsSameRoom(CurrentRoom, lobbyRoom))
                {
                    _chatRecoveryAttempts = 0;
                }
                CurrentRoom = lobbyRoom;
                Countdown.ApplyRoom(lobbyRoom);
                if (publish)
                {
                    LobbyRoomChanged?.Invoke(this, new ServerEventArgs<LobbyRoom>(lobbyRoom));
                }
            }
        }
    }
}