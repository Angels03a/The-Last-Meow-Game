using System;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int SingleLife = 1;
        private const int OnlyCardIndex = 0;

        /// <summary>
        /// Voltea la carta de arriba sin voltear de la pila de un jugador durante la resolución del reto. Primero se
        /// voltea la pila propia del apostador y después elige el dueño antes de cada volteo. El reto termina al
        /// alcanzar el número prometido (éxito) o al revelar un Gato sin vida extra (fallo). Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que pide voltear; debe ser el apostador.</param>
        /// <param name="pileOwnerId">Dueño de la pila de la que se voltea.</param>
        /// <param name="nowUtc">Hora UTC del servidor; si el reto falla, de ella sale el plazo del castigo.</param>
        /// <returns>Revealed, ChallengeSucceeded o ChallengeFailed; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        public ChallengeRevealStatus RevealCard(int playerId, int pileOwnerId, DateTime nowUtc)
        {
            ValidateUtc(nowUtc);
            ChallengeRevealStatus revealStatus;
            MatchPlayer pileOwner = FindPlayer(pileOwnerId);

            if (FindPlayer(playerId) == null)
            {
                revealStatus = ChallengeRevealStatus.PlayerNotInMatch;
            }
            else if (Phase != RoundPhase.ChallengeReady)
            {
                revealStatus = ChallengeRevealStatus.NoChallengeInProgress;
            }
            else if (playerId != BetLeader.PlayerId)
            {
                revealStatus = ChallengeRevealStatus.NotTheBettor;
            }
            else if (pileOwner == null)
            {
                revealStatus = ChallengeRevealStatus.PileOwnerNotInMatch;
            }
            else
            {
                revealStatus = Resolution.RevealCard(pileOwner);
                ApplyChallengeOutcome(revealStatus, nowUtc);
            }

            return revealStatus;
        }

        /// <summary>Cierra la resolución según su desenlace; mientras el reto continúa no cambia nada.</summary>
        private void ApplyChallengeOutcome(ChallengeRevealStatus revealStatus, DateTime nowUtc)
        {
            if (revealStatus == ChallengeRevealStatus.ChallengeSucceeded)
            {
                CompleteChallengeSuccess();
            }
            else if (revealStatus == ChallengeRevealStatus.ChallengeFailed)
            {
                CompleteChallengeFailure(nowUtc);
            }
        }

        /// <summary>
        /// Reto exitoso: suma un reto al apostador, le asigna la siguiente secuencia de éxito y todas las cartas
        /// jugadas vuelven a las manos. Si llegó a los retos de la modalidad, la partida termina con su victoria; si
        /// no, empieza la siguiente ronda.
        /// </summary>
        private void CompleteChallengeSuccess()
        {
            MatchPlayer bettor = BetLeader;
            Resolution.ReturnPilesToHands();
            bettor.SuccessfulChallengeCount++;
            _successfulChallengeSequence++;
            bettor.LastSuccessfulChallengeSequence = _successfulChallengeSequence;

            if (bettor.SuccessfulChallengeCount >= ModeRules.SuccessfulChallengesToWin)
            {
                Winner = bettor;
                ResetBetting();
                Phase = RoundPhase.MatchFinished;
            }
            else
            {
                StartNextRound();
            }
        }

        /// <summary>
        /// Reto fallido: todas las cartas jugadas vuelven a las manos y empieza el castigo pendiente, con plazo de
        /// PunishmentChoiceSeconds. Elige el dueño de la pila del Gato, a ciegas si no es el propio apostador. Si el
        /// apostador tiene una sola vida no hay nada que elegir: la carta se elimina en la misma transición.
        /// </summary>
        private void CompleteChallengeFailure(DateTime nowUtc)
        {
            MatchPlayer bettor = BetLeader;
            MatchPlayer chooser = Resolution.FatalCatOwner;
            bool isBlindChoice = !ReferenceEquals(chooser, bettor);
            DateTime deadlineUtc = nowUtc.AddSeconds(ValidationRules.PunishmentChoiceSeconds);
            Resolution.ReturnPilesToHands();
            PendingPunishment = new PendingPunishment(chooser.PlayerId, bettor, isBlindChoice, deadlineUtc);
            Phase = RoundPhase.PunishmentPending;

            if (bettor.Lives == SingleLife)
            {
                ApplyPunishment(OnlyCardIndex);
            }
        }
    }
}
