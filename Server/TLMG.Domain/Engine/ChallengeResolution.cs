using System.Collections.Generic;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// The bidder reveals one card per call, starting with their own pile and then choosing a pile for each reveal.
    /// Stop at the promised count or a Fatal Cat that the extra life cannot absorb. MatchState creates and manages the
    /// resolution when the challenge is ready. Resolution uses neither a clock nor randomness.
    /// </summary>
    public sealed class ChallengeResolution
    {
        private readonly MatchPlayer _bettor;
        private readonly int _promisedCardCount;
        private readonly IReadOnlyList<MatchPlayer> _players;

        /// <summary>Creates the bidder's challenge resolution.</summary>
        /// <param name="bettor">Highest bidder.</param>
        /// <param name="promisedCardCount">Number of cards the bidder promised to reveal.</param>
        /// <param name="players">Match players whose cards return to their hands at the end.</param>
        internal ChallengeResolution(MatchPlayer bettor, int promisedCardCount, IReadOnlyList<MatchPlayer> players)
        {
            _bettor = bettor;
            _promisedCardCount = promisedCardCount;
            _players = players;
        }

        /// <summary>Revealed cards that count toward the bid, including an absorbed Fatal Cat.</summary>
        public int CountedCardCount { get; private set; }

        /// <summary>Last card revealed, or null before the first reveal.</summary>
        public CardKind? LastRevealedCard { get; private set; }

        /// <summary>Owner of the last revealed card's pile, or null before the first reveal.</summary>
        public int? LastRevealedPlayerId { get; private set; }

        /// <summary>Owner of the Fatal Cat pile that caused failure, or null if the challenge did not fail.</summary>
        internal MatchPlayer FatalCatOwner { get; private set; }

        /// <summary>Reveals the top hidden card of the chosen pile if the reveal order allows it.</summary>
        /// <param name="pileOwner">Owner of the chosen pile. Required.</param>
        /// <returns>Revealed, ChallengeSucceeded or ChallengeFailed, or the reason for rejection.</returns>
        internal ChallengeRevealStatus RevealCard(MatchPlayer pileOwner)
        {
            ChallengeRevealStatus revealStatus;

            if (pileOwner.UnrevealedCardCount == 0)
            {
                revealStatus = ChallengeRevealStatus.PileFullyRevealed;
            }
            else if (_bettor.UnrevealedCardCount > 0 && !ReferenceEquals(pileOwner, _bettor))
            {
                revealStatus = ChallengeRevealStatus.OwnPileFirst;
            }
            else
            {
                revealStatus = ApplyReveal(pileOwner);
            }

            return revealStatus;
        }

        /// <summary>Returns each player's placed cards to their hand.</summary>
        internal void ReturnPilesToHands()
        {
            foreach (MatchPlayer player in _players)
            {
                player.ReturnPileToHand();
            }
        }

        /// <summary>
        /// Checks a revealed Fatal Cat first. Without an extra life, the challenge fails before the card counts.
        /// </summary>
        private ChallengeRevealStatus ApplyReveal(MatchPlayer pileOwner)
        {
            CardKind revealedCard = pileOwner.RevealTopCard();
            ChallengeRevealStatus revealStatus;
            LastRevealedCard = revealedCard;
            LastRevealedPlayerId = pileOwner.PlayerId;

            if (revealedCard == CardKind.FatalCat && !_bettor.HasExtraLife)
            {
                FatalCatOwner = pileOwner;
                revealStatus = ChallengeRevealStatus.ChallengeFailed;
            }
            else
            {
                revealStatus = CountRevealedCard(revealedCard);
            }

            return revealStatus;
        }

        /// <summary>
        /// Counts the revealed card. A Fatal Cat reaching this step uses the extra life and counts toward the bid.
        /// </summary>
        private ChallengeRevealStatus CountRevealedCard(CardKind revealedCard)
        {
            ChallengeRevealStatus revealStatus = ChallengeRevealStatus.Revealed;

            if (revealedCard == CardKind.FatalCat)
            {
                _bettor.HasExtraLife = false;
            }

            CountedCardCount++;

            if (CountedCardCount >= _promisedCardCount)
            {
                revealStatus = ChallengeRevealStatus.ChallengeSucceeded;
            }

            return revealStatus;
        }
    }
}
