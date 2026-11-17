using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;
using TLMG.Services.Match;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Traduce el motor al contrato: arma la vista privada de cada jugador y convierte cartas, fases y rechazos. Solo
    /// copia valores; nunca conserva referencias al MatchState ni a sus listas.
    /// </summary>
    internal static class MatchViewHelper
    {
        private static readonly Dictionary<CardKind, MatchCardKind> _contractCardsByEngineCard =
            new Dictionary<CardKind, MatchCardKind>
            {
                { CardKind.Safe, MatchCardKind.Safe },
                { CardKind.FatalCat, MatchCardKind.FatalCat }
            };

        private static readonly Dictionary<MatchCardKind, CardKind> _engineCardsByContractCard =
            new Dictionary<MatchCardKind, CardKind>
            {
                { MatchCardKind.Safe, CardKind.Safe },
                { MatchCardKind.FatalCat, CardKind.FatalCat }
            };

        private static readonly Dictionary<RoundPhase, MatchRoundPhase> _contractPhasesByEnginePhase =
            new Dictionary<RoundPhase, MatchRoundPhase>
            {
                { RoundPhase.PlacingCards, MatchRoundPhase.PlacingCards },
                { RoundPhase.Betting, MatchRoundPhase.Betting },
                { RoundPhase.ChallengeReady, MatchRoundPhase.ChallengeReady },
                { RoundPhase.PunishmentPending, MatchRoundPhase.PunishmentPending },
                { RoundPhase.MatchFinished, MatchRoundPhase.MatchFinished }
            };

        private static readonly Dictionary<CardPlacementStatus, string> _errorKeysByPlacementStatus =
            new Dictionary<CardPlacementStatus, string>
            {
                { CardPlacementStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { CardPlacementStatus.BetInProgress, MatchErrorKeys.BetInProgress },
                { CardPlacementStatus.NotPlayersTurn, MatchErrorKeys.NotYourTurn },
                { CardPlacementStatus.HandEmpty, MatchErrorKeys.HandEmpty },
                { CardPlacementStatus.CardNotInHand, MatchErrorKeys.CardNotInHand },
                { CardPlacementStatus.PunishmentPending, MatchErrorKeys.PunishmentPending },
                { CardPlacementStatus.MatchFinished, MatchErrorKeys.MatchFinished }
            };

        /// <summary>
        /// Arma la vista de un destinatario: lo visible para todos y, solo del propio destinatario, su mano y su pila.
        /// Requiere que el llamador tenga serializado el acceso al MatchState (bajo _sessionsLock).
        /// </summary>
        /// <param name="matchId">Partida de la vista.</param>
        /// <param name="version">Versión de la partida al armar la vista.</param>
        /// <param name="matchState">Estado del motor; solo se lee.</param>
        /// <param name="nicknamesByUserId">Nicknames congelados al iniciar la partida.</param>
        /// <param name="viewerUserId">Jugador que recibirá la vista.</param>
        /// <returns>Una vista nueva, sin referencias al motor.</returns>
        internal static MatchView CreateMatchView(
            Guid matchId,
            int version,
            MatchState matchState,
            IReadOnlyDictionary<int, string> nicknamesByUserId,
            int viewerUserId)
        {
            MatchPlayer viewer = matchState.Players.FirstOrDefault(player => player.PlayerId == viewerUserId);
            MatchView matchView = new MatchView
            {
                MatchId = matchId,
                Version = version,
                RoundNumber = matchState.RoundNumber,
                Phase = ConvertToContractPhase(matchState.Phase),
                PlayerInTurnUserId = matchState.PlayerInTurn.PlayerId,
                Players = CreatePlayerViews(matchState, nicknamesByUserId),
                OwnHandCards = ConvertToContractCards(viewer?.HandCards),
                OwnPileCards = ConvertToContractCards(viewer?.PileCards)
            };

            return matchView;
        }

        /// <summary>Convierte una fase del motor a la del contrato.</summary>
        /// <param name="enginePhase">Fase del motor.</param>
        /// <returns>La fase equivalente del contrato.</returns>
        internal static MatchRoundPhase ConvertToContractPhase(RoundPhase enginePhase)
        {
            MatchRoundPhase contractPhase = _contractPhasesByEnginePhase[enginePhase];

            return contractPhase;
        }

        /// <summary>Convierte una carta del motor a la del contrato.</summary>
        /// <param name="engineCard">Carta del motor.</param>
        /// <returns>La carta equivalente del contrato.</returns>
        internal static MatchCardKind ConvertToContractCard(CardKind engineCard)
        {
            MatchCardKind contractCard = _contractCardsByEngineCard[engineCard];

            return contractCard;
        }

        /// <summary>Convierte la carta recibida del cliente al motor.</summary>
        /// <param name="contractCard">Tipo recibido; puede ser un valor fuera del enum.</param>
        /// <param name="engineCard">El tipo del motor; solo vale si devuelve true.</param>
        /// <returns>true si el tipo existe; en caso contrario, false.</returns>
        internal static bool TryConvertToEngineCard(MatchCardKind contractCard, out CardKind engineCard)
        {
            bool isValid = _engineCardsByContractCard.TryGetValue(contractCard, out engineCard);

            return isValid;
        }

        /// <summary>Traduce el estado devuelto por el motor al colocar una carta.</summary>
        /// <param name="placementStatus">Resultado del motor.</param>
        /// <returns>null si la carta se colocó; la clave de error en cualquier otro caso.</returns>
        internal static string ConvertToErrorKey(CardPlacementStatus placementStatus)
        {
            string errorKey;

            if (placementStatus == CardPlacementStatus.Placed)
            {
                errorKey = null;
            }
            else if (!_errorKeysByPlacementStatus.TryGetValue(placementStatus, out errorKey))
            {
                errorKey = MatchErrorKeys.NotInMatch;
            }

            return errorKey;
        }

        private static List<MatchPlayerView> CreatePlayerViews(
            MatchState matchState,
            IReadOnlyDictionary<int, string> nicknamesByUserId)
        {
            List<MatchPlayerView> playerViews = new List<MatchPlayerView>();

            foreach (MatchPlayer player in matchState.Players)
            {
                playerViews.Add(new MatchPlayerView
                {
                    UserId = player.PlayerId,
                    Nickname = nicknamesByUserId[player.PlayerId],
                    HandCardCount = player.HandCards.Count,
                    PileCardCount = player.PileCards.Count,
                    IsEliminated = player.IsEliminated
                });
            }

            return playerViews;
        }

        private static List<MatchCardKind> ConvertToContractCards(IReadOnlyList<CardKind> engineCards)
        {
            List<MatchCardKind> contractCards = new List<MatchCardKind>();

            if (engineCards != null)
            {
                contractCards.AddRange(engineCards.Select(ConvertToContractCard));
            }

            return contractCards;
        }
    }
}
