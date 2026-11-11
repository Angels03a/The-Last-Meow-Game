using System;

using Xunit;

using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de CardsPerPlayerValidator.</summary>
    public class CardsPerPlayerValidatorTests
    {
        /// <summary>Verifica ValidateCardsPerPlayerCount_WithSupportedCount_ReturnsNoError.</summary>
        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        public void ValidateCardsPerPlayerCount_WithSupportedCount_ReturnsNoError(int cardsPerPlayerCount)
        {
            // Act
            string errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Verifica ValidateCardsPerPlayerCount_WithCountBelowRange_ReturnsError.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(-4)]
        public void ValidateCardsPerPlayerCount_WithCountBelowRange_ReturnsError(int cardsPerPlayerCount)
        {
            // Act
            string errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal("Lobby_CardsPerPlayerNotValidError", errorKey);
        }

        /// <summary>Verifica ValidateCardsPerPlayerCount_WithCountBetweenModes_ReturnsError.</summary>
        [Theory]
        [InlineData(5)]
        [InlineData(7)]
        public void ValidateCardsPerPlayerCount_WithCountBetweenModes_ReturnsError(int cardsPerPlayerCount)
        {
            // Act
            string errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal("Lobby_CardsPerPlayerNotValidError", errorKey);
        }

        /// <summary>Verifica ValidateCardsPerPlayerCount_WithCountAboveRange_ReturnsError.</summary>
        [Theory]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(48)]
        public void ValidateCardsPerPlayerCount_WithCountAboveRange_ReturnsError(int cardsPerPlayerCount)
        {
            // Act
            string errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal("Lobby_CardsPerPlayerNotValidError", errorKey);
        }

        /// <summary>Verifica ValidateCardsPerPlayerCount_WithExtremeValues_DoesNotThrowAndReturnsError.</summary>
        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void ValidateCardsPerPlayerCount_WithExtremeValues_DoesNotThrowAndReturnsError(
            int cardsPerPlayerCount)
        {
            // Arrange
            string errorKey = null;

            // Act
            Exception thrown = Record.Exception(
                () => errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount));

            // Assert
            Assert.Null(thrown);
            Assert.Equal("Lobby_CardsPerPlayerNotValidError", errorKey);
        }
    }
}
