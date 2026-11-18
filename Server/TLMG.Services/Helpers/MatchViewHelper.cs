using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Traduce el motor al contrato: arma la vista privada de cada jugador y convierte cartas, fases y rechazos. Solo
    /// copia valores; nunca conserva referencias al MatchState ni a sus listas.
    /// </summary>
    internal static class MatchViewHelper
    {
        private const int NoUserId = 0;

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

        private static readonly Dictionary<BetActionStatus, string> _errorKeysByBetStatus =
            new Dictionary<BetActionStatus, string>
            {
                { BetActionStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { BetActionStatus.NotPlayersTurn, MatchErrorKeys.NotYourTurn },
                { BetActionStatus.BetInProgress, MatchErrorKeys.BetInProgress },
                { BetActionStatus.NoBetInProgress, MatchErrorKeys.NoBetInProgress },
                { BetActionStatus.TableEmpty, MatchErrorKeys.TableEmpty },
                { BetActionStatus.EmptyHandRequiresBetOfOne, MatchErrorKeys.EmptyHandBet },
                { BetActionStatus.BetValueNotValid, MatchErrorKeys.BetValueNotValid },
                { BetActionStatus.NoCardPlacedInRound, MatchErrorKeys.NoCardPlaced },
                { BetActionStatus.PunishmentPending, MatchErrorKeys.PunishmentPending },
                { BetActionStatus.MatchFinished, MatchErrorKeys.MatchFinished }
            };

        private static readonly HashSet<ChallengeRevealStatus> _acceptedRevealStatuses =
            new HashSet<ChallengeRevealStatus>
            {
                ChallengeRevealStatus.Revealed,
                ChallengeRevealStatus.ChallengeSucceeded,
                ChallengeRevealStatus.ChallengeFailed
            };

        private static readonly Dictionary<ChallengeRevealStatus, string> _errorKeysByRevealStatus =
            new Dictionary<ChallengeRevealStatus, string>
            {
                { ChallengeRevealStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { ChallengeRevealStatus.NoChallengeInProgress, MatchErrorKeys.NoChallenge },
                { ChallengeRevealStatus.NotTheBettor, MatchErrorKeys.NotTheBettor },
                { ChallengeRevealStatus.PileOwnerNotInMatch, MatchErrorKeys.PileOwnerNotInMatch },
                { ChallengeRevealStatus.OwnPileFirst, MatchErrorKeys.OwnPileFirst },
                { ChallengeRevealStatus.PileFullyRevealed, MatchErrorKeys.PileEmpty }
            };

        private static readonly Dictionary<PunishmentStatus, string> _errorKeysByPunishmentStatus =
            new Dictionary<PunishmentStatus, string>
            {
                { PunishmentStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch },
                { PunishmentStatus.NoPunishmentPending, MatchErrorKeys.NoPunishmentPending },
                { PunishmentStatus.NotTheChooser, MatchErrorKeys.NotTheChooser },
                { PunishmentStatus.CardIndexNotValid, MatchErrorKeys.CardIndexNotValid }
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
        /// Arma la vista de un destinatario: lo visible para todos y, solo del propio destinatario, su mano, su pila y
        /// las acciones que puede hacer. Requiere que el llamador tenga serializado el acceso al MatchState (bajo
        /// _sessionsLock). Nunca incluye los tipos de cartas ocultas de otro jugador (Regla No. 2).
        /// </summary>
        /// <param name="context">Estado del motor y datos que el registro guarda fuera de él.</param>
        /// <param name="viewerUserId">Jugador que recibirá la vista.</param>
        /// <returns>Una vista nueva, sin referencias al motor.</returns>
        internal static MatchView CreateMatchView(MatchViewContext context, int viewerUserId)
        {
            MatchState matchState = context.MatchState;
            MatchPlayer viewer = matchState.Players.FirstOrDefault(player => player.PlayerId == viewerUserId);
            MatchView matchView = new MatchView
            {
                MatchId = context.MatchId,
                Version = context.Version,
                RoundNumber = matchState.RoundNumber,
                Phase = ConvertToContractPhase(matchState.Phase),
                PlayerInTurnUserId = FindPlayerInTurnUserId(matchState),
                Players = CreatePlayerViews(matchState, context.NicknamesByUserId),
                OwnHandCards = ConvertToContractCards(viewer?.HandCards),
                OwnPileCards = ConvertToContractCards(viewer?.PileCards),
                CurrentBet = matchState.CurrentBet,
                BetLeaderUserId = matchState.BetLeader?.PlayerId ?? NoUserId,
                ChallengesToWin = matchState.ModeRules.SuccessfulChallengesToWin,
                WinnerUserId = matchState.Winner?.PlayerId ?? NoUserId,
                CardsOnTable = matchState.CardsOnTable,
                Challenge = CreateChallengeView(context.LastChallenge),
                Punishment = CreatePunishmentView(matchState)
            };
            MatchActionAvailabilityHelper.ApplyAvailability(matchView, matchState, viewer);

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

        /// <summary>Traduce el estado devuelto por el motor al abrir, subir o retirarse de una apuesta.</summary>
        /// <param name="betStatus">Resultado del motor.</param>
        /// <returns>null si la acción se aplicó; la clave de error en cualquier otro caso.</returns>
        internal static string ConvertToErrorKey(BetActionStatus betStatus)
        {
            string errorKey;

            if (betStatus == BetActionStatus.Accepted)
            {
                errorKey = null;
            }
            else if (!_errorKeysByBetStatus.TryGetValue(betStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        /// <summary>Traduce el estado devuelto por el motor al voltear una carta del reto.</summary>
        /// <param name="revealStatus">Resultado del motor.</param>
        /// <returns>null si la carta se volteó (aunque el reto termine); la clave de error si se rechazó.</returns>
        internal static string ConvertToErrorKey(ChallengeRevealStatus revealStatus)
        {
            string errorKey;

            if (_acceptedRevealStatuses.Contains(revealStatus))
            {
                errorKey = null;
            }
            else if (!_errorKeysByRevealStatus.TryGetValue(revealStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        /// <summary>Traduce el estado devuelto por el motor al elegir la carta del castigo.</summary>
        /// <param name="punishmentStatus">Resultado del motor.</param>
        /// <returns>null si la carta se eliminó; la clave de error en cualquier otro caso.</returns>
        internal static string ConvertToErrorKey(PunishmentStatus punishmentStatus)
        {
            string errorKey;

            if (punishmentStatus == PunishmentStatus.CardRemoved)
            {
                errorKey = null;
            }
            else if (!_errorKeysByPunishmentStatus.TryGetValue(punishmentStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
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
                    IsEliminated = player.IsEliminated,
                    SuccessfulChallengeCount = player.SuccessfulChallengeCount,
                    HasWithdrawnFromBet = player.HasWithdrawnFromBet,
                    HasLeftMatch = player.HasLeftMatch,
                    RevealedPileCards = ConvertToContractCards(FindRevealedPileCards(player))
                });
            }

            return playerViews;
        }

        /// <summary>
        /// Jugador al que se le muestra el turno: quien tiene el turno; quien debe elegir mientras hay un castigo
        /// pendiente; nadie cuando la partida terminó (el motor ya no da significado al turno).
        /// </summary>
        private static int FindPlayerInTurnUserId(MatchState matchState)
        {
            int playerInTurnUserId = matchState.PlayerInTurn.PlayerId;

            if (matchState.Phase == RoundPhase.PunishmentPending)
            {
                playerInTurnUserId = matchState.PendingPunishment.ChooserPlayerId;
            }
            else if (matchState.Phase == RoundPhase.MatchFinished)
            {
                playerInTurnUserId = NoUserId;
            }

            return playerInTurnUserId;
        }

        /// <summary>Cartas volteadas de la pila de un jugador, de arriba hacia abajo; información pública.</summary>
        private static IReadOnlyList<CardKind> FindRevealedPileCards(MatchPlayer player)
        {
            IReadOnlyList<CardKind> revealedCards = player.PileCards
                .Reverse()
                .Take(player.RevealedCardCount)
                .ToList()
                .AsReadOnly();

            return revealedCards;
        }

        private static MatchChallengeView CreateChallengeView(MatchChallengeRecord challengeRecord)
        {
            MatchChallengeView challengeView = null;

            if (challengeRecord != null)
            {
                challengeView = new MatchChallengeView
                {
                    BettorUserId = challengeRecord.BettorUserId,
                    PromisedCount = challengeRecord.PromisedCount,
                    Outcome = challengeRecord.Outcome,
                    RevealedCards = challengeRecord.RevealedCards
                        .Select(card => new MatchRevealedCardView
                        {
                            PileOwnerUserId = card.PileOwnerUserId,
                            Card = card.Card
                        })
                        .ToList(),
                    PunishedUserId = challengeRecord.PunishedUserId,
                    PunishmentChooserUserId = challengeRecord.PunishmentChooserUserId,
                    PunishmentResolution = challengeRecord.PunishmentResolution
                };
            }

            return challengeView;
        }

        /// <summary>Castigo pendiente sin ninguna carta: quién elige, a quién castigan y entre cuántas.</summary>
        private static MatchPunishmentView CreatePunishmentView(MatchState matchState)
        {
            MatchPunishmentView punishmentView = null;
            PendingPunishment pendingPunishment = matchState.PendingPunishment;

            if (pendingPunishment != null)
            {
                MatchPlayer punishedPlayer = matchState.Players
                    .First(player => player.PlayerId == pendingPunishment.PunishedPlayerId);
                punishmentView = new MatchPunishmentView
                {
                    ChooserUserId = pendingPunishment.ChooserPlayerId,
                    PunishedUserId = pendingPunishment.PunishedPlayerId,
                    IsBlindChoice = pendingPunishment.IsBlindChoice,
                    CardSlotCount = punishedPlayer.HandCards.Count
                };
            }

            return punishmentView;
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
