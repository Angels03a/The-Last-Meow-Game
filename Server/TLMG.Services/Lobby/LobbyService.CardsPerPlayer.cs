using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Validators;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        public ResultWithNotification<OperationLobbyRoomResult> ChangeCardsPerPlayer(
            SessionChannelCaller caller,
            OperationChangeCardsPerPlayerRequest operationChangeCardsPerPlayerRequest)
        {
            OperationLobbyRoomResult operationChangeCardsPerPlayerResult =
                new OperationLobbyRoomResult();

            ResultWithNotification<OperationLobbyRoomResult> changeCardsPerPlayerResult;
            string validationErrorKey = ValidateCardsPerPlayerRequest(operationChangeCardsPerPlayerRequest);

            if (validationErrorKey == null)
            {
                LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.ChangeCardsPerPlayer(
                    caller, operationChangeCardsPerPlayerRequest.RoomId, operationChangeCardsPerPlayerRequest.CardsPerPlayerCount);

                operationChangeCardsPerPlayerResult = CreateChangeCardsPerPlayerResult(
                    lobbyMembershipChange, operationChangeCardsPerPlayerRequest);

                changeCardsPerPlayerResult = new ResultWithNotification<OperationLobbyRoomResult>(
                    operationChangeCardsPerPlayerResult,
                    SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange));
            }
            else
            {
                operationChangeCardsPerPlayerResult.ErrorKey = validationErrorKey;
                changeCardsPerPlayerResult =
                    new ResultWithNotification<OperationLobbyRoomResult>(operationChangeCardsPerPlayerResult);
            }

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

        private static OperationLobbyRoomResult CreateChangeCardsPerPlayerResult(
            LobbyMembershipChange lobbyMembershipChange,
            OperationChangeCardsPerPlayerRequest operationChangeCardsPlayerRequest)
        {
            OperationLobbyRoomResult operationChangeCardsPerPlayerResult =
                new OperationLobbyRoomResult();
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