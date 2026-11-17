using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Callback de partida de prueba: guarda los resultados y las vistas que recibe, en orden.</summary>
    internal sealed class MatchCallbackStub : IMatchManagerCallback
    {
        internal List<OperationResult> SubscribeResults { get; } = new List<OperationResult>();

        internal List<OperationResult> PlaceCardResults { get; } = new List<OperationResult>();

        internal List<MatchView> Views { get; } = new List<MatchView>();

        internal MatchView LastView => Views.LastOrDefault();

        public void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult)
        {
            SubscribeResults.Add(operationSubscribeToMatchResult);
        }

        public void PlaceCardResult(OperationResult operationPlaceCardResult)
        {
            PlaceCardResults.Add(operationPlaceCardResult);
        }

        public void OnMatchViewChanged(MatchView matchView)
        {
            Views.Add(matchView);
        }
    }
}
