using System.Collections.Generic;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Validators;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        /// <summary>
        /// Cambia las cartas por jugador de la sala: valida el valor, delega la regla atómica en el registro y arma
        /// el resultado y los avisos para los demás jugadores, que quien llama envía fuera de cualquier candado.
        /// </summary>
        /// <param name="caller">Token de sesión y canal de lobby del jugador.</param>
        /// <param name="operationChangeCardsPerPlayerRequest">Sala y cantidad deseada; null se rechaza sin revelar la sala.</param>
        /// <returns>El resultado con la sala o la clave de error y los avisos para los demás jugadores.</returns>
        public LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> ChangeCardsPerPlayer(
            LobbyCaller caller,
            OperationChangeCardsPerPlayerRequest operationChangeCardsPerPlayerRequest)
        {
            OperationChangeCardsPerPlayerResult operationChangeCardsPerPlayerResult =
                new OperationChangeCardsPerPlayerResult();
            IReadOnlyList<LobbyChangeNotification> lobbyChangeNotifications = LobbyChangeNotification.None;

            string validationErrorKey = ValidateCardsPerPlayerRequest(operationChangeCardsPerPlayerRequest);

            if (validationErrorKey == null)
            {
                LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.ChangeCardsPerPlayer(
                    caller, operationChangeCardsPerPlayerRequest.RoomId, operationChangeCardsPerPlayerRequest.CardsPerPlayerCount);

                operationChangeCardsPerPlayerResult = CreateChangeCardsPerPlayerResult(
                    lobbyMembershipChange, operationChangeCardsPerPlayerRequest);

                lobbyChangeNotifications = CreateNotifications(lobbyMembershipChange);
            }
            else
            {
                operationChangeCardsPerPlayerResult.ErrorKey = validationErrorKey;
            }

            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> changeCardsPerPlayerResult =
                new LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult>(
                    operationChangeCardsPerPlayerResult,
                    lobbyChangeNotifications);

            return changeCardsPerPlayerResult;
        }

        private static string ValidateCardsPerPlayerRequest(OperationChangeCardsPerPlayerRequest operationChangeCardsPerPlayerRequest)
        {
            string errorKey;

            if (operationChangeCardsPerPlayerRequest == null)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
                _log.Warn("Se rechazó cambiar las cartas por jugador: la petición llegó vacía.");
            }
            else
            {
                errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(operationChangeCardsPerPlayerRequest.CardsPerPlayerCount);

                if (errorKey != null)
                {
                    _log.WarnFormat(
                        "Se rechazó cambiar las cartas por jugador de la sala {0}: el valor {1} no es válido.",
                        operationChangeCardsPerPlayerRequest.RoomId,
                        operationChangeCardsPerPlayerRequest.CardsPerPlayerCount);
                }
            }

            return errorKey;
        }

        private static OperationChangeCardsPerPlayerResult CreateChangeCardsPerPlayerResult(
            LobbyMembershipChange lobbyMembershipChange,
            OperationChangeCardsPerPlayerRequest operationChangeCardsPlayerRequest)
        {
            OperationChangeCardsPerPlayerResult operationChangeCardsPerPlayerResult =
                new OperationChangeCardsPerPlayerResult();
            ApplyOutcome(operationChangeCardsPerPlayerResult, lobbyMembershipChange);

            if (operationChangeCardsPerPlayerResult.IsSuccess)
            {
                _log.InfoFormat(
                    "El jugador {0} cambió las cartas por jugador de la sala {1} a {2}.",
                    lobbyMembershipChange.UserId,
                    operationChangeCardsPlayerRequest.RoomId,
                    operationChangeCardsPlayerRequest.CardsPerPlayerCount);
            }
            else
            {
                LogRejectedOperation("cambiar las cartas por jugador", lobbyMembershipChange, operationChangeCardsPlayerRequest.RoomId);
            }

            operationChangeCardsPerPlayerResult.Room = ConvertRoomOrNull(lobbyMembershipChange);

            return operationChangeCardsPerPlayerResult;
        }
    }
}