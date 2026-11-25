using System.Collections.Generic;
using System.Linq;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Player's hand, pile, successful challenges and extra life. Only the engine changes this state. Services reads
    /// it.
    /// </summary>
    public sealed class MatchPlayer
    {
        private const int LastIndexOffset = 1;
        private const int NoLives = 0;

        private readonly List<CardKind> _handCards = new List<CardKind>();
        private readonly List<CardKind> _pileCards = new List<CardKind>();

        /// <summary>Creates a player with the full hand for this mode and an empty pile.</summary>
        /// <param name="playerId">Player ID within the match.</param>
        /// <param name="modeRules">Mode rules that determine the starting hand.</param>
        internal MatchPlayer(int playerId, MatchModeRules modeRules)
        {
            PlayerId = playerId;
            HandCards = _handCards.AsReadOnly();
            PileCards = _pileCards.AsReadOnly();

            _handCards.AddRange(Enumerable.Repeat(CardKind.FatalCat, modeRules.FatalCatCount));
            _handCards.AddRange(Enumerable.Repeat(CardKind.Safe, modeRules.SafeCardCount));
        }

        /// <summary>Player ID within the match.</summary>
        public int PlayerId { get; }

        /// <summary>Unplayed cards, visible only to their owner.</summary>
        public IReadOnlyList<CardKind> HandCards { get; }

        /// <summary>Placed cards, face down, from the oldest to the top card.</summary>
        public IReadOnlyList<CardKind> PileCards { get; }

        /// <summary>Player's lives, equal to cards in hand plus cards in the pile.</summary>
        public int Lives => _handCards.Count + _pileCards.Count;

        /// <summary>
        /// Whether the player has zero lives. An empty hand with cards still in the pile does not eliminate the player.
        /// </summary>
        public bool IsEliminated => Lives == NoLives;

        /// <summary>
        /// Player's exit after leaving voluntarily, failing to reconnect or being voted out. Null while still in the
        /// match.
        /// </summary>
        public PlayerExit Exit { get; internal set; }

        /// <summary>Whether the player left. They stay in Players but can no longer take part.</summary>
        public bool HasLeftMatch => Exit != null;

        /// <summary>Whether the player is still active, having neither left nor been eliminated.</summary>
        public bool IsInPlay => !IsEliminated && !HasLeftMatch;

        /// <summary>Whether the player folded and is out of the current bidding round.</summary>
        public bool HasWithdrawnFromBet { get; internal set; }

        /// <summary>
        /// Number of pile cards revealed from the top in the current challenge, or 0 outside a challenge.
        /// </summary>
        public int RevealedCardCount { get; private set; }

        /// <summary>Player's successful challenges.</summary>
        public int SuccessfulChallengeCount { get; internal set; }

        /// <summary>
        /// Sequence number of the player's latest success, or 0 without a success. A lower number means they reached
        /// their current challenge count earlier (Rule 5.0.4).
        /// </summary>
        public int LastSuccessfulChallengeSequence { get; internal set; }

        /// <summary>Whether the player still has the extra life available only in eight-card mode.</summary>
        public bool HasExtraLife { get; internal set; }

        /// <summary>Face-down cards remaining in the pile.</summary>
        internal int UnrevealedCardCount => _pileCards.Count - RevealedCardCount;

        /// <summary>Moves a card from the hand to the top of the pile if the hand contains it.</summary>
        /// <param name="card">Card type to place.</param>
        /// <returns>True if placed, false if the hand has no such card.</returns>
        internal bool TryPlaceCard(CardKind card)
        {
            bool wasPlaced = _handCards.Remove(card);

            if (wasPlaced)
            {
                _pileCards.Add(card);
            }

            return wasPlaced;
        }

        /// <summary>Reveals the top hidden card. The pile must have a hidden card left.</summary>
        /// <returns>Type of card revealed.</returns>
        internal CardKind RevealTopCard()
        {
            CardKind revealedCard = _pileCards[UnrevealedCardCount - LastIndexOffset];
            RevealedCardCount++;

            return revealedCard;
        }

        /// <summary>Permanently removes a hand card at a valid index.</summary>
        /// <param name="handIndex">Card index in the hand.</param>
        /// <returns>Type of card removed.</returns>
        internal CardKind RemoveHandCardAt(int handIndex)
        {
            CardKind removedCard = _handCards[handIndex];
            _handCards.RemoveAt(handIndex);

            return removedCard;
        }

        /// <summary>Returns the pile to the hand, leaving an empty pile and no revealed cards.</summary>
        internal void ReturnPileToHand()
        {
            _handCards.AddRange(_pileCards);
            _pileCards.Clear();
            RevealedCardCount = 0;
        }
    }
}
