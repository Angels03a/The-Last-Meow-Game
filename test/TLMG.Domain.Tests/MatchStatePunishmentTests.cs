using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas del castigo pendiente en MatchState: plazo, elección del jugador, elección al azar al vencer, pérdida
    /// de la carta, eliminación, fin por último jugador en pie y turnos que omiten a los eliminados.
    /// </summary>
    public class MatchStatePunishmentTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int PlayerDId = 4;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int ThreeCardBet = 3;
        private const int PunishmentChoiceSeconds = 10;
        private const int OneTick = 1;
        private const int ThirdCardIndex = 2;
        private const int FirstCardIndex = 0;
        private const int NegativeCardIndex = -1;
        private const int ThreeLives = 3;
        private const int FiveLives = 5;
        private const int SafeCardsToLoseBeforeLastLife = 3;
        private const int OneChallenge = 1;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _nowUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime _deadlineUtc = _nowUtc.AddSeconds(PunishmentChoiceSeconds);
        private static readonly DateTime _beforeDeadlineUtc = _deadlineUtc.AddTicks(-OneTick);

        /// <summary>Verifica RevealCard_WhenChallengeFails_SetsDeadlineAfterPunishmentChoiceSeconds.</summary>
        [Fact]
        public void RevealCard_WhenChallengeFails_SetsDeadlineAfterPunishmentChoiceSeconds()
        {
            // Arrange
            MatchState match = CreateBlindPunishmentReadyToFail();

            // Act
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            // Assert
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemovedCard);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica ExpirePunishment_Scenario2WithBlindChoice_RemovesTheRandomCard.</summary>
        [Fact]
        public void ExpirePunishment_Scenario2WithBlindChoice_RemovesTheRandomCard()
        {
            // Arrange: 6 cartas; A falla con el Gato de C y C no elige a tiempo.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, FatalCatCard);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerCId, _nowUtc);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, cardCount => ThirdCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(FiveLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>Verifica ChoosePunishmentCard_Scenario2AfterRandomChoice_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ChoosePunishmentCard_Scenario2AfterRandomChoice_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, SafeCard, FatalCatCard);
            OpenAndConcede(match, ThreeCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerCId, _nowUtc);
            match.ExpirePunishment(_deadlineUtc, cardCount => ThirdCardIndex);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerCId, FirstCardIndex, _deadlineUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(FiveLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ExpirePunishment_Scenario2WithOwnCat_CanRemoveTheCat.</summary>
        [Fact]
        public void ExpirePunishment_Scenario2WithOwnCat_CanRemoveTheCat()
        {
            // Arrange
            MatchState match = CreateOwnPunishment(SixCardsPerPlayer);
            int catIndex = IndexOfCard(match, PlayerAId, FatalCatCard);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, cardCount => catIndex);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(FatalCatCard, match.LastPunishment.RemovedCard);
            Assert.DoesNotContain(FatalCatCard, GetPlayer(match, PlayerAId).HandCards);
        }

        /// <summary>Verifica ChoosePunishmentCard_Scenario3WithBlindChoiceOfTheCat_LeavesThreeSafeCards.</summary>
        [Fact]
        public void ChoosePunishmentCard_Scenario3WithBlindChoiceOfTheCat_LeavesThreeSafeCards()
        {
            // Arrange: B elige a ciegas y le toca el Gato de A.
            MatchState match = CreateBlindPunishment();
            int catIndex = IndexOfCard(match, PlayerAId, FatalCatCard);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, catIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.All(GetPlayer(match, PlayerAId).HandCards, card => Assert.Equal(SafeCard, card));
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ChoosePunishmentCard_WithBlindChoice_StartsNextRoundWithFirstActivePlayer.</summary>
        [Fact]
        public void ChoosePunishmentCard_WithBlindChoice_StartsNextRoundWithFirstActivePlayer()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _nowUtc);

            // Assert
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(PlayerAId, match.PlayerInTurn.PlayerId);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(PlayerAId, match.LastPunishment.PunishedPlayerId);
        }

        /// <summary>Verifica ChoosePunishmentCard_WithOwnChoice_RemovesTheChosenSafeCard.</summary>
        [Fact]
        public void ChoosePunishmentCard_WithOwnChoice_RemovesTheChosenSafeCard()
        {
            // Arrange
            MatchState match = CreateOwnPunishment(FourCardsPerPlayer);
            int safeIndex = IndexOfCard(match, PlayerAId, SafeCard);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerAId, safeIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(SafeCard, match.LastPunishment.RemovedCard);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(PlayerAId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica RevealCard_Scenario4WithOneLife_EliminatesInTheSameOperation.</summary>
        [Fact]
        public void RevealCard_Scenario4WithOneLife_EliminatesInTheSameOperation()
        {
            // Arrange: orden A, B, C, D; A con solo su Gato lo coloca y abre con exactamente 1.
            MatchState match = CreateLastLifeChallengeOfFirstPlayer();

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.ChallengeFailed, status);
            Assert.True(GetPlayer(match, PlayerAId).IsEliminated);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(FatalCatCard, match.LastPunishment.RemovedCard);
            Assert.Equal(new[] { PlayerAId }, match.EliminatedPlayers.Select(player => player.PlayerId));
        }

        /// <summary>Verifica RevealCard_Scenario4WithFirstOfOrderEliminated_StartsRoundWithNextActive.</summary>
        [Fact]
        public void RevealCard_Scenario4WithFirstOfOrderEliminated_StartsRoundWithNextActive()
        {
            // Arrange
            MatchState match = CreateLastLifeChallengeOfFirstPlayer();

            // Act
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica PlaceCard_WithEliminatedPlayer_SkipsHimInTheTurnOrder.</summary>
        [Fact]
        public void PlaceCard_WithEliminatedPlayer_SkipsHimInTheTurnOrder()
        {
            // Arrange
            MatchState match = CreateMatchWithFirstPlayerEliminated();

            // Act
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);

            // Assert
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica WithdrawFromBet_WithEliminatedPlayer_DoesNotCountHimAsBettor.</summary>
        [Fact]
        public void WithdrawFromBet_WithEliminatedPlayer_DoesNotCountHimAsBettor()
        {
            // Arrange
            MatchState match = CreateMatchWithFirstPlayerEliminated();
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard);
            match.OpenBet(PlayerBId, OneCardBet);
            match.WithdrawFromBet(PlayerCId);

            // Act
            match.WithdrawFromBet(PlayerDId);

            // Assert
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(PlayerBId, match.BetLeader.PlayerId);
        }

        /// <summary>Verifica OpenBet_WithEliminatedPlayerNextInOrder_PassesTurnToNextActive.</summary>
        [Fact]
        public void OpenBet_WithEliminatedPlayerNextInOrder_PassesTurnToNextActive()
        {
            // Arrange
            MatchState match = CreateMatchWithFirstPlayerEliminated();
            PlaceInTurn(match, SafeCard, SafeCard, SafeCard, SafeCard, SafeCard);

            // Act: abre D y el siguiente del orden es A, que está eliminado.
            match.OpenBet(PlayerDId, OneCardBet);

            // Assert
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica RevealCard_Scenario5WithLastSurvivor_FinishesMatchWithHisVictory.</summary>
        [Fact]
        public void RevealCard_Scenario5WithLastSurvivor_FinishesMatchWithHisVictory()
        {
            // Arrange: B ya eliminado, C con 1 reto y A con una sola vida (su Gato).
            MatchState match = CreateMatchBeforeLastSurvivor();

            // Act
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            // Assert
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(PlayerCId, match.Winner.PlayerId);
            Assert.Equal(OneChallenge, match.Winner.SuccessfulChallengeCount);
            Assert.Equal(
                new[] { PlayerBId, PlayerAId },
                match.EliminatedPlayers.Select(player => player.PlayerId));
        }

        /// <summary>Verifica IsEliminated_WithEmptyHandAndCardsInPile_ReturnsFalse.</summary>
        [Fact]
        public void IsEliminated_WithEmptyHandAndCardsInPile_ReturnsFalse()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            PlaceInTurn(
                match,
                FatalCatCard,
                FatalCatCard,
                FatalCatCard,
                SafeCard,
                SafeCard,
                SafeCard,
                SafeCard,
                SafeCard,
                SafeCard,
                SafeCard,
                SafeCard);

            // Assert
            Assert.Empty(GetPlayer(match, PlayerAId).HandCards);
            Assert.False(GetPlayer(match, PlayerAId).IsEliminated);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica GrantExtraLife_AfterFirstCardPlaced_ReturnsCardAlreadyPlaced.</summary>
        [Fact]
        public void GrantExtraLife_AfterFirstCardPlaced_ReturnsCardAlreadyPlaced()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.PlaceCard(PlayerAId, SafeCard);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(PlayerBId);

            // Assert
            Assert.Equal(ExtraLifeStatus.CardAlreadyPlaced, status);
            Assert.False(GetPlayer(match, PlayerBId).HasExtraLife);
        }

        /// <summary>Verifica ChoosePunishmentCard_ByPlayerWhoIsNotTheChooser_ReturnsNotTheChooser.</summary>
        [Fact]
        public void ChoosePunishmentCard_ByPlayerWhoIsNotTheChooser_ReturnsNotTheChooser()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            PendingPunishment punishment = match.PendingPunishment;

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerAId, FirstCardIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NotTheChooser, status);
            Assert.Same(punishment, match.PendingPunishment);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ChoosePunishmentCard_ByUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void ChoosePunishmentCard_ByUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(UnknownPlayerId, FirstCardIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, status);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica ChoosePunishmentCard_ASecondTime_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ChoosePunishmentCard_ASecondTime_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _nowUtc);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ChoosePunishmentCard_WithIndexOutOfHand_ReturnsCardIndexNotValid.</summary>
        [Theory]
        [InlineData(NegativeCardIndex)]
        [InlineData(FourCardsPerPlayer)]
        public void ChoosePunishmentCard_WithIndexOutOfHand_ReturnsCardIndexNotValid(int cardIndex)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, cardIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.CardIndexNotValid, status);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica ChoosePunishmentCard_WithoutPunishment_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ChoosePunishmentCard_WithoutPunishment_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerAId, FirstCardIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>Verifica ChoosePunishmentCard_OneTickBeforeDeadline_RemovesTheCard.</summary>
        [Fact]
        public void ChoosePunishmentCard_OneTickBeforeDeadline_RemovesTheCard()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _beforeDeadlineUtc);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
        }

        /// <summary>Verifica ChoosePunishmentCard_AtDeadline_ReturnsDeadlineExpired.</summary>
        [Fact]
        public void ChoosePunishmentCard_AtDeadline_ReturnsDeadlineExpired()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _deadlineUtc);

            // Assert
            Assert.Equal(PunishmentStatus.DeadlineExpired, status);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).Lives);
            Assert.NotNull(match.PendingPunishment);
        }

        /// <summary>Verifica ExpirePunishment_OneTickBeforeDeadline_ReturnsDeadlineNotReached.</summary>
        [Fact]
        public void ExpirePunishment_OneTickBeforeDeadline_ReturnsDeadlineNotReached()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.ExpirePunishment(_beforeDeadlineUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.DeadlineNotReached, status);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).Lives);
            Assert.NotNull(match.PendingPunishment);
        }

        /// <summary>Verifica ExpirePunishment_WithoutPunishment_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ExpirePunishment_WithoutPunishment_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
        }

        /// <summary>Verifica ExpirePunishment_WithNullRandomFunction_ThrowsArgumentNullException.</summary>
        [Fact]
        public void ExpirePunishment_WithNullRandomFunction_ThrowsArgumentNullException()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            Exception thrown = Record.Exception(() => match.ExpirePunishment(_deadlineUtc, null));

            // Assert
            Assert.IsType<ArgumentNullException>(thrown);
            Assert.NotNull(match.PendingPunishment);
        }

        /// <summary>Verifica ExpirePunishment_WithRandomIndexOutOfRange_ThrowsWithoutChangingState.</summary>
        [Theory]
        [InlineData(NegativeCardIndex)]
        [InlineData(FourCardsPerPlayer)]
        public void ExpirePunishment_WithRandomIndexOutOfRange_ThrowsWithoutChangingState(int randomIndex)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            Exception thrown = Record.Exception(() => match.ExpirePunishment(_deadlineUtc, cardCount => randomIndex));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
            Assert.Equal(FourCardsPerPlayer, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica ChoosePunishmentCard_WithTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ChoosePunishmentCard_WithTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            DateTime notUtcTime = DateTime.SpecifyKind(_nowUtc, timeKind);

            // Act
            Exception thrown = Record.Exception(
                () => match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, notUtcTime));

            // Assert
            Assert.IsType<ArgumentException>(thrown);
            Assert.NotNull(match.PendingPunishment);
        }

        /// <summary>Verifica ExpirePunishment_WithTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ExpirePunishment_WithTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            DateTime notUtcTime = DateTime.SpecifyKind(_deadlineUtc, timeKind);

            // Act
            Exception thrown = Record.Exception(() => match.ExpirePunishment(notUtcTime, cardCount => FirstCardIndex));

            // Assert
            Assert.IsType<ArgumentException>(thrown);
            Assert.NotNull(match.PendingPunishment);
        }

        /// <summary>Verifica RevealCard_WithTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void RevealCard_WithTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            MatchState match = CreateBlindPunishmentReadyToFail();
            DateTime notUtcTime = DateTime.SpecifyKind(_nowUtc, timeKind);

            // Act
            Exception thrown = Record.Exception(() => match.RevealCard(PlayerAId, PlayerBId, notUtcTime));

            // Assert
            Assert.IsType<ArgumentException>(thrown);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
        }

        /// <summary>Verifica PlaceCard_AfterLastSurvivorWins_ReturnsMatchFinished.</summary>
        [Fact]
        public void PlaceCard_AfterLastSurvivorWins_ReturnsMatchFinished()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            CardPlacementStatus status = match.PlaceCard(PlayerCId, SafeCard);

            // Assert
            Assert.Equal(CardPlacementStatus.MatchFinished, status);
        }

        /// <summary>Verifica OpenBet_AfterLastSurvivorWins_ReturnsMatchFinished.</summary>
        [Fact]
        public void OpenBet_AfterLastSurvivorWins_ReturnsMatchFinished()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            BetActionStatus status = match.OpenBet(PlayerCId, OneCardBet);

            // Assert
            Assert.Equal(BetActionStatus.MatchFinished, status);
        }

        /// <summary>Verifica RevealCard_AfterLastSurvivorWins_ReturnsNoChallengeInProgress.</summary>
        [Fact]
        public void RevealCard_AfterLastSurvivorWins_ReturnsNoChallengeInProgress()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            ChallengeRevealStatus status = match.RevealCard(PlayerCId, PlayerCId, _nowUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, status);
        }

        /// <summary>Verifica ChoosePunishmentCard_AfterLastSurvivorWins_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ChoosePunishmentCard_AfterLastSurvivorWins_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerCId, FirstCardIndex, _nowUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
        }

        /// <summary>Verifica ExpirePunishment_AfterLastSurvivorWins_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ExpirePunishment_AfterLastSurvivorWins_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount, params int[] playerIdsInOrder)
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, playerIdsInOrder);

            return match;
        }

        private static MatchPlayer GetPlayer(MatchState match, int playerId)
        {
            return match.Players.First(player => player.PlayerId == playerId);
        }

        private static int IndexOfCard(MatchState match, int playerId, CardKind card)
        {
            return GetPlayer(match, playerId).HandCards.ToList().IndexOf(card);
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

        /// <summary>Quien tiene el turno abre la apuesta y todos los demás se retiran: el que abre apuesta.</summary>
        private static void OpenAndConcede(MatchState match, int betValue)
        {
            int openingPlayerId = match.PlayerInTurn.PlayerId;
            BetActionStatus openStatus = match.OpenBet(openingPlayerId, betValue);
            Assert.True(openStatus == BetActionStatus.Accepted,
                $"El jugador {openingPlayerId} no pudo abrir la apuesta de {betValue} en la ronda "
                + $"{match.RoundNumber}: {openStatus}.");
            WithdrawUntilChallengeReady(match);
        }

        /// <summary>4 cartas, A[S], B[G], C[S]; A apuesta 2 y ya volteó su S: el siguiente volteo de B falla.</summary>
        private static MatchState CreateBlindPunishmentReadyToFail()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }

        /// <summary>Castigo pendiente en el que B elige a ciegas una de las 4 cartas de A.</summary>
        private static MatchState CreateBlindPunishment()
        {
            MatchState match = CreateBlindPunishmentReadyToFail();
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            return match;
        }

        /// <summary>A[G], B[S], C[S]; A apuesta 1 y revela su propio Gato: A elige viendo sus cartas.</summary>
        private static MatchState CreateOwnPunishment(int cardsPerPlayerCount)
        {
            MatchState match = CreateMatch(cardsPerPlayerCount, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }

        /// <summary>
        /// Con A primero del orden, A coloca su Gato, los demás una segura, A apuesta 1, revela su Gato y elige perder
        /// una carta segura; se repite las veces indicadas.
        /// </summary>
        private static void LoseSafeCardsOnOwnCat(MatchState match, int roundCount)
        {
            for (int roundIndex = 0; roundIndex < roundCount; roundIndex++)
            {
                PlaceInTurn(match, FatalCatCard);
                PlaceSafeCardsUntilTurnOf(match, PlayerAId);
                OpenAndConcede(match, OneCardBet);
                match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
                match.ChoosePunishmentCard(PlayerAId, IndexOfCard(match, PlayerAId, SafeCard), _nowUtc);
            }
        }

        private static void PlaceSafeCardsUntilTurnOf(MatchState match, int playerId)
        {
            int maximumPlacements = match.Players.Count;

            for (int placement = 0; placement < maximumPlacements && match.PlayerInTurn.PlayerId != playerId;
                placement++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                CardPlacementStatus status = match.PlaceCard(blockingPlayerId, SafeCard);
                Assert.True(status == CardPlacementStatus.Placed,
                    $"El jugador {blockingPlayerId} no pudo colocar en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.PlayerInTurn.PlayerId == playerId,
                $"El turno quedó en {match.PlayerInTurn.PlayerId} en la ronda {match.RoundNumber}, no en {playerId}.");
        }

        /// <summary>Orden A, B, C, D; A pierde sus tres seguras y apuesta 1 sobre su último Gato.</summary>
        private static MatchState CreateLastLifeChallengeOfFirstPlayer()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LoseSafeCardsOnOwnCat(match, SafeCardsToLoseBeforeLastLife);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);

            return match;
        }

        /// <summary>A revela su último Gato: queda eliminado en la misma operación y la ronda la abre B.</summary>
        private static MatchState CreateMatchWithFirstPlayerEliminated()
        {
            MatchState match = CreateLastLifeChallengeOfFirstPlayer();
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }

        /// <summary>
        /// Orden A, B, C con 4 cartas: B pierde sus cuatro cartas en retos fallidos sobre su propio Gato, C gana un
        /// reto y A queda con una sola vida (su Gato) en un reto listo de apuesta 1 sobre su propia pila.
        /// </summary>
        private static MatchState CreateMatchBeforeLastSurvivor()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            EliminateSecondPlayer(match);
            PlaceInTurn(match, SafeCard, SafeCard);
            match.OpenBet(PlayerAId, OneCardBet);
            match.RaiseBet(PlayerCId, TwoCardBet);
            match.WithdrawFromBet(PlayerAId);
            match.RevealCard(PlayerCId, PlayerCId, _nowUtc);
            match.RevealCard(PlayerCId, PlayerAId, _nowUtc);
            LoseSafeCardsOnOwnCat(match, SafeCardsToLoseBeforeLastLife);
            PlaceInTurn(match, FatalCatCard, SafeCard);
            OpenAndConcede(match, OneCardBet);

            return match;
        }

        /// <summary>Hasta que B queda eliminado: en cada ronda B apuesta 1 sobre su Gato y pierde una carta.</summary>
        private static void EliminateSecondPlayer(MatchState match)
        {
            int maximumLosses = match.ModeRules.CardsPerPlayerCount;

            for (int loss = 0; loss < maximumLosses && !GetPlayer(match, PlayerBId).IsEliminated; loss++)
            {
                PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard, SafeCard);
                OpenAndConcede(match, OneCardBet);
                match.RevealCard(PlayerBId, PlayerBId, _nowUtc);
                ChooseSafeCardIfPending(match, PlayerBId);
            }

            Assert.True(GetPlayer(match, PlayerBId).IsEliminated,
                $"El jugador {PlayerBId} no quedó eliminado en la ronda {match.RoundNumber}.");
        }

        private static void ChooseSafeCardIfPending(MatchState match, int playerId)
        {
            if (match.Phase == RoundPhase.PunishmentPending)
            {
                match.ChoosePunishmentCard(playerId, IndexOfCard(match, playerId, SafeCard), _nowUtc);
            }
        }

        private static MatchState CreateMatchFinishedByLastSurvivor()
        {
            MatchState match = CreateMatchBeforeLastSurvivor();
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }
    }
}
