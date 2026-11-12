using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        private const int LobbyStartCheckRetrySeconds = 1;
        private const int ZeroCount = 0;

        /// <summary>Cambia el estado Listo y prepara el resultado y los avisos fuera del candado.</summary>
        /// <param name="caller">Token de sesión y canal de lobby del jugador.</param>
        /// <param name="lobbyRequest">Sala y estado deseado; null se rechaza.</param>
        /// <returns>Resultado de la petición y avisos de estado.</returns>
        public LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult> ChangePlayerReadiness(
            LobbyCaller caller,
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

            OperationChangePlayerReadinessResult operationChangePlayerReadinessResult =
                new OperationChangePlayerReadinessResult();
            ApplyOutcome(operationChangePlayerReadinessResult, lobbyMembershipChange);
            operationChangePlayerReadinessResult.Room = ConvertRoomOrNull(lobbyMembershipChange);

            if (!operationChangePlayerReadinessResult.IsSuccess)
            {
                LogRejectedOperation("cambiar el estado Listo", lobbyMembershipChange, requestedRoomId);
            }

            LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult> changePlayerReadinessResult =
                new LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult>(
                    operationChangePlayerReadinessResult,
                    CreateNotifications(lobbyMembershipChange));

            return changePlayerReadinessResult;
        }

        /// <summary>
        /// Valida el inicio con el reloj del registro; una consulta anticipada solo indica reintento.
        /// </summary>
        /// <param name="caller">Token de sesión y canal de lobby del jugador.</param>
        /// <param name="matchStartRequest">Sala y generación de cuenta observada; null se rechaza.</param>
        /// <returns>Estado actual, reintento si hay cuenta y avisos si se autorizó la partida.</returns>
        public LobbyOperationResultWithNotification<OperationRequestMatchStartResult> RequestMatchStart(
            LobbyCaller caller,
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
                    "El jugador {0} autorizó el inicio de la partida {1} en la sala {2}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.Room.Start.MatchId,
                    lobbyMembershipChange.RoomId);
            }

            LobbyOperationResultWithNotification<OperationRequestMatchStartResult> requestMatchStartResult =
                new LobbyOperationResultWithNotification<OperationRequestMatchStartResult>(
                    operationRequestMatchStartResult,
                    CreateNotifications(lobbyMembershipChange));

            return requestMatchStartResult;
        }
    }
}