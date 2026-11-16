using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de MatchState.GrantExtraLife: quién puede recibir la vida extra y cuántas veces.</summary>
    public class MatchStateExtraLifeTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int FirstPlayerId = 1;
        private const int SecondPlayerId = 2;
        private const int ThirdPlayerId = 3;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;

        /// <summary>Verifica GrantExtraLife_WithEightCardMode_GrantsItToThePlayer.</summary>
        [Fact]
        public void GrantExtraLife_WithEightCardMode_GrantsItToThePlayer()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(FirstPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.Granted, status);
            Assert.True(match.Players[0].HasExtraLife);
            Assert.Equal(EightCardsPerPlayer, match.Players[0].Lives);
        }

        /// <summary>Verifica GrantExtraLife_WithModeWithoutExtraLife_ReturnsModeDoesNotAllowExtraLife.</summary>
        [Theory]
        [InlineData(FourCardsPerPlayer)]
        [InlineData(SixCardsPerPlayer)]
        public void GrantExtraLife_WithModeWithoutExtraLife_ReturnsModeDoesNotAllowExtraLife(int cardsPerPlayerCount)
        {
            // Arrange
            MatchState match = CreateMatch(cardsPerPlayerCount);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(FirstPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ModeDoesNotAllowExtraLife, status);
            Assert.False(match.Players[0].HasExtraLife);
        }

        /// <summary>Verifica GrantExtraLife_WithAnotherPlayerAfterTheFirst_ReturnsExtraLifeAlreadyGranted.</summary>
        [Fact]
        public void GrantExtraLife_WithAnotherPlayerAfterTheFirst_ReturnsExtraLifeAlreadyGranted()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);
            match.GrantExtraLife(FirstPlayerId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(SecondPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ExtraLifeAlreadyGranted, status);
            Assert.False(match.Players[1].HasExtraLife);
        }

        /// <summary>Verifica GrantExtraLife_WithTheSamePlayerTwice_DoesNotAccumulateIt.</summary>
        [Fact]
        public void GrantExtraLife_WithTheSamePlayerTwice_DoesNotAccumulateIt()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);
            match.GrantExtraLife(FirstPlayerId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(FirstPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ExtraLifeAlreadyGranted, status);
            Assert.True(match.Players[0].HasExtraLife);
        }

        /// <summary>Verifica GrantExtraLife_AfterTheExtraLifeWasConsumed_ReturnsExtraLifeAlreadyGranted.</summary>
        [Fact]
        public void GrantExtraLife_AfterTheExtraLifeWasConsumed_ReturnsExtraLifeAlreadyGranted()
        {
            // Arrange: A[G] con vida extra apuesta 1 y el Gato la consume.
            MatchState match = CreateMatch(EightCardsPerPlayer);
            match.GrantExtraLife(FirstPlayerId);
            match.PlaceCard(FirstPlayerId, CardKind.FatalCat);
            match.PlaceCard(SecondPlayerId, CardKind.Safe);
            match.PlaceCard(ThirdPlayerId, CardKind.Safe);
            match.OpenBet(FirstPlayerId, OneCardBet);
            match.WithdrawFromBet(SecondPlayerId);
            match.WithdrawFromBet(ThirdPlayerId);
            match.RevealCard(FirstPlayerId, FirstPlayerId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(SecondPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ExtraLifeAlreadyGranted, status);
            Assert.False(match.Players[0].HasExtraLife);
            Assert.False(match.Players[1].HasExtraLife);
        }

        /// <summary>Verifica GrantExtraLife_WithUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void GrantExtraLife_WithUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(UnknownPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.PlayerNotInMatch, status);
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount)
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, new[] { FirstPlayerId, SecondPlayerId, ThirdPlayerId });

            return match;
        }
    }
}
