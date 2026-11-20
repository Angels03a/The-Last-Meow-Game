using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Match results and views received only on the match channel.</summary>
    [ServiceContract]
    public interface IMatchManagerCallback
    {
        /// <summary>Delivers the player's catch attempt result.</summary>
        [OperationContract(IsOneWay = true)]
        void TryCatchFugitiveKittenResult(OperationResult result);

        /// <summary>Delivers the player's deadline check result.</summary>
        [OperationContract(IsOneWay = true)]
        void CheckFugitiveKittenDeadlineResult(OperationResult result);

        /// <summary>Delivers the subscription result. OnMatchViewChanged delivers the view afterward.</summary>
        /// <param name="operationSubscribeToMatchResult">Subscription confirmation or error key.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToMatchResult(OperationResult operationSubscribeToMatchResult);

        /// <summary>Delivers the card placement result to the requester.</summary>
        /// <param name="operationPlaceCardResult">Subscription confirmation or error key.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceCardResult(OperationResult operationPlaceCardResult);

        /// <summary>Delivers the result of opening or raising a bid to the requester.</summary>
        /// <param name="operationPlaceBetResult">Subscription confirmation or error key.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceBetResult(OperationResult operationPlaceBetResult);

        /// <summary>Delivers the result of folding to the requester.</summary>
        /// <param name="operationWithdrawFromBetResult">Subscription confirmation or error key.</param>
        [OperationContract(IsOneWay = true)]
        void WithdrawFromBetResult(OperationResult operationWithdrawFromBetResult);

        /// <summary>Delivers the reveal result to the requester.</summary>
        /// <param name="operationRevealCardResult">Subscription confirmation or error key.</param>
        [OperationContract(IsOneWay = true)]
        void RevealCardResult(OperationResult operationRevealCardResult);

        /// <summary>Delivers the punishment choice result to the requester.</summary>
        /// <param name="operationChoosePunishmentCardResult">Subscription confirmation or error key.</param>
        [OperationContract(IsOneWay = true)]
        void ChoosePunishmentCardResult(OperationResult operationChoosePunishmentCardResult);

        /// <summary>Delivers each subscriber's private match view and its version.</summary>
        /// <param name="matchView">Recipient's private view.</param>
        [OperationContract(IsOneWay = true)]
        void OnMatchViewChanged(MatchView matchView);
        [OperationContract(IsOneWay = true)]
        void MatchHeartbeatEcho(int sequence, string errorKey);
    }
}
