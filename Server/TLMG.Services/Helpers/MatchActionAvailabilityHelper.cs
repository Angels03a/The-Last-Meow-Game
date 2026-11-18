using System;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Calcula qué acciones puede hacer cada destinatario de una vista. Es una ayuda para la interfaz: el motor sigue
    /// siendo quien acepta o rechaza cada acción, así que un valor de aquí nunca da un permiso.
    /// </summary>
    internal static class MatchActionAvailabilityHelper
    {
        private const int MinimumOpeningBetValue = 1;
        private const int NoBetValue = 0;

        /// <summary>Llena las banderas de acciones y el rango de apuesta de la vista de un destinatario.</summary>
        /// <param name="matchView">Vista del destinatario, ya con fase, apuesta y cartas.</param>
        /// <param name="matchState">Estado del motor; solo se lee.</param>
        /// <param name="viewer">El jugador destinatario; null si no es jugador de la partida.</param>
        internal static void ApplyAvailability(MatchView matchView, MatchState matchState, MatchPlayer viewer)
        {
            bool isViewerInPlay = viewer != null && viewer.IsInPlay;
            bool hasTurn = isViewerInPlay && ReferenceEquals(viewer, matchState.PlayerInTurn);

            if (isViewerInPlay)
            {
                ApplyTurnActions(matchView, matchState, viewer, hasTurn);
                ApplyBetRange(matchView, matchState, viewer, hasTurn);
                matchView.CanRevealCard = matchState.Phase == RoundPhase.ChallengeReady
                    && ReferenceEquals(viewer, matchState.BetLeader);
                matchView.CanChoosePunishmentCard = CanChoosePunishment(matchState, viewer);
            }
        }

        private static void ApplyTurnActions(
            MatchView matchView,
            MatchState matchState,
            MatchPlayer viewer,
            bool hasTurn)
        {
            bool isPlacingPhase = matchState.Phase == RoundPhase.PlacingCards;

            matchView.CanPlaceCard = hasTurn && isPlacingPhase && viewer.HandCards.Count > 0;
            matchView.CanWithdrawFromBet = hasTurn && matchState.Phase == RoundPhase.Betting;
        }

        private static void ApplyBetRange(
            MatchView matchView,
            MatchState matchState,
            MatchPlayer viewer,
            bool hasTurn)
        {
            int maximumBet = Math.Min(matchState.CardsOnTable, matchState.ModeRules.CardsPerPlayerCount);

            if (hasTurn && CanOpenBet(matchState, viewer, maximumBet))
            {
                matchView.CanPlaceBet = true;
                matchView.MinimumBetValue = MinimumOpeningBetValue;
                matchView.MaximumBetValue = viewer.HandCards.Count == 0 ? MinimumOpeningBetValue : maximumBet;
            }
            else if (hasTurn && CanRaiseBet(matchState, maximumBet))
            {
                matchView.CanPlaceBet = true;
                matchView.MinimumBetValue = matchState.CurrentBet + MinimumOpeningBetValue;
                matchView.MaximumBetValue = maximumBet;
            }
        }

        /// <summary>Abrir exige la fase de colocación, una carta propia ya colocada y cartas en la mesa.</summary>
        private static bool CanOpenBet(MatchState matchState, MatchPlayer viewer, int maximumBet)
        {
            bool isPlacingPhase = matchState.Phase == RoundPhase.PlacingCards;
            bool hasPlacedCard = viewer.PileCards.Count > 0;

            return isPlacingPhase && hasPlacedCard && maximumBet > NoBetValue;
        }

        private static bool CanRaiseBet(MatchState matchState, int maximumBet)
        {
            bool isBettingPhase = matchState.Phase == RoundPhase.Betting;

            return isBettingPhase && matchState.CurrentBet < maximumBet;
        }

        private static bool CanChoosePunishment(MatchState matchState, MatchPlayer viewer)
        {
            PendingPunishment pendingPunishment = matchState.PendingPunishment;
            bool canChoose = false;

            if (pendingPunishment != null)
            {
                bool isChooser = pendingPunishment.ChooserPlayerId == viewer.PlayerId;
                canChoose = isChooser && !pendingPunishment.RemainingTime.HasValue;
            }

            return canChoose;
        }
    }
}
