using TLMG.Contracts.DataContracts;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        private const int LobbyStartCheckRetrySeconds = 1;
        private const int ZeroCount = 0;

        public ResultWithNotification<OperationLobbyRoomResult> ChangePlayerReadiness(
            SessionChannelCaller caller,
            OperationChangePlayerReadinessRequest lobbyRequest)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int requestedRoomId = LobbyMembershipChange.NoRoomId;

            if (lobbyRequest == null)
            {
                lobbyMembershipChange = CreateInvalidSessionChange();
            }
            else
            {
                requestedRoomId = lobbyRequest.RoomId;
                lobbyMembershipChange =
                    _sessionRegistry.ChangePlayerReadiness(caller, requestedRoomId, lobbyRequest.IsReady);
            }

            OperationLobbyRoomResult operationChangePlayerReadinessResult =
                new OperationLobbyRoomResult();
            ApplyOutcome(operationChangePlayerReadinessResult, lobbyMembershipChange);
            operationChangePlayerReadinessResult.Room = ConvertRoomOrNull(lobbyMembershipChange);

            if (!operationChangePlayerReadinessResult.IsSuccess)
            {
                LogRejectedOperation("cambiar el estado Listo", lobbyMembershipChange, requestedRoomId);
            }

            ResultWithNotification<OperationLobbyRoomResult> changePlayerReadinessResult =
                new ResultWithNotification<OperationLobbyRoomResult>(
                    operationChangePlayerReadinessResult,
                    SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange));

            return changePlayerReadinessResult;
        }

        public ResultWithNotification<OperationRequestMatchStartResult> RequestMatchStart(
            SessionChannelCaller caller,
            OperationMatchStartRequest matchStartRequest)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int requestedRoomId = LobbyMembershipChange.NoRoomId;

            if (matchStartRequest == null)
            {
                lobbyMembershipChange = CreateInvalidSessionChange();
            }
            else
            {
                requestedRoomId = matchStartRequest.RoomId;
                lobbyMembershipChange =
                    _sessionRegistry.RequestMatchStart(caller, requestedRoomId, matchStartRequest.CountdownId);
            }

            OperationRequestMatchStartResult operationRequestMatchStartResult =
                new OperationRequestMatchStartResult();
            ApplyOutcome(operationRequestMatchStartResult, lobbyMembershipChange);
            operationRequestMatchStartResult.Room = ConvertRoomOrNull(lobbyMembershipChange);

            if (operationRequestMatchStartResult.Room != null
                && operationRequestMatchStartResult.Room.CountdownEndsAtUtc.HasValue)
            {
                operationRequestMatchStartResult.RetryAfterSeconds = LobbyStartCheckRetrySeconds;
            }

            if (!operationRequestMatchStartResult.IsSuccess)
            {
                LogRejectedOperation("solicitar el inicio de la partida", lobbyMembershipChange, requestedRoomId);
            }

            if (lobbyMembershipChange.RecipientCallbacks.Count > ZeroCount)
            {
                _log.InfoFormat(
                    "El jugador {0} autorizó el inicio de la partida {1} en la sala {2}: {3} cartas, {4} jugadores.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.Room.Start.MatchId,
                    lobbyMembershipChange.RoomId,
                    lobbyMembershipChange.Room.Start.CardsPerPlayerCount,
                    lobbyMembershipChange.Room.Players.Count);
            }

            ResultWithNotification<OperationRequestMatchStartResult> requestMatchStartResult =
                new ResultWithNotification<OperationRequestMatchStartResult>(
                    operationRequestMatchStartResult,
                    SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange));

            return requestMatchStartResult;
        }
    }
}