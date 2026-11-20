using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Match
{
    /// <summary>
    /// Lógica de partida del servidor: convierte las decisiones del registro en el resultado para quien pidió y en
    /// las vistas que hay que entregar a cada suscrito. El motor y la autorización viven detrás de ISessionRegistry.
    /// </summary>
    public sealed class MatchService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MatchService));

        private readonly ISessionRegistry _sessionRegistry;

        /// <summary>Crea el servicio sobre el registro compartido.</summary>
        /// <param name="sessionRegistry">Registro de sesiones, salas y partidas.</param>
        public MatchService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>Suscribe al jugador a su partida y prepara el envío de su vista.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <param name="matchId">Partida solicitada.</param>
        /// <param name="callback">Callback del canal de partida.</param>
        /// <returns>El resultado para quien pidió y su vista como aviso pendiente de entregar.</returns>
        public ResultWithNotification<OperationResult> SubscribeToMatch(
            SessionChannelCaller caller,
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

        /// <summary>Valida un intento de atrapar al gatito.</summary>
        public ResultWithNotification<OperationResult> TryCatchFugitiveKitten(SessionChannelCaller caller)
        {
            return CreateResult(_sessionRegistry.TryCatchFugitiveKitten(caller));
        }

        /// <summary>Comprueba el vencimiento del minijuego.</summary>
        public ResultWithNotification<OperationResult> CheckFugitiveKittenDeadline(SessionChannelCaller caller)
        {
            return CreateResult(_sessionRegistry.CheckFugitiveKittenDeadline(caller));
        }

        /// <summary>Coloca una carta del jugador y prepara la vista de cada suscrito.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <param name="card">Tipo de carta que se quiere colocar.</param>
        /// <returns>El resultado para quien pidió y una vista por cada suscrito si se aceptó.</returns>
        public ResultWithNotification<OperationResult> PlaceCard(SessionChannelCaller caller, MatchCardKind card)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceCard(caller, card);
            ResultWithNotification<OperationResult> placeCardResult = CreateResult(matchDelivery);

            return placeCardResult;
        }

        /// <summary>Abre o sube una apuesta y prepara la vista de cada suscrito.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <param name="betValue">Cartas que declara poder voltear.</param>
        /// <returns>El resultado para quien pidió y una vista por cada suscrito si se aceptó.</returns>
        public ResultWithNotification<OperationResult> PlaceBet(SessionChannelCaller caller, int betValue)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceBet(caller, betValue);
            ResultWithNotification<OperationResult> placeBetResult = CreateResult(matchDelivery);

            return placeBetResult;
        }

        /// <summary>Retira al jugador de la apuesta y prepara la vista de cada suscrito.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <returns>El resultado para quien pidió y una vista por cada suscrito si se aceptó.</returns>
        public ResultWithNotification<OperationResult> WithdrawFromBet(SessionChannelCaller caller)
        {
            MatchDelivery matchDelivery = _sessionRegistry.WithdrawFromBet(caller);
            ResultWithNotification<OperationResult> withdrawFromBetResult = CreateResult(matchDelivery);

            return withdrawFromBetResult;
        }

        /// <summary>Voltea una carta del reto y prepara la vista de cada suscrito.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <param name="pileOwnerUserId">Dueño de la pila de la que se voltea.</param>
        /// <returns>El resultado para quien pidió y una vista por cada suscrito si se aceptó.</returns>
        public ResultWithNotification<OperationResult> RevealCard(SessionChannelCaller caller, int pileOwnerUserId)
        {
            MatchDelivery matchDelivery = _sessionRegistry.RevealCard(caller, pileOwnerUserId);
            ResultWithNotification<OperationResult> revealCardResult = CreateResult(matchDelivery);

            return revealCardResult;
        }

        /// <summary>Elige la carta del castigo y prepara la vista de cada suscrito.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <param name="cardIndex">Lugar mostrado (a ciegas) o posición en la mano (castigo propio).</param>
        /// <returns>El resultado para quien pidió y una vista por cada suscrito si se aceptó.</returns>
        public ResultWithNotification<OperationResult> ChoosePunishmentCard(SessionChannelCaller caller, int cardIndex)
        {
            MatchDelivery matchDelivery = _sessionRegistry.ChoosePunishmentCard(caller, cardIndex);
            ResultWithNotification<OperationResult> choosePunishmentCardResult = CreateResult(matchDelivery);

            return choosePunishmentCardResult;
        }

        /// <summary>Quita la suscripción de un canal de partida cerrado o fallido, sin tocar la partida.</summary>
        /// <param name="channelId">Canal que se cerró o falló.</param>
        /// <param name="closedCallback">Callback que tenía ese canal.</param>
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
