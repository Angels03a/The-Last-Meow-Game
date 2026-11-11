using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        /// <summary>Cambia la cantidad de cartas por jugador de la sala; todavía no está implementado.</summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="channelId">Identificador del canal de lobby desde el que llama el jugador.</param>
        /// <param name="request">Datos de la petición; puede ser null.</param>
        /// <returns>El resultado con la clave de error inesperado y sin avisos mientras no se implemente.</returns>
        public LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> ChangeCardsPerPlayer(
            string sessionToken,
            string channelId,
            OperationChangeCardsPerPlayerRequest request)
        {
            // TODO: Implementar el cambio de cartas por jugador de la sala. (BL-151).
            OperationChangeCardsPerPlayerResult operationChangeCardsPerPlayerResult =
                new OperationChangeCardsPerPlayerResult { ErrorKey = ErrorKeys.Common.Unexpected };
            LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult> changeCardsPerPlayerResult =
                new LobbyOperationResultWithNotification<OperationChangeCardsPerPlayerResult>(
                    operationChangeCardsPerPlayerResult);

            return changeCardsPerPlayerResult;
        }
    }
}
