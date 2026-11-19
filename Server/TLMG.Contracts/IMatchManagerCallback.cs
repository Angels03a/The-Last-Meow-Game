using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Resultados y vistas de partida recibidos exclusivamente por el canal de partida.</summary>
    [ServiceContract]
    public interface IMatchManagerCallback
    {
        /// <summary>Entrega el resultado individual del intento.</summary>
        [OperationContract(IsOneWay = true)]
        void TryCatchFugitiveKittenResult(OperationResult result);

        /// <summary>Entrega el resultado individual de la comprobación.</summary>
        [OperationContract(IsOneWay = true)]
        void CheckFugitiveKittenDeadlineResult(OperationResult result);

        /// <summary>Entrega el resultado de la suscripción; la vista llega después por OnMatchViewChanged.</summary>
        /// <param name="operationSubscribeToMatchResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult);

        /// <summary>Entrega el resultado de colocar una carta a quien lo pidió.</summary>
        /// <param name="operationPlaceCardResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceCardResult(OperationResult operationPlaceCardResult);

        /// <summary>Entrega el resultado de abrir o subir una apuesta a quien lo pidió.</summary>
        /// <param name="operationPlaceBetResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceBetResult(OperationResult operationPlaceBetResult);

        /// <summary>Entrega el resultado de retirarse de la apuesta a quien lo pidió.</summary>
        /// <param name="operationWithdrawFromBetResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void WithdrawFromBetResult(OperationResult operationWithdrawFromBetResult);

        /// <summary>Entrega el resultado de voltear una carta a quien lo pidió.</summary>
        /// <param name="operationRevealCardResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void RevealCardResult(OperationResult operationRevealCardResult);

        /// <summary>Entrega el resultado de elegir la carta del castigo a quien lo pidió.</summary>
        /// <param name="operationChoosePunishmentCardResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void ChoosePunishmentCardResult(OperationResult operationChoosePunishmentCardResult);

        /// <summary>Entrega a cada suscrito su propia vista de la partida, con su versión.</summary>
        /// <param name="matchView">Vista privada del destinatario.</param>
        [OperationContract(IsOneWay = true)]
        void OnMatchViewChanged(MatchView matchView);
    }
}
