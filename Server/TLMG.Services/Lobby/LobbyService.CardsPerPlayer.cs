using System;
using System.Collections.Generic;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Validators;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        /// <summary>
        /// Cambia las cartas por jugador de la sala: valida el valor, delega la regla atómica en el registro y arma
        /// el resultado y los avisos para los demás jugadores, que quien llama envía fuera de cualquier candado. Un
        /// error inesperado se registra y se convierte en Common_UnexpectedError.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="channelId">Identificador del canal de lobby desde el que llama el jugador.</param>
        /// <param name="request">Sala y cantidad deseada; null se rechaza sin revelar la sala.</param>
        /// <returns>El resultado con la sala o la clave de error y los avisos para los demás jugadores.</returns>
        public LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> ChangeCardsPerPlayer(
            string sessionToken,
            string channelId,
            OperationChangeCardsPerPlayerRequest request)
        {
            OperationChangeCardsPerPlayerResult operationChangeCardsPerPlayerResult =
                new OperationChangeCardsPerPlayerResult();
            IReadOnlyList<LobbyChangeNotification> lobbyChangeNotifications = LobbyChangeNotification.None;

            try
            {
                string validationErrorKey = ValidateCardsPerPlayerRequest(request);

                if (validationErrorKey == null)
                {
                    LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.ChangeCardsPerPlayer(
                        sessionToken, channelId, request.RoomId, request.CardsPerPlayerCount);
                    operationChangeCardsPerPlayerResult = CreateChangeCardsPerPlayerResult(
                        lobbyMembershipChange, request);
                    lobbyChangeNotifications = CreateNotifications(lobbyMembershipChange);
                }
                else
                {
                    operationChangeCardsPerPlayerResult.ErrorKey = validationErrorKey;
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al cambiar las cartas por jugador de la sala.", exception);
                operationChangeCardsPerPlayerResult =
                    new OperationChangeCardsPerPlayerResult { ErrorKey = ErrorKeys.Common.Unexpected };
                lobbyChangeNotifications = LobbyChangeNotification.None;
            }

            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> changeCardsPerPlayerResult =
                new LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult>(
                    operationChangeCardsPerPlayerResult,
                    lobbyChangeNotifications);

            return changeCardsPerPlayerResult;
        }

        private static string ValidateCardsPerPlayerRequest(OperationChangeCardsPerPlayerRequest request)
        {
            string errorKey;

            if (request == null)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
                _log.Warn("Se rechazó cambiar las cartas por jugador: la petición llegó vacía.");
            }
            else
            {
                errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(request.CardsPerPlayerCount);

                if (errorKey != null)
                {
                    _log.WarnFormat(
                        "Se rechazó cambiar las cartas por jugador de la sala {0}: el valor {1} no es válido.",
                        request.RoomId,
                        request.CardsPerPlayerCount);
                }
            }

            return errorKey;
        }

        private static OperationChangeCardsPerPlayerResult CreateChangeCardsPerPlayerResult(
            LobbyMembershipChange lobbyMembershipChange,
            OperationChangeCardsPerPlayerRequest request)
        {
            OperationChangeCardsPerPlayerResult operationChangeCardsPerPlayerResult =
                new OperationChangeCardsPerPlayerResult
                {
                    IsSuccess = lobbyMembershipChange.Status == LobbyChangeStatus.Applied
                };

            if (operationChangeCardsPerPlayerResult.IsSuccess)
            {
                _log.InfoFormat(
                    "El jugador {0} dejó la sala {1} con {2} cartas por jugador.",
                    lobbyMembershipChange.UserId,
                    request.RoomId,
                    request.CardsPerPlayerCount);
            }
            else
            {
                operationChangeCardsPerPlayerResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
                LogRejectedOperation("cambiar las cartas por jugador", lobbyMembershipChange, request.RoomId);
            }

            if (lobbyMembershipChange.Room != null)
            {
                operationChangeCardsPerPlayerResult.Room =
                    LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
            }

            return operationChangeCardsPerPlayerResult;
        }
    }
}
