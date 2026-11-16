using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la resolución del reto en MatchState: volteo de cartas, éxito, fallo con castigo pendiente, vida
    /// extra, fin de la partida y bloqueo de acciones. Los escenarios 1 a 4 del Dominio v3 (§4.4) se usan en su parte
    /// de colocación, apuesta y resolución, sin el castigo mismo.
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

        /// <summary>Verifica RevealCard_Scenario1WithThreeSafePiles_SucceedsOnThirdReveal.</summary>
        [Fact]
        public void RevealCard_Scenario1WithThreeSafePiles_SucceedsOnThirdReveal()
        {
            // Arrange: pilas A[S], B[S], C[S]; A apuesta 3 y los demás se retiran.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus ownPileStatus = match.RevealCard(PlayerAId, PlayerAId);
            ChallengeRevealStatus secondPileStatus = match.RevealCard(PlayerAId, PlayerBId);
            ChallengeRevealStatus thirdPileStatus = match.RevealCard(PlayerAId, PlayerCId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, ownPileStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, secondPileStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, thirdPileStatus);
        }

        /// <summary>Verifica RevealCard_WhenChallengeSucceeds_AddsOneChallengeAndReturnsCardsToHands.</summary>
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
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.PileCards.Count));
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.RevealedCardCount));
            Assert.All(match.Players, player => Assert.Equal(FourCardsPerPlayer, player.Lives));
        }

        /// <summary>Verifica RevealCard_Scenario3WhenBettorIsNotFirstOfOrder_StartsNextRoundWithFirstPlayer.</summary>
        [Fact]
        public void RevealCard_Scenario3WhenBettorIsNotFirstOfOrder_StartsNextRoundWithFirstPlayer()
        {
            // Arrange: orden C, A, B; pilas C[S,S], A[S], B[S]; A apuesta 4.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerCId, PlayerAId, PlayerBId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);
            match.RevealCard(PlayerAId, PlayerCId);

            // Act
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, lastStatus);
            Assert.Equal(PlayerCId, match.PlayerInTurn.PlayerId);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
        }

        /// <summary>Verifica RevealCard_Scenario3WithPileFullyRevealed_RejectsWithoutChangingState.</summary>
        [Fact]
        public void RevealCard_Scenario3WithPileFullyRevealed_RejectsWithoutChangingState()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerCId, PlayerAId, PlayerBId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);

            // Act
            ChallengeRevealStatus repeatedStatus = match.RevealCard(PlayerAId, PlayerBId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PileFullyRevealed, repeatedStatus);
            Assert.Equal(TwoCountedCards, match.Resolution.CountedCardCount);
            Assert.Equal(OneRevealedCard, GetPlayer(match, PlayerBId).RevealedCardCount);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
        }

        /// <summary>Verifica RevealCard_Scenario2WithTwoOwnCards_SucceedsAfterOwnPileThenOtherPile.</summary>
        [Fact]
        public void RevealCard_Scenario2WithTwoOwnCards_SucceedsAfterOwnPileThenOtherPile()
        {
            // Arrange: pilas A[S,S], B[S,S], C[S]; C abre con 1, A sube a 4 y los demás se retiran.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);

            // Act
            ChallengeRevealStatus firstOwnStatus = match.RevealCard(PlayerAId, PlayerAId);
            ChallengeRevealStatus secondOwnStatus = match.RevealCard(PlayerAId, PlayerAId);
            ChallengeRevealStatus firstOtherStatus = match.RevealCard(PlayerAId, PlayerBId);
            ChallengeRevealStatus secondOtherStatus = match.RevealCard(PlayerAId, PlayerBId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, firstOwnStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, secondOwnStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, firstOtherStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, secondOtherStatus);
        }

        /// <summary>Verifica RevealCard_Scenario2bWithCatUnderSafeCard_FailsOnSecondCardOfOtherPile.</summary>
        [Fact]
        public void RevealCard_Scenario2bWithCatUnderSafeCard_FailsOnSecondCardOfOtherPile()
        {
            // Arrange: B[G,S] (S arriba); la cuarta carta que se voltea es el Gato de B.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard, SafeCard, SafeCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);

            // Act
            ChallengeRevealStatus catStatus = match.RevealCard(PlayerAId, PlayerBId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, catStatus);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica RevealCard_Scenario2bWhenChallengeFails_RegistersBlindPunishmentForPileOwner.</summary>
        [Fact]
        public void RevealCard_Scenario2bWhenChallengeFails_RegistersBlindPunishmentForPileOwner()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard, SafeCard, SafeCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);

            // Act
            match.RevealCard(PlayerAId, PlayerBId);

            // Assert
            Assert.Equal(PlayerBId, match.PendingPunishment.ChooserPlayerId);
            Assert.Equal(PlayerAId, match.PendingPunishment.PunishedPlayerId);
            Assert.True(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Verifica RevealCard_WhenChallengeFails_ReturnsEveryPlayedCardToHands.</summary>
        [Fact]
        public void RevealCard_WhenChallengeFails_ReturnsEveryPlayedCardToHands()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId);

            // Act
            match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.All(match.Players, player => Assert.Equal(FourCardsPerPlayer, player.HandCards.Count));
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.PileCards.Count));
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.RevealedCardCount));
            Assert.Equal(NoChallenges, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
        }

        /// <summary>Verifica RevealCard_WithOtherPileBeforeOwnPile_ReturnsOwnPileFirstWithoutChangingState.</summary>
        [Fact]
        public void RevealCard_WithOtherPileBeforeOwnPile_ReturnsOwnPileFirstWithoutChangingState()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerBId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.OwnPileFirst, status);
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
            Assert.All(match.Players, player => Assert.Equal(NoCards, player.RevealedCardCount));
        }

        /// <summary>Verifica RevealCard_Scenario4WithCatUnderSafeCard_FailsOnCatEvenIfNumberWouldBeReached.</summary>
        [Fact]
        public void RevealCard_Scenario4WithCatUnderSafeCard_FailsOnCatEvenIfNumberWouldBeReached()
        {
            // Arrange: A[G,S] (S arriba) apuesta 2: el Gato completaría el número, pero el Gato se evalúa primero.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            ChallengeRevealStatus safeStatus = match.RevealCard(PlayerAId, PlayerAId);

            // Act
            ChallengeRevealStatus catStatus = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, safeStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, catStatus);
        }

        /// <summary>Verifica RevealCard_Scenario4WhenBettorOwnsTheCat_RegistersOwnChoicePunishment.</summary>
        [Fact]
        public void RevealCard_Scenario4WhenBettorOwnsTheCat_RegistersOwnChoicePunishment()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId);

            // Act
            match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(PlayerAId, match.PendingPunishment.ChooserPlayerId);
            Assert.Equal(PlayerAId, match.PendingPunishment.PunishedPlayerId);
            Assert.False(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Verifica RevealCard_Scenario4bWithBetOfOne_SucceedsWithoutRevealingTheCat.</summary>
        [Fact]
        public void RevealCard_Scenario4bWithBetOfOne_SucceedsWithoutRevealingTheCat()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, status);
            Assert.Equal(SafeCard, match.Resolution.LastRevealedCard);
            Assert.Equal(OneCountedCard, match.Resolution.CountedCardCount);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Verifica RevealCard_Scenario5WithExtraLife_ConsumesItAndCountsTheCat.</summary>
        [Fact]
        public void RevealCard_Scenario5WithExtraLife_ConsumesItAndCountsTheCat()
        {
            // Arrange: orden B, C, A; A[S], B[S,G], C[S]; C abre con 1, A sube a 3 y tiene la vida extra.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerBId, PlayerCId, PlayerAId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, FatalCatCard);
            RaiseAndConcede(match, OneCardBet, ThreeCardBet);

            // Act
            ChallengeRevealStatus ownStatus = match.RevealCard(PlayerAId, PlayerAId);
            ChallengeRevealStatus catStatus = match.RevealCard(PlayerAId, PlayerBId);
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, ownStatus);
            Assert.Equal(ChallengeRevealStatus.Revealed, catStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, lastStatus);
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(EightCardsPerPlayer, GetPlayer(match, PlayerBId).HandCards.Count);
        }

        /// <summary>Verifica RevealCard_Scenario5bWithTableExhausted_SucceedsOnFourthReveal.</summary>
        [Fact]
        public void RevealCard_Scenario5bWithTableExhausted_SucceedsOnFourthReveal()
        {
            // Arrange: apuesta de 4 con 4 cartas en la mesa: S, G (consumido), S y S.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerBId, PlayerCId, PlayerAId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, FatalCatCard);
            RaiseAndConcede(match, OneCardBet, FourCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);
            match.RevealCard(PlayerAId, PlayerBId);

            // Act
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, lastStatus);
            Assert.Equal(OneChallenge, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
        }

        /// <summary>Verifica RevealCard_Scenario5cWithSingleCatOnOwnPile_SucceedsWithConsumedCat.</summary>
        [Fact]
        public void RevealCard_Scenario5cWithSingleCatOnOwnPile_SucceedsWithConsumedCat()
        {
            // Arrange: A[G] con vida extra apuesta 1.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeSucceeded, status);
            Assert.Equal(FatalCatCard, match.Resolution.LastRevealedCard);
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Verifica RevealCard_Scenario6WithoutExtraLife_FailsOnFirstCat.</summary>
        [Fact]
        public void RevealCard_Scenario6WithoutExtraLife_FailsOnFirstCat()
        {
            // Arrange: A[S,G,G] sin vida extra apuesta 2.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceCatsOnOwnPileOfFirstPlayer(match);
            OpenAndConcede(match, TwoCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, status);
            Assert.Equal(PlayerAId, match.PendingPunishment.ChooserPlayerId);
            Assert.False(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Verifica RevealCard_Scenario6WithExtraLife_FailsOnSecondCatBeforeCompleting.</summary>
        [Fact]
        public void RevealCard_Scenario6WithExtraLife_FailsOnSecondCatBeforeCompleting()
        {
            // Arrange: A[S,G,G] con vida extra apuesta 3; el primer Gato se absorbe.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.GrantExtraLife(PlayerAId);
            PlaceCatsOnOwnPileOfFirstPlayer(match);
            OpenAndConcede(match, ThreeCardBet);
            ChallengeRevealStatus firstCatStatus = match.RevealCard(PlayerAId, PlayerAId);

            // Act
            ChallengeRevealStatus secondCatStatus = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.Revealed, firstCatStatus);
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, secondCatStatus);
        }

        /// <summary>Verifica RevealCard_Scenario6WhenChallengeFailsAfterExtraLife_KeepsExtraLifeConsumed.</summary>
        [Fact]
        public void RevealCard_Scenario6WhenChallengeFailsAfterExtraLife_KeepsExtraLifeConsumed()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.GrantExtraLife(PlayerAId);
            PlaceCatsOnOwnPileOfFirstPlayer(match);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId);

            // Act
            match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Equal(PlayerAId, match.PendingPunishment.ChooserPlayerId);
            Assert.False(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Verifica RevealCard_Scenario6bWithCatsOnOtherPile_RegistersBlindPunishmentForPileOwner.</summary>
        [Fact]
        public void RevealCard_Scenario6bWithCatsOnOtherPile_RegistersBlindPunishmentForPileOwner()
        {
            // Arrange: orden B, C, A; A[S], B[G,G]; con vida extra A apuesta 3: S, G (absorbido) y G (falla).
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerBId, PlayerCId, PlayerAId);
            match.GrantExtraLife(PlayerAId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, FatalCatCard);
            RaiseAndConcede(match, OneCardBet, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerBId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, status);
            Assert.Equal(PlayerBId, match.PendingPunishment.ChooserPlayerId);
            Assert.Equal(PlayerAId, match.PendingPunishment.PunishedPlayerId);
            Assert.True(match.PendingPunishment.IsBlindChoice);
        }

        /// <summary>Verifica RevealCard_WithSecondSuccessInShortMode_FinishesMatchWithWinner.</summary>
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

        /// <summary>Verifica RevealCard_WithFourthSuccessInEightCardMode_FinishesMatchWithWinner.</summary>
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

        /// <summary>Verifica RevealCard_WithThirdSuccessInEightCardMode_KeepsMatchInProgress.</summary>
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

        /// <summary>Verifica RevealCard_WithoutChallenge_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_WithoutChallenge_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Verifica RevealCard_ByPlayerWhoIsNotTheBettor_ReturnsNotTheBettor.</summary>
        [Fact]
        public void RevealCard_ByPlayerWhoIsNotTheBettor_ReturnsNotTheBettor()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerBId, PlayerBId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NotTheBettor, status);
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
        }

        /// <summary>Verifica RevealCard_ByUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void RevealCard_ByUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(UnknownPlayerId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PlayerNotInMatch, status);
        }

        /// <summary>Verifica RevealCard_WithUnknownPileOwner_ReturnsPileOwnerNotInMatch.</summary>
        [Fact]
        public void RevealCard_WithUnknownPileOwner_ReturnsPileOwnerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, UnknownPlayerId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PileOwnerNotInMatch, status);
            Assert.Equal(NoCountedCards, match.Resolution.CountedCardCount);
        }

        /// <summary>Verifica RevealCard_AfterSuccess_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_AfterSuccess_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaySuccessfulRound(match);

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Verifica RevealCard_WithPunishmentPending_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_WithPunishmentPending_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatchWithPunishmentPending();

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Verifica PlaceCard_WithPunishmentPending_ReturnsPunishmentPending.</summary>
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

        /// <summary>Verifica OpenBet_WithPunishmentPending_ReturnsPunishmentPending.</summary>
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

        /// <summary>Verifica RaiseBet_WithPunishmentPending_ReturnsPunishmentPending.</summary>
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

        /// <summary>Verifica WithdrawFromBet_WithPunishmentPending_ReturnsPunishmentPending.</summary>
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

        /// <summary>Verifica PlaceCard_WithMatchFinished_ReturnsMatchFinished.</summary>
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

        /// <summary>Verifica OpenBet_WithMatchFinished_ReturnsMatchFinished.</summary>
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

        /// <summary>Verifica Resolution_BeforeAnyChallenge_IsNull.</summary>
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

        /// <summary>Verifica Resolution_WhenChallengeIsReady_StartsWithoutCountedCards.</summary>
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

        /// <summary>Verifica RevealCard_WithSafeCard_RecordsLastRevealedCardAndItsOwner.</summary>
        [Fact]
        public void RevealCard_WithSafeCard_RecordsLastRevealedCardAndItsOwner()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId);

            // Act
            match.RevealCard(PlayerAId, PlayerCId);

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
            while (match.Phase == RoundPhase.Betting)
            {
                match.WithdrawFromBet(match.PlayerInTurn.PlayerId);
            }
        }

        /// <summary>Quien tiene el turno abre la apuesta y todos los demás se retiran: el que abre apuesta.</summary>
        private static void OpenAndConcede(MatchState match, int betValue)
        {
            match.OpenBet(match.PlayerInTurn.PlayerId, betValue);
            WithdrawUntilChallengeReady(match);
        }

        /// <summary>Quien tiene el turno abre, el siguiente sube y los demás se retiran: el que sube apuesta.</summary>
        private static void RaiseAndConcede(MatchState match, int openingBet, int raisedBet)
        {
            match.OpenBet(match.PlayerInTurn.PlayerId, openingBet);
            match.RaiseBet(match.PlayerInTurn.PlayerId, raisedBet);
            WithdrawUntilChallengeReady(match);
        }

        /// <summary>Coloca A[S,G,G], B[S,S,S] y C[S,S,S]: el Gato de A queda arriba de su pila.</summary>
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

        /// <summary>Juega una ronda con tres pilas de una carta segura en la que A apuesta 3 y la cumple.</summary>
        private static ChallengeRevealStatus PlaySuccessfulRound(MatchState match)
        {
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerBId);
            ChallengeRevealStatus lastStatus = match.RevealCard(PlayerAId, PlayerCId);

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
            match.RevealCard(PlayerAId, PlayerAId);
            match.RevealCard(PlayerAId, PlayerAId);

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
