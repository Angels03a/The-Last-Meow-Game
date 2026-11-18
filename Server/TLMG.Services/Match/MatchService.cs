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

        /// <summary>Coloca una carta del jugador y prepara la vista de cada suscrito.</summary>
        /// <param name="caller">Token y canal de partida de quien pide.</param>
        /// <param name="card">Tipo de carta que se quiere colocar.</param>
        /// <returns>El resultado para quien pidió y una vista por cada suscrito si se aceptó.</returns>
        public ResultWithNotification<OperationResult> PlaceCard(LobbyCaller caller, MatchCardKind card)
        {
            MatchDelivery matchDelivery = _sessionRegistry.PlaceCard(caller, card);
            ResultWithNotification<OperationResult> placeCardResult = CreateResult(matchDelivery);

            return placeCardResult;
        }

        /// <summary>Quita la suscripción de un canal de partida cerrado o fallido, sin tocar la partida.</summary>
        /// <param name="channelId">Canal que se cerró o falló.</param>
        /// <param name="closedCallback">Callback que tenía ese canal.</param>
        public void RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback)
        {
            if (_sessionRegistry.RemoveMatchChannel(channelId, closedCallback))
            {
                _log.InfoFormat("Se quitó la suscripción del canal de partida {0}.", channelId);
            }
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
