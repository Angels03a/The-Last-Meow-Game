using TLMG.Contracts.DataContracts;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        private const int LobbyStartCheckRetrySeconds = 1;
        private const int ZeroCount = 0;

        /// <summary>Cambia el estado Listo y prepara el resultado y los avisos fuera del candado.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="channelId">Canal de lobby que hace la petición.</param>
        /// <param name="lobbyRequest">Sala y estado deseado; null se rechaza.</param>
        /// <returns>Resultado de la petición y avisos de estado.</returns>
        public LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult> ChangePlayerReadiness(
            string sessionToken,string channelId,OperationChangePlayerReadinessRequest lobbyRequest)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int requestedRoomId = LobbyMembershipChange.NoRoomId;

            if (lobbyRequest == null)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                    LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);
            }
            else
            {
                requestedRoomId = lobbyRequest.RoomId;
                lobbyMembershipChange = _sessionRegistry.ChangePlayerReadiness(
                    sessionToken, channelId, requestedRoomId, lobbyRequest.IsReady);
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

            LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult> changePlayerReadinessResult =
                new LobbyOperationResultWithNotification<OperationChangePlayerReadinessResult>(
                    operationChangePlayerReadinessResult,
                    CreateNotifications(lobbyMembershipChange));

            return changePlayerReadinessResult;
        }

        /// <summary>
        /// Valida el inicio con el reloj del registro; una consulta anticipada solo indica reintento.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="channelId">Canal de lobby que hace la petición.</param>
        /// <param name="matchStartRequest">Sala y generación de cuenta observada; null se rechaza.</param>
        /// <returns>Estado actual, reintento si hay cuenta y avisos si se autorizó la partida.</returns>
        public LobbyOperationResultWithNotification<OperationRequestMatchStartResult> RequestMatchStart(
            string sessionToken, string channelId,OperationMatchStartRequest matchStartRequest)
        {
            LobbyMembershipChange lobbyMembershipChange;
            int requestedRoomId = LobbyMembershipChange.NoRoomId;

            if (matchStartRequest == null)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                    LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);
            }
            else
            {
                requestedRoomId = matchStartRequest.RoomId;
                lobbyMembershipChange = _sessionRegistry.RequestMatchStart(sessionToken, channelId, 
                    requestedRoomId, matchStartRequest.CountdownId);
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
