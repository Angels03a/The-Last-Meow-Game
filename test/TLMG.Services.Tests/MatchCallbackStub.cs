using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Test match callback that saves results and views in arrival order.</summary>
    internal sealed class MatchCallbackStub : IMatchManagerCallback
    {
        internal List<OperationResult> SubscribeResults { get; } = new List<OperationResult>();

        internal List<OperationResult> PlaceCardResults { get; } = new List<OperationResult>();

        internal List<OperationResult> PlaceBetResults { get; } = new List<OperationResult>();

        internal List<OperationResult> WithdrawFromBetResults { get; } = new List<OperationResult>();

        internal List<OperationResult> RevealCardResults { get; } = new List<OperationResult>();

        internal List<OperationResult> ChoosePunishmentCardResults { get; } = new List<OperationResult>();

        internal List<MatchView> Views { get; } = new List<MatchView>();

        internal MatchView LastView => Views.LastOrDefault();

        public void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult)
        {
            SubscribeResults.Add(operationSubscribeToMatchResult);
        }

        public void TryCatchFugitiveKittenResult(OperationResult result) { }

        public void CheckFugitiveKittenDeadlineResult(OperationResult result) { }

        public void PlaceCardResult(OperationResult operationPlaceCardResult)
        {
            PlaceCardResults.Add(operationPlaceCardResult);
        }

        public void PlaceBetResult(OperationResult operationPlaceBetResult)
        {
            PlaceBetResults.Add(operationPlaceBetResult);
        }

        public void WithdrawFromBetResult(OperationResult operationWithdrawFromBetResult)
        {
            WithdrawFromBetResults.Add(operationWithdrawFromBetResult);
        }

        public void RevealCardResult(OperationResult operationRevealCardResult)
        {
            RevealCardResults.Add(operationRevealCardResult);
        }

        public void ChoosePunishmentCardResult(OperationResult operationChoosePunishmentCardResult)
        {
            ChoosePunishmentCardResults.Add(operationChoosePunishmentCardResult);
        }

        public void OnMatchViewChanged(MatchView matchView)
        {
            Views.Add(matchView);
        }
        public void MatchHeartbeatEcho(int sequence, string errorKey) { }
    }
}
