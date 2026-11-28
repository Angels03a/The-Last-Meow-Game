using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Applies match actions and manages match subscriptions.</summary>
    public interface IMatchRegistry
    {
        /// <summary>
        /// Authorizes a subscription and returns the player's view under the same lock. The userId comes from the
        /// session and the player must still belong to the match's room.
        /// </summary>
        /// <param name="caller">Current session token and match channel.</param>
        /// <param name="matchId">Requested match.</param>
        /// <param name="callback">Match channel callback replacing the player's previous one.</param>
        /// <returns>Player's view on acceptance, otherwise the error key.</returns>
        MatchDelivery SubscribeToMatch(SessionChannelCaller caller, Guid matchId, IMatchManagerCallback callback);

        /// <summary>
        /// Authorizes card placement in the channel's subscribed match. Rejection leaves state unchanged. Acceptance
        /// increases the version and returns one view per subscriber.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="card">Type of card to place.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery PlaceCard(SessionChannelCaller caller, MatchCardKind card);

        /// <summary>
        /// Authorizes a bid in the subscribed match. The phase determines whether to open or raise. Rejection leaves
        /// state unchanged. Acceptance increases the version and returns one view per subscriber.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="betValue">Number of cards the player claims they can reveal.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery PlaceBet(SessionChannelCaller caller, int betValue);

        /// <summary>
        /// Authorizes folding. When only one bidder remains, the challenge is ready and the registry starts recording
        /// its outcome.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery WithdrawFromBet(SessionChannelCaller caller);

        /// <summary>
        /// Authorizes revealing the top card of the chosen pile. Records revealed cards, success or failure and
        /// punishment in the match record.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="pileOwnerUserId">Account of the player whose pile supplies the card.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery RevealCard(SessionChannelCaller caller, int pileOwnerUserId);

        /// <summary>
        /// Authorizes removal of the chosen punishment card. For a blind choice, the server maps the displayed slot to
        /// the actual index and keeps that mapping private.
        /// </summary>
        /// <param name="caller">Current token and subscribed match channel.</param>
        /// <param name="cardIndex">Displayed slot for a blind choice or hand position for a visible choice.</param>
        /// <returns>All subscribers' views on acceptance, otherwise the error key.</returns>
        MatchDelivery ChoosePunishmentCard(SessionChannelCaller caller, int cardIndex);

        MatchDelivery TryCatchFugitiveKitten(SessionChannelCaller caller);

        MatchDelivery CheckFugitiveKittenDeadline(SessionChannelCaller caller);

        /// <summary>
        /// Removes the callback only if the closed or failed channel is still current. A lost match connection can
        /// start a reconnection window.
        /// </summary>
        /// <param name="channelId">Closed or failed channel.</param>
        /// <param name="closedCallback">Original channel callback, compared by reference.</param>
        /// <returns>True if the subscription was removed.</returns>
        bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback,
            out IReadOnlyList<MatchViewDelivery> deliveries);
    }
}
