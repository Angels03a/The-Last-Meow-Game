using System;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int SingleLife = 1;
        private const int OnlyCardIndex = 0;

        /// <summary>
        /// Reveals the top hidden card from a pile. The bidder reveals their own pile first, then chooses a pile owner
        /// before every reveal. Reaching the bid succeeds. Revealing a Fatal Cat without an extra life fails. Rejection
        /// leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player requesting the reveal. Must be the bidder.</param>
        /// <param name="pileOwnerId">Owner of the pile to reveal.</param>
        /// <param name="nowUtc">Server time in UTC, used to set the punishment deadline if the challenge fails.</param>
        /// <returns>Revealed, ChallengeSucceeded or ChallengeFailed, or the reason for rejection.</returns>
        /// <exception cref="ArgumentException">nowUtc is not a UTC time.</exception>
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

        /// <summary>
        /// Closes the challenge according to its outcome. A continuing challenge leaves state unchanged.
        /// </summary>
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
        /// On success, credits the bidder with a challenge and the next success sequence number. Returns played cards
        /// to all hands. The bidder wins on reaching the mode's target. Otherwise the next round starts.
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
        /// On failure, returns played cards to all hands and starts punishment with a PunishmentChoiceSeconds deadline.
        /// The Fatal Cat's owner chooses blindly if they are not the bidder. A bidder with one life loses the card
        /// immediately without a choice.
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
