using System;
using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Dedicated duplex channel for following a match and placing cards. The server makes the decisions.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IMatchManagerCallback))]
    public interface IMatchManager
    {
        /// <summary>
        /// Subscribes the channel to the player's match. The player's latest subscription wins. The callback delivers
        /// the result and the player's private view.
        /// </summary>
        /// <param name="sessionToken">Current token of the registered player.</param>
        /// <param name="matchId">Match authorized at room start, identified by the room.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToMatch(string sessionToken, Guid matchId);

        /// <summary>Requests an attempt to catch the kitten.</summary>
        [OperationContract(IsOneWay = true)]
        void TryCatchFugitiveKitten(string sessionToken);

        /// <summary>Requests a check of the minigame deadline.</summary>
        [OperationContract(IsOneWay = true)]
        void CheckFugitiveKittenDeadline(string sessionToken);

        /// <summary>
        /// Requests to move a card from the player's hand to their pile in the subscribed match. The result reports
        /// acceptance or rejection. Each subscriber receives the updated state in a private view.
        /// </summary>
        /// <param name="sessionToken">Current token of the registered player.</param>
        /// <param name="cardKind">Type of card to place.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceCard(string sessionToken, MatchCardKind cardKind);

        /// <summary>
        /// Opens or raises a bid according to the phase decided by the server. The bid must be between MinimumBetValue
        /// and MaximumBetValue in the view. The result reports acceptance or rejection. Each subscriber receives the
        /// updated state in a private view.
        /// </summary>
        /// <param name="sessionToken">Current token of the registered player.</param>
        /// <param name="betValue">Number of cards the player claims they can reveal.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceBet(string sessionToken, int betValue);

        /// <summary>
        /// Folds for the rest of this bidding round. When only one bidder remains, that player takes the challenge.
        /// </summary>
        /// <param name="sessionToken">Current token of the registered player.</param>
        [OperationContract(IsOneWay = true)]
        void WithdrawFromBet(string sessionToken);

        /// <summary>
        /// Reveals the top card of the chosen pile during the player's challenge. Reveal the player's own pile first,
        /// then choose another player's pile before each reveal. Stop when the bid is met or a Fatal Cat is revealed.
        /// </summary>
        /// <param name="sessionToken">Current token of the registered player.</param>
        /// <param name="pileOwnerUserId">Account of the player whose pile supplies the card.</param>
        [OperationContract(IsOneWay = true)]
        void RevealCard(string sessionToken, int pileOwnerUserId);

        /// <summary>
        /// Chooses the card the punished player loses. For a blind choice, the server maps the displayed slot (0 to
        /// CardSlotCount - 1). When choosing from a visible hand, use the card's position in OwnHandCards.
        /// </summary>
        /// <param name="sessionToken">Current token of the registered player.</param>
        /// <param name="cardIndex">Slot or position of the card to remove.</param>
        [OperationContract(IsOneWay = true)]
        void ChoosePunishmentCard(string sessionToken, int cardIndex);
        [OperationContract(IsOneWay = true)]
        void MatchHeartbeat(string sessionToken, int sequence, int confirmedSequence);
    }
}
