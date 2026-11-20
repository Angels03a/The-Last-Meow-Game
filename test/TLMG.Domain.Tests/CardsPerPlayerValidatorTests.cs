using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of CardsPerPlayerValidator. The supported modes (4, 6 and 8 cards per player) are written here on
    /// purpose and not read from ValidationRules, so that changing a mode in production breaks a test.
    /// </summary>
    public class CardsPerPlayerValidatorTests
    {
        /// <summary>The three game modes are accepted.</summary>
        /// <param name="cardsPerPlayerCount">Cards per player of a supported mode.</param>
        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        public void ValidateCardsPerPlayerCount_WithSupportedMode_ReturnsNull(int cardsPerPlayerCount)
        {
            // Arrange

            // Act
            string errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>
        /// Any other value is rejected: zero and negatives, the values next to each mode, the ones between modes and
        /// the extremes of the integer range.
        /// </summary>
        /// <param name="cardsPerPlayerCount">Cards per player that is not a supported mode.</param>
        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-4)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(48)]
        [InlineData(int.MaxValue)]
        public void ValidateCardsPerPlayerCount_WithUnsupportedValue_ReturnsCardsPerPlayerNotValidError(
            int cardsPerPlayerCount)
        {
            // Arrange

            // Act
            string errorKey = CardsPerPlayerValidator.ValidateCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.CardsPerPlayerNotValid, errorKey);
        }
    }
}
