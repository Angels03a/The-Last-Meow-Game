using TLMG.Contracts.DataContracts;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        private const int LobbyStartCheckRetrySeconds = 1;

        /// <summary>Cambia el estado Listo y prepara el resultado y los avisos fuera del candado.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="channelId">Canal de lobby que hace la petición.</param>
        /// <param name="request">Sala y estado deseado; null se rechaza.</param>
        /// <returns>Resultado de la petición y avisos de estado.</returns>
        public LobbyOutcome<OperationChangePlayerReadinessResult> ChangePlayerReadiness(
            string sessionToken,
            string channelId,
            OperationChangePlayerReadinessRequest request)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int requestedRoomId = LobbyMembershipChange.NoRoomId;

            if (request == null)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                    LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);
            }
            else
            {
                requestedRoomId = request.RoomId;
                lobbyMembershipChange = _sessionRegistry.ChangePlayerReadiness(
                    sessionToken, channelId, requestedRoomId, request.IsReady);
            }

            OperationChangePlayerReadinessResult operationChangePlayerReadinessResult =
                new OperationChangePlayerReadinessResult
                {
                    IsSuccess = lobbyMembershipChange.Status == LobbyChangeStatus.Applied
                };

            if (operationChangePlayerReadinessResult.IsSuccess)
            {
                operationChangePlayerReadinessResult.ErrorKey = null;
            }
            else
            {
                operationChangePlayerReadinessResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
            }

            if (lobbyMembershipChange.Room == null)
            {
                operationChangePlayerReadinessResult.Room = null;
            }
            else
            {
                operationChangePlayerReadinessResult.Room =
                    LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
            }

            if (lobbyMembershipChange.Status != LobbyChangeStatus.Applied)
            {
                LogRejectedOperation(
                    "cambiar el estado Listo",
                    lobbyMembershipChange,
                    requestedRoomId);
            }

            LobbyOutcome<OperationChangePlayerReadinessResult> lobbyOutcome =
                new LobbyOutcome<OperationChangePlayerReadinessResult>(
                    operationChangePlayerReadinessResult,
                    CreateNotifications(lobbyMembershipChange));

            return lobbyOutcome;
        }

        /// <summary>
        /// Valida el inicio con el reloj del registro; una consulta anticipada solo indica reintento.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="channelId">Canal de lobby que hace la petición.</param>
        /// <param name="request">Sala y generación de cuenta observada; null se rechaza.</param>
        /// <returns>Estado actual, reintento si hay cuenta y avisos si se autorizó la partida.</returns>
        public LobbyOutcome<OperationRequestMatchStartResult> RequestMatchStart(
            string sessionToken,
            string channelId,
            OperationRequestMatchStartRequest request)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int requestedRoomId = LobbyMembershipChange.NoRoomId;

            if (request == null)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                    LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);
            }
            else
            {
                requestedRoomId = request.RoomId;
                lobbyMembershipChange = _sessionRegistry.RequestMatchStart(
                    sessionToken, channelId, requestedRoomId, request.CountdownId);
            }

            OperationRequestMatchStartResult operationRequestMatchStartResult =
                new OperationRequestMatchStartResult
                {
                    IsSuccess = lobbyMembershipChange.Status == LobbyChangeStatus.Applied
                };

            if (operationRequestMatchStartResult.IsSuccess)
            {
                operationRequestMatchStartResult.ErrorKey = null;
            }
            else
            {
                operationRequestMatchStartResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
            }

            if (lobbyMembershipChange.Room == null)
            {
                operationRequestMatchStartResult.Room = null;
            }
            else
            {
                operationRequestMatchStartResult.Room = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
            }

            if (operationRequestMatchStartResult.Room != null
                && operationRequestMatchStartResult.Room.CountdownEndsAtUtc.HasValue)
            {
                operationRequestMatchStartResult.RetryAfterSeconds = LobbyStartCheckRetrySeconds;
            }

            if (lobbyMembershipChange.Status != LobbyChangeStatus.Applied)
            {
                LogRejectedOperation(
                    "solicitar el inicio de la partida",
                    lobbyMembershipChange,
                    requestedRoomId);
            }

            if (lobbyMembershipChange.RecipientCallbacks.Count > 0)
            {
                _log.InfoFormat(
                    "El jugador {0} autorizó el inicio de la partida {1} en la sala {2}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.Room.Start.MatchId,
                    lobbyMembershipChange.RoomId);
            }

            LobbyOutcome<OperationRequestMatchStartResult> lobbyOutcome =
                new LobbyOutcome<OperationRequestMatchStartResult>(
                    operationRequestMatchStartResult,
                    CreateNotifications(lobbyMembershipChange));

            return lobbyOutcome;
        }
    }
}
