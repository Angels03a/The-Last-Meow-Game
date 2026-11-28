using System;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests challenge reveals, success, failure with pending punishment, extra life, match end and blocked actions.
    /// Uses placement, bidding and resolution from Domain v3 scenarios 1 to 4 (§4.4), up to the punishment itself.
    /// </summary>
    public class MatchStateChallengeTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int EightCardsPerPlayer = 8;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int ThreeCardBet = 3;
        private const int FourCardBet = 4;
        private const int NoCards = 0;
        private const int NoBet = 0;
        private const int NoCountedCards = 0;
        private const int OneCountedCard = 1;
        private const int TwoCountedCards = 2;
        private const int OneRevealedCard = 1;
        private const int NoChallenges = 0;
        private const int OneChallenge = 1;
        private const int TwoChallenges = 2;
        private const int FourChallenges = 4;
        private const int RoundsToFinishShortMatch = 2;
        private const int RoundsBeforeFourthWinInEightCardMode = 3;
        private const int RoundsBeforeThirdWinInEightCardMode = 2;
        private const int SpareRaiseValue = 2;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _nowUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Checks RevealCard_Scenario1WithThreeSafePiles_SucceedsOnThirdReveal.</summary>
        [Fact]
        public void RevealCard_Scenario1WithThreeSafePiles_SucceedsOnThirdReveal()
        {
            // Arrange: Piles A[S], B[S], C[S]. A bids 3 and the others fold.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus ownPileStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            ChallengeRevealStatus secondPileStatus = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            ChallengeRevealStatus thirdPileStatus = match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, ownPileStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, secondPileStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, thirdPileStatus);
        }

        /// <summary>Checks RevealCard_WhenChallengeSucceeds_AddsOneChallengeAndReturnsCardsToHands.</summary>
        [Fact]
        public void RevealCard_WhenChallengeSucceeds_AddsOneChallengeAndReturnsCardsToHands()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            PlaySuccessfulRound(match);

            // Assert
            Assert.Equal(OneChallenge, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
            Assert.All(match.Players, player => Assert.Equal(FourCardsPerPlayer, player.HandCards.Count));
            Assert.All(match.Players, player => Assert.Empty(player.PileCards));
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.RevealedCardCount));
            Assert.All(match.Players, player => Assert.Equal(FourCardsPerPlayer, player.Lives));
        }

        /// <summary>Checks RevealCard_Scenario3WhenBettorIsNotFirstOfOrder_StartsNextRoundWithFirstPlayer.</summary>
        [Fact]
        public void RevealCard_Scenario3WhenBettorIsNotFirstOfOrder_StartsNextRoundWithFirstPlayer()
        {
            // Arrange: Order C, A, B. Piles C[S,S], A[S], B[S]. A bids 4.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerCId, PlayerAId, PlayerBId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Act
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, lastStatus);
            Assert.Equal(PlayerCId, match.PlayerInTurn.PlayerId);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
        }

        /// <summary>Checks RevealCard_Scenario3WithPileFullyRevealed_RejectsWithoutChangingState.</summary>
        [Fact]
        public void RevealCard_Scenario3WithPileFullyRevealed_RejectsWithoutChangingState()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerCId, PlayerAId, PlayerBId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Act
            ChallengeRevealStatus repeatedStatus = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PileFullyRevealed, repeatedStatus);
            Assert.Equal(TwoCountedCards, match.Resolution.CountedCardCount);
            Assert.Equal(OneRevealedCard, GetPlayer(match, PlayerBId).RevealedCardCount);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
        }

        /// <summary>Checks RevealCard_Scenario2WithTwoOwnCards_SucceedsAfterOwnPileThenOtherPile.</summary>
        [Fact]
        public void RevealCard_Scenario2WithTwoOwnCards_SucceedsAfterOwnPileThenOtherPile()
        {
            // Arrange: Piles A[S,S], B[S,S], C[S]. C opens at 1, A raises to 4 and the others fold.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);

            // Act
            ChallengeRevealStatus firstOwnStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            ChallengeRevealStatus secondOwnStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            ChallengeRevealStatus firstOtherStatus = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            ChallengeRevealStatus secondOtherStatus = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, firstOwnStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, secondOwnStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, firstOtherStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, secondOtherStatus);
        }

        /// <summary>Checks RevealCard_Scenario2bWithCatUnderSafeCard_FailsOnSecondCardOfOtherPile.</summary>
        [Fact]
        public void RevealCard_Scenario2bWithCatUnderSafeCard_FailsOnSecondCardOfOtherPile()
        {
            // Arrange: B[G,S] has S on top. B's Fatal Cat is the fourth card revealed.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard, SafeCard, SafeCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Act
            ChallengeRevealStatus catStatus = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, catStatus);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Checks RevealCard_Scenario2bWhenChallengeFails_RegistersBlindPunishmentForPileOwner.</summary>
        [Fact]
        public void RevealCard_Scenario2bWhenChallengeFails_RegistersBlindPunishmentForPileOwner()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard, SafeCard, SafeCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Act
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(PlayerBId, match.PendingPunishment.ChooserPlayerId);
            Assert.Equal(PlayerAId, match.PendingPunishment.PunishedPlayerId);
            Assert.True(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Checks RevealCard_WhenChallengeFails_ReturnsEveryPlayedCardToHands.</summary>
        [Fact]
        public void RevealCard_WhenChallengeFails_ReturnsEveryPlayedCardToHands()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Act
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.All(match.Players, player => Assert.Equal(FourCardsPerPlayer, player.HandCards.Count));
            Assert.All(match.Players, player => Assert.Empty(player.PileCards));
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.RevealedCardCount));
            Assert.Equal(NoChallenges, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
        }

        /// <summary>Checks RevealCard_WithOtherPileBeforeOwnPile_ReturnsOwnPileFirstWithoutChangingState.</summary>
        [Fact]
        public void RevealCard_WithOtherPileBeforeOwnPile_ReturnsOwnPileFirstWithoutChangingState()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.OwnPileFirst, status);
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.RevealedCardCount));
        }

        /// <summary>Checks RevealCard_Scenario4WithCatUnderSafeCard_FailsOnCatEvenIfNumberWouldBeReached.</summary>
        [Fact]
        public void RevealCard_Scenario4WithCatUnderSafeCard_FailsOnCatEvenIfNumberWouldBeReached()
        {
            // Arrange: A[G,S] has S on top and bids 2. The Fatal Cat would meet the bid, but its effect is checked
            // first.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            ChallengeRevealStatus safeStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Act
            ChallengeRevealStatus catStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, safeStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, catStatus);
        }

        /// <summary>Checks RevealCard_Scenario4WhenBettorOwnsTheCat_RegistersOwnChoicePunishment.</summary>
        [Fact]
        public void RevealCard_Scenario4WhenBettorOwnsTheCat_RegistersOwnChoicePunishment()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Act
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(PlayerAId, match.PendingPunishment.ChooserPlayerId);
            Assert.Equal(PlayerAId, match.PendingPunishment.PunishedPlayerId);
            Assert.False(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Checks RevealCard_Scenario4bWithBetOfOne_SucceedsWithoutRevealingTheCat.</summary>
        [Fact]
        public void RevealCard_Scenario4bWithBetOfOne_SucceedsWithoutRevealingTheCat()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, status);
            Assert.Equal(SafeCard, match.Resolution.LastRevealedCard);
            Assert.Equal(OneCountedCard, match.Resolution.CountedCardCount);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Checks RevealCard_Scenario5WithExtraLife_ConsumesItAndCountsTheCat.</summary>
        [Fact]
        public void RevealCard_Scenario5WithExtraLife_ConsumesItAndCountsTheCat()
        {
            // Arrange: Order B, C, A. Piles A[S], B[S,G], C[S]. C opens at 1 and A raises to 3 with the extra life.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerBId, PlayerCId, PlayerAId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, FatalCatCard);
            RaiseAndConcede(match, OneCardBet, ThreeCardBet);

            // Act
            ChallengeRevealStatus ownStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            ChallengeRevealStatus catStatus = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, ownStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, catStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, lastStatus);
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(EightCardsPerPlayer, GetPlayer(match, PlayerBId).HandCards.Count);
        }

        /// <summary>Checks RevealCard_Scenario5bWithTableExhausted_SucceedsOnFourthReveal.</summary>
        [Fact]
        public void RevealCard_Scenario5bWithTableExhausted_SucceedsOnFourthReveal()
        {
            // Arrange: Bid 4 with four cards on the table. Reveals are S, G (absorbed), S and S.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerBId, PlayerCId, PlayerAId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, FatalCatCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Act
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, lastStatus);
            Assert.Equal(OneChallenge, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
        }

        /// <summary>Checks RevealCard_Scenario5cWithSingleCatOnOwnPile_SucceedsWithConsumedCat.</summary>
        [Fact]
        public void RevealCard_Scenario5cWithSingleCatOnOwnPile_SucceedsWithConsumedCat()
        {
            // Arrange: A[G] bids 1 with an extra life.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, status);
            Assert.Equal(FatalCatCard, match.Resolution.LastRevealedCard);
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Checks RevealCard_Scenario6WithoutExtraLife_FailsOnFirstCat.</summary>
        [Fact]
        public void RevealCard_Scenario6WithoutExtraLife_FailsOnFirstCat()
        {
            // Arrange: A[S,G,G] bids 2 without an extra life.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceCatsOnOwnPileOfFirstPlayer(match);
            OpenAndConcede(match, TwoCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, status);
            Assert.Equal(PlayerAId, match.PendingPunishment.ChooserPlayerId);
            Assert.False(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Checks RevealCard_Scenario6WithExtraLife_FailsOnSecondCatBeforeCompleting.</summary>
        [Fact]
        public void RevealCard_Scenario6WithExtraLife_FailsOnSecondCatBeforeCompleting()
        {
            // Arrange: A[S,G,G] bids 3 with an extra life that absorbs the first Fatal Cat.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.GrantExtraLife(PlayerAId);
            PlaceCatsOnOwnPileOfFirstPlayer(match);
            OpenAndConcede(match, ThreeCardBet);
            ChallengeRevealStatus firstCatStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Act
            ChallengeRevealStatus secondCatStatus = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, firstCatStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, secondCatStatus);
        }

        /// <summary>Checks RevealCard_Scenario6WhenChallengeFailsAfterExtraLife_KeepsExtraLifeConsumed.</summary>
        [Fact]
        public void RevealCard_Scenario6WhenChallengeFailsAfterExtraLife_KeepsExtraLifeConsumed()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.GrantExtraLife(PlayerAId);
            PlaceCatsOnOwnPileOfFirstPlayer(match);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Act
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Equal(PlayerAId, match.PendingPunishment.ChooserPlayerId);
            Assert.False(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Checks RevealCard_Scenario6bWithCatsOnOtherPile_RegistersBlindPunishmentForPileOwner.</summary>
        [Fact]
        public void RevealCard_Scenario6bWithCatsOnOtherPile_RegistersBlindPunishmentForPileOwner()
        {
            // Arrange: Order B, C, A with A[S] and B[G,G]. A bids 3 with the extra life. S counts, the first G is
            // absorbed and the second G fails.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerBId, PlayerCId, PlayerAId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, FatalCatCard);
            RaiseAndConcede(match, OneCardBet, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, status);
            Assert.Equal(PlayerBId, match.PendingPunishment.ChooserPlayerId);
            Assert.Equal(PlayerAId, match.PendingPunishment.PunishedPlayerId);
            Assert.True(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Checks RevealCard_WithSecondSuccessInShortMode_FinishesMatchWithWinner.</summary>
        [Fact]
        public void RevealCard_WithSecondSuccessInShortMode_FinishesMatchWithWinner()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaySuccessfulRound(match);

            // Act
            ChallengeRevealStatus status = PlaySuccessfulRound(match);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Same(GetPlayer(match, PlayerAId), match.Winner);
            Assert.Equal(TwoChallenges, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
        }

        /// <summary>Checks RevealCard_WithFourthSuccessInEightCardMode_FinishesMatchWithWinner.</summary>
        [Fact]
        public void RevealCard_WithFourthSuccessInEightCardMode_FinishesMatchWithWinner()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlayRoundsToSuccess(match, RoundsBeforeFourthWinInEightCardMode);

            // Act
            ChallengeRevealStatus status = PlaySuccessfulRound(match);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(FourChallenges, match.Winner.SuccessfulChallengeCount);
        }

        /// <summary>Checks RevealCard_WithThirdSuccessInEightCardMode_KeepsMatchInProgress.</summary>
        [Fact]
        public void RevealCard_WithThirdSuccessInEightCardMode_KeepsMatchInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlayRoundsToSuccess(match, RoundsBeforeThirdWinInEightCardMode);

            // Act
            PlaySuccessfulRound(match);

            // Assert
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Null(match.Winner);
        }

        /// <summary>Checks RevealCard_WithoutChallenge_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_WithoutChallenge_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Checks RevealCard_ByPlayerWhoIsNotTheBettor_ReturnsNotTheBettor.</summary>
        [Fact]
        public void RevealCard_ByPlayerWhoIsNotTheBettor_ReturnsNotTheBettor()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerBId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NotTheBettor, status);
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
        }

        /// <summary>Checks RevealCard_ByUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void RevealCard_ByUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(UnknownPlayerId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PlayerNotInMatch, status);
        }

        /// <summary>Checks RevealCard_WithUnknownPileOwner_ReturnsPileOwnerNotInMatch.</summary>
        [Fact]
        public void RevealCard_WithUnknownPileOwner_ReturnsPileOwnerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, UnknownPlayerId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PileOwnerNotInMatch, status);
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
        }

        /// <summary>Checks RevealCard_AfterSuccess_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_AfterSuccess_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaySuccessfulRound(match);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Checks RevealCard_WithPunishmentPending_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_WithPunishmentPending_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatchWithPunishmentPending();

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Checks PlaceCard_WithPunishmentPending_ReturnsPunishmentPending.</summary>
        [Fact]
        public void PlaceCard_WithPunishmentPending_ReturnsPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchWithPunishmentPending();

            // Act
            CardPlacementStatus status = match.PlaceCard(PlayerAId, SafeCard);

            // Assert
            Assert.Equal(CardPlacementStatus.PunishmentPending, status);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).HandCards.Count);
        }

        /// <summary>Checks OpenBet_WithPunishmentPending_ReturnsPunishmentPending.</summary>
        [Fact]
        public void OpenBet_WithPunishmentPending_ReturnsPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchWithPunishmentPending();

            // Act
            BetActionStatus status = match.OpenBet(PlayerAId, OneCardBet);

            // Assert
            Assert.Equal(BetActionStatus.PunishmentPending, status);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Checks RaiseBet_WithPunishmentPending_ReturnsPunishmentPending.</summary>
        [Fact]
        public void RaiseBet_WithPunishmentPending_ReturnsPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchWithPunishmentPending();

            // Act
            BetActionStatus status = match.RaiseBet(PlayerAId, SpareRaiseValue);

            // Assert
            Assert.Equal(BetActionStatus.PunishmentPending, status);
        }

        /// <summary>Checks WithdrawFromBet_WithPunishmentPending_ReturnsPunishmentPending.</summary>
        [Fact]
        public void WithdrawFromBet_WithPunishmentPending_ReturnsPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchWithPunishmentPending();

            // Act
            BetActionStatus status = match.WithdrawFromBet(PlayerAId);

            // Assert
            Assert.Equal(BetActionStatus.PunishmentPending, status);
        }

        /// <summary>Checks PlaceCard_WithMatchFinished_ReturnsMatchFinished.</summary>
        [Fact]
        public void PlaceCard_WithMatchFinished_ReturnsMatchFinished()
        {
            // Arrange
            MatchState match = CreateFinishedShortMatch();

            // Act
            CardPlacementStatus status = match.PlaceCard(PlayerAId, SafeCard);

            // Assert
            Assert.Equal(CardPlacementStatus.MatchFinished, status);
        }

        /// <summary>Checks OpenBet_WithMatchFinished_ReturnsMatchFinished.</summary>
        [Fact]
        public void OpenBet_WithMatchFinished_ReturnsMatchFinished()
        {
            // Arrange
            MatchState match = CreateFinishedShortMatch();

            // Act
            BetActionStatus status = match.OpenBet(PlayerAId, OneCardBet);

            // Assert
            Assert.Equal(BetActionStatus.MatchFinished, status);
        }

        /// <summary>Checks Resolution_BeforeAnyChallenge_IsNull.</summary>
        [Fact]
        public void Resolution_BeforeAnyChallenge_IsNull()
        {
            // Act
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Assert
            Assert.Null(match.Resolution);
            Assert.Null(match.PendingPunishment);
            Assert.Null(match.Winner);
        }

        /// <summary>Checks Resolution_WhenChallengeIsReady_StartsWithoutCountedCards.</summary>
        [Fact]
        public void Resolution_WhenChallengeIsReady_StartsWithoutCountedCards()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);

            // Act
            OpenAndConcede(match, ThreeCardBet);

            // Assert
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
            Assert.Null(match.Resolution.LastRevealedCard);
            Assert.Null(match.Resolution.LastRevealedPlayerId);
        }

        /// <summary>Checks RevealCard_WithSafeCard_RecordsLastRevealedCardAndItsOwner.</summary>
        [Fact]
        public void RevealCard_WithSafeCard_RecordsLastRevealedCardAndItsOwner()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Act
            match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Assert
            Assert.Equal(SafeCard, match.Resolution.LastRevealedCard);
            Assert.Equal(PlayerCId, match.Resolution.LastRevealedPlayerId);
            Assert.Equal(TwoCountedCards, match.Resolution.CountedCardCount);
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount, params int[] playerIdsInOrder)
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, playerIdsInOrder);

            return match;
        }

        private static MatchPlayer GetPlayer(MatchState match, int playerId)
        {
            MatchPlayer foundPlayer = null;

            foreach (MatchPlayer player in match.Players)
            {
                if (player.PlayerId == playerId)
                {
                    foundPlayer = player;
                }
            }

            return foundPlayer;
        }

        private static void PlaceInTurn(MatchState match, params CardKind[] cards)
        {
            foreach (CardKind card in cards)
            {
                match.PlaceCard(match.PlayerInTurn.PlayerId, card);
            }
        }

        private static void WithdrawUntilChallengeReady(MatchState match)
        {
            int maximumWithdrawals = match.Players.Count;

            for (int withdrawal = 0; withdrawal < maximumWithdrawals && match.Phase == RoundPhase.Betting;
                withdrawal++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                BetActionStatus status = match.WithdrawFromBet(blockingPlayerId);
                Assert.True(status == BetActionStatus.Accepted,
                    $"El jugador {blockingPlayerId} no pudo retirarse en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.Phase == RoundPhase.ChallengeReady,
                $"El reto no quedó listo en la ronda {match.RoundNumber}: fase {match.Phase}, turno de "
                + $"{match.PlayerInTurn.PlayerId}.");
        }

        /// <summary>The current player opens and all others fold, leaving the opener as the final bidder.</summary>
        private static void OpenAndConcede(MatchState match, int betValue)
        {
            int openingPlayerId = match.PlayerInTurn.PlayerId;
            BetActionStatus openStatus = match.OpenBet(openingPlayerId, betValue);
            Assert.True(openStatus == BetActionStatus.Accepted,
                $"El jugador {openingPlayerId} no pudo abrir la apuesta de {betValue} en la ronda "
                + $"{match.RoundNumber}: {openStatus}.");
            WithdrawUntilChallengeReady(match);
        }

        /// <summary>
        /// The current player opens, the next raises and the others fold. The player who raised takes the challenge.
        /// </summary>
        private static void RaiseAndConcede(MatchState match, int openingBet, int raisedBet)
        {
            match.OpenBet(match.PlayerInTurn.PlayerId, openingBet);
            match.RaiseBet(match.PlayerInTurn.PlayerId, raisedBet);
            WithdrawUntilChallengeReady(match);
        }

        /// <summary>Places A[S,G,G], B[S,S,S] and C[S,S,S], leaving A's Fatal Cat on top.</summary>
        private static void PlaceCatsOnOwnPileOfFirstPlayer(MatchState match)
        {
            PlaceInTurn(
                match,
                SafeCard,
                SafeCard,
                SafeCard,
                FatalCatCard,
                SafeCard,
                SafeCard,
                FatalCatCard,
                SafeCard,
                SafeCard);
        }

        /// <summary>Plays a round with one safe card per pile. A bids 3 and succeeds.</summary>
        private static ChallengeRevealStatus PlaySuccessfulRound(MatchState match)
        {
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            return lastStatus;
        }

        private static void PlayRoundsToSuccess(MatchState match, int roundCount)
        {
            for (int roundIndex = 0; roundIndex < roundCount; roundIndex++)
            {
                PlaySuccessfulRound(match);
            }
        }

        private static MatchState CreateMatchWithPunishmentPending()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }

        private static MatchState CreateFinishedShortMatch()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlayRoundsToSuccess(match, RoundsToFinishShortMatch);

            return match;
        }
    }
}
