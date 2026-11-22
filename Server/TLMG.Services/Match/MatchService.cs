using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Match
{
    /// <summary>
    /// Builds the requester's result and each subscriber's view from the registry's decision. ISessionRegistry handles
    /// authorization and access to the engine.
    /// </summary>
    public sealed class MatchService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MatchService));

        private readonly ISessionRegistry _sessionRegistry;

        /// <summary>Creates the service with the shared registry.</summary>
        /// <param name="sessionRegistry">Registry of sessions, rooms and matches.</param>
        public MatchService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>Subscribes the player to their match and prepares their view for delivery.</summary>
        /// <param name="caller">Requester's token and match channel.</param>
        /// <param name="matchId">Requested match.</param>
        /// <param name="callback">Match channel callback.</param>
        /// <returns>Requester's result and private view ready for delivery.</returns>
        public ResultWithNotification<OperationResult> SubscribeToMatch(
            SessionChannelCaller caller,
            Guid matchId,
            IMatchManagerCallback callback)
        {
            MatchDelivery matchDelivery = _sessionRegistry.SubscribeToMatch(caller, matchId, callback);
            ResultWithNotification<OperationResult> subscribeToMatchResult = CreateResult(matchDelivery);

            if (subscribeToMatchResult.OperationResult.IsSuccess)
            {
                _log.InfoFormat("Un jugador se suscribió a la partida {0}.", matchId);
            }

            return subscribeToMatchResult;
        }

        /// <summary>Checks an attempt to catch the kitten.</summary>
        public ResultWithNotification<OperationResult> TryCatchFugitiveKitten(SessionChannelCaller caller)
        {
            return CreateResult(_sessionRegistry.TryCatchFugitiveKitten(caller));
        }

        /// <summary>Checks whether the minigame deadline has passed.</summary>
        public ResultWithNotification<OperationResult> CheckFugitiveKittenDeadline(SessionChannelCaller caller)
        {
            return CreateResult(_sessionRegistry.CheckFugitiveKittenDeadline(caller));
        }

        /// <summary>Places the player's card and prepares a view for each subscriber.</summary>
        /// <param name="caller">Requester's token and match channel.</param>
        /// <param name="card">Type of card to place.</param>
        /// <returns>Requester's result and, on acceptance, one view per subscriber.</returns>
        public ResultWithNotification<OperationResult> PlaceCard(SessionChannelCaller caller, MatchCardKind card)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceCard(caller, card);
            ResultWithNotification<OperationResult> placeCardResult = CreateResult(matchDelivery);

            return placeCardResult;
        }

        /// <summary>Opens or raises a bid and prepares a view for each subscriber.</summary>
        /// <param name="caller">Requester's token and match channel.</param>
        /// <param name="betValue">Number of cards the player claims they can reveal.</param>
        /// <returns>Requester's result and, on acceptance, one view per subscriber.</returns>
        public ResultWithNotification<OperationResult> PlaceBet(SessionChannelCaller caller, int betValue)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceBet(caller, betValue);
            ResultWithNotification<OperationResult> placeBetResult = CreateResult(matchDelivery);

            return placeBetResult;
        }

        /// <summary>Folds for the player and prepares a view for each subscriber.</summary>
        /// <param name="caller">Requester's token and match channel.</param>
        /// <returns>Requester's result and, on acceptance, one view per subscriber.</returns>
        public ResultWithNotification<OperationResult> WithdrawFromBet(SessionChannelCaller caller)
        {
            MatchDelivery matchDelivery = _sessionRegistry.WithdrawFromBet(caller);
            ResultWithNotification<OperationResult> withdrawFromBetResult = CreateResult(matchDelivery);

            return withdrawFromBetResult;
        }

        /// <summary>Reveals a challenge card and prepares a view for each subscriber.</summary>
        /// <param name="caller">Requester's token and match channel.</param>
        /// <param name="pileOwnerUserId">Owner of the pile to reveal.</param>
        /// <returns>Requester's result and, on acceptance, one view per subscriber.</returns>
        public ResultWithNotification<OperationResult> RevealCard(SessionChannelCaller caller, int pileOwnerUserId)
        {
            MatchDelivery matchDelivery = _sessionRegistry.RevealCard(caller, pileOwnerUserId);
            ResultWithNotification<OperationResult> revealCardResult = CreateResult(matchDelivery);

            return revealCardResult;
        }

        /// <summary>Chooses the punishment card and prepares a view for each subscriber.</summary>
        /// <param name="caller">Requester's token and match channel.</param>
        /// <param name="cardIndex">Displayed slot for a blind choice or hand position for a visible choice.</param>
        /// <returns>Requester's result and, on acceptance, one view per subscriber.</returns>
        public ResultWithNotification<OperationResult> ChoosePunishmentCard(SessionChannelCaller caller, int cardIndex)
        {
            MatchDelivery matchDelivery = _sessionRegistry.ChoosePunishmentCard(caller, cardIndex);
            ResultWithNotification<OperationResult> choosePunishmentCardResult = CreateResult(matchDelivery);

            return choosePunishmentCardResult;
        }

        /// <summary>
        /// Removes a closed or failed match channel's subscription. A lost connection starts the applicable grace
        /// period.
        /// </summary>
        /// <param name="channelId">Closed or failed channel.</param>
        /// <param name="closedCallback">Callback registered for that channel.</param>
        public IReadOnlyList<Action> RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback)
        {
            if (_sessionRegistry.RemoveMatchChannel(channelId, closedCallback, out var deliveries))
            {
                _log.InfoFormat("Se quitó la suscripción del canal de partida {0}.", channelId);
            }
            return SessionNotificationHelper.CreateMatchViewNotifications(deliveries);
        }

        private static ResultWithNotification<OperationResult> CreateResult(MatchDelivery matchDelivery)
        {
            bool isSuccess = matchDelivery != null && matchDelivery.ErrorKey == null;
            string errorKey = isSuccess ? string.Empty : matchDelivery?.ErrorKey;
            if (!isSuccess && string.IsNullOrWhiteSpace(errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }
            OperationResult operationResult = new OperationResult
            {
                IsSuccess = isSuccess,
                ErrorKey = errorKey
            };
            LogMatchRejection(isSuccess ? null : errorKey);

            ResultWithNotification<OperationResult> matchResult = new ResultWithNotification<OperationResult>(
                operationResult, SessionNotificationHelper.CreateMatchViewNotifications(
                    matchDelivery?.ViewDeliveries ?? new List<MatchViewDelivery>()));

            return matchResult;
        }

        private static void LogMatchRejection(string errorKey)
        {
            if (errorKey != null)
            {
                _log.WarnFormat("Se rechazó una operación de partida: {0}.", errorKey);
            }
        }
    }
}
