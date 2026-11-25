using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Match
{
    public sealed class MatchService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MatchService));

        private readonly ISessionRegistry _sessionRegistry;

        public MatchService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        public ResultWithNotification<OperationResult> SubscribeToMatch(
            LobbyCaller caller,
            Guid matchId,
            IMatchManagerCallback callback)
        {
            MatchDelivery matchDelivery = _sessionRegistry.SubscribeToMatch(caller, matchId, callback);
            ResultWithNotification<OperationResult> subscribeToMatchResult = CreateResult(matchDelivery);

            if (matchDelivery.ErrorKey == null)
            {
                _log.InfoFormat("Un jugador se suscribió a la partida {0}.", matchId);
            }

            return subscribeToMatchResult;
        }

        public ResultWithNotification<OperationResult> TryCatchFugitiveKitten(LobbyCaller caller)
        {
            return CreateResult(_sessionRegistry.TryCatchFugitiveKitten(caller));
        }

        public ResultWithNotification<OperationResult> CheckFugitiveKittenDeadline(LobbyCaller caller)
        {
            return CreateResult(_sessionRegistry.CheckFugitiveKittenDeadline(caller));
        }

        public ResultWithNotification<OperationResult> PlaceCard(LobbyCaller caller, MatchCardKind card)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceCard(caller, card);
            ResultWithNotification<OperationResult> placeCardResult = CreateResult(matchDelivery);

            return placeCardResult;
        }

        public ResultWithNotification<OperationResult> PlaceBet(LobbyCaller caller, int betValue)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceBet(caller, betValue);
            ResultWithNotification<OperationResult> placeBetResult = CreateResult(matchDelivery);

            return placeBetResult;
        }

        public ResultWithNotification<OperationResult> WithdrawFromBet(LobbyCaller caller)
        {
            MatchDelivery matchDelivery = _sessionRegistry.WithdrawFromBet(caller);
            ResultWithNotification<OperationResult> withdrawFromBetResult = CreateResult(matchDelivery);

            return withdrawFromBetResult;
        }

        public ResultWithNotification<OperationResult> RevealCard(LobbyCaller caller, int pileOwnerUserId)
        {
            MatchDelivery matchDelivery = _sessionRegistry.RevealCard(caller, pileOwnerUserId);
            ResultWithNotification<OperationResult> revealCardResult = CreateResult(matchDelivery);

            return revealCardResult;
        }

        public ResultWithNotification<OperationResult> ChoosePunishmentCard(LobbyCaller caller, int cardIndex)
        {
            MatchDelivery matchDelivery = _sessionRegistry.ChoosePunishmentCard(caller, cardIndex);
            ResultWithNotification<OperationResult> choosePunishmentCardResult = CreateResult(matchDelivery);

            return choosePunishmentCardResult;
        }

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
            OperationResult operationResult = new OperationResult
            {
                IsSuccess = matchDelivery.ErrorKey == null,
                ErrorKey = matchDelivery.ErrorKey
            };
            LogMatchRejection(matchDelivery.ErrorKey);

            ResultWithNotification<OperationResult> matchResult = new ResultWithNotification<OperationResult>(
                operationResult, SessionNotificationHelper.CreateMatchViewNotifications(matchDelivery.ViewDeliveries));

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
