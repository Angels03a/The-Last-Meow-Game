using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Resultados y vistas de partida recibidos exclusivamente por el canal de partida.</summary>
    [ServiceContract]
    public interface IMatchManagerCallback
    {
        /// <summary>Entrega el resultado de la suscripción; la vista llega después por OnMatchViewChanged.</summary>
        /// <param name="operationSubscribeToMatchResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult);

        /// <summary>Entrega el resultado de colocar una carta a quien lo pidió.</summary>
        /// <param name="operationPlaceCardResult">Aceptación o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceCardResult(OperationResult operationPlaceCardResult);

        /// <summary>Entrega a cada suscrito su propia vista de la partida, con su versión.</summary>
        /// <param name="matchView">Vista privada del destinatario.</param>
        [OperationContract(IsOneWay = true)]
        void OnMatchViewChanged(MatchView matchView);
    }
}
