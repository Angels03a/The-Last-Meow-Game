using System;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de MatchModeRules.</summary>
    public class MatchModeRulesTests
    {
        /// <summary>Verifica CreateFromCardsPerPlayerCount_WithSupportedCount_ReturnsModeData.</summary>
        [Theory]
        [InlineData(4, 1, 3, 2)]
        [InlineData(6, 1, 5, 2)]
        [InlineData(8, 2, 6, 4)]
        public void CreateFromCardsPerPlayerCount_WithSupportedCount_ReturnsModeData(
            int cardsPerPlayerCount,
            int expectedFatalCatCount,
            int expectedSafeCardCount,
            int expectedSuccessfulChallengesToWin)
        {
            // Act
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal(cardsPerPlayerCount, modeRules.CardsPerPlayerCount);
            Assert.Equal(expectedFatalCatCount, modeRules.FatalCatCount);
            Assert.Equal(expectedSafeCardCount, modeRules.SafeCardCount);
            Assert.Equal(expectedSuccessfulChallengesToWin, modeRules.SuccessfulChallengesToWin);
        }

        /// <summary>Verifica CreateFromCardsPerPlayerCount_WithSupportedCount_AllowsExtraLifeOnlyInEightMode.</summary>
        [Theory]
        [InlineData(4, false)]
        [InlineData(6, false)]
        [InlineData(8, true)]
        public void CreateFromCardsPerPlayerCount_WithSupportedCount_AllowsExtraLifeOnlyInEightMode(
            int cardsPerPlayerCount,
            bool expectedAllowsExtraLife)
        {
            // Act
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal(expectedAllowsExtraLife, modeRules.AllowsExtraLife);
        }

        /// <summary>Verifica CreateFromCardsPerPlayerCount_WithUnsupportedCount_ThrowsArgumentOutOfRange.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(-4)]
        public void CreateFromCardsPerPlayerCount_WithUnsupportedCount_ThrowsArgumentOutOfRange(int cardsPerPlayerCount)
        {
            // Act
            Exception thrown = Record.Exception(
                () => MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
        }
    }
}
