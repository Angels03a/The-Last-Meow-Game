using System.Collections.Generic;

using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Latest challenge outcome. The engine moves directly to another round or ends the match, so the registry keeps
    /// the outcome visible until the next opening bid or card placement. Change only under _sessionsLock.
    /// </summary>
    internal sealed class MatchChallengeRecord
    {
        private readonly List<MatchRevealedCardView> _revealedCards = new List<MatchRevealedCardView>();

        private MatchChallengeRecord(int bettorUserId, int promisedCount)
        {
            BettorUserId = bettorUserId;
            PromisedCount = promisedCount;
            Outcome = MatchChallengeOutcome.InProgress;
            PunishmentResolution = MatchPunishmentResolution.None;
        }

        /// <summary>Bidder's account.</summary>
        internal int BettorUserId { get; }

        /// <summary>Cards the bidder promised to reveal.</summary>
        internal int PromisedCount { get; }

        /// <summary>Challenge status or outcome.</summary>
        internal MatchChallengeOutcome Outcome { get; private set; }

        /// <summary>Cards in reveal order with their pile owners.</summary>
        internal IReadOnlyList<MatchRevealedCardView> RevealedCards => _revealedCards.AsReadOnly();

        /// <summary>Punished player's account, or 0 unless the challenge failed.</summary>
        internal int PunishedUserId { get; private set; }

        /// <summary>Chooser's account, or 0 unless the challenge failed.</summary>
        internal int PunishmentChooserUserId { get; private set; }

        /// <summary>Current or final punishment status.</summary>
        internal MatchPunishmentResolution PunishmentResolution { get; private set; }

        /// <summary>Creates a record for a new challenge.</summary>
        /// <param name="bettorUserId">Bidder's account.</param>
        /// <param name="promisedCount">Number of cards the bidder promised to reveal.</param>
        /// <returns>Challenge in progress with no revealed cards.</returns>
        internal static MatchChallengeRecord Begin(int bettorUserId, int promisedCount)
        {
            MatchChallengeRecord challengeRecord = new MatchChallengeRecord(bettorUserId, promisedCount);

            return challengeRecord;
        }

        /// <summary>Records one revealed card.</summary>
        /// <param name="pileOwnerUserId">Owner of the pile that supplied the card.</param>
        /// <param name="card">Revealed card type.</param>
        internal void RecordReveal(int pileOwnerUserId, MatchCardKind card)
        {
            _revealedCards.Add(new MatchRevealedCardView { PileOwnerUserId = pileOwnerUserId, Card = card });
        }

        /// <summary>Marks the challenge as successful, with no punishment.</summary>
        internal void MarkSucceeded()
        {
            Outcome = MatchChallengeOutcome.Succeeded;
        }

        /// <summary>Marks the challenge as failed and records the resulting punishment.</summary>
        /// <param name="chooserUserId">Player choosing the lost card.</param>
        /// <param name="punishedUserId">Player losing the card.</param>
        /// <param name="resolution">
        /// Pending while waiting for a choice, or SingleLifeNoChoice for removal without a choice.
        /// </param>
        internal void MarkFailed(int chooserUserId, int punishedUserId, MatchPunishmentResolution resolution)
        {
            Outcome = MatchChallengeOutcome.Failed;
            PunishmentChooserUserId = chooserUserId;
            PunishedUserId = punishedUserId;
            PunishmentResolution = resolution;
        }

        /// <summary>Records how a pending punishment ended.</summary>
        /// <param name="resolution">Final punishment result.</param>
        internal void ResolvePunishment(MatchPunishmentResolution resolution)
        {
            PunishmentResolution = resolution;
        }
    }
}
