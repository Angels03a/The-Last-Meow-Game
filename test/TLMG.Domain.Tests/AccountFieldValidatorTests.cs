using Xunit;

using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of the field rules shared by the account flows. Email and nickname format are exercised through
    /// PlayerAccountRegistrationValidatorTests, which call these rules; here only the helpers with their own contract
    /// are checked.
    /// </summary>
    public class AccountFieldValidatorTests
    {
        private const int MinimumLength = 3;
        private const int MaximumLength = 5;

        /// <summary>The length range is inclusive at both ends.</summary>
        /// <param name="length">Length of the text.</param>
        /// <param name="expectedIsInRange">Whether that length is inside the range.</param>
        [Theory]
        [InlineData(0, false)]
        [InlineData(MinimumLength - 1, false)]
        [InlineData(MinimumLength, true)]
        [InlineData(MaximumLength, true)]
        [InlineData(MaximumLength + 1, false)]
        public void IsLengthInRange_AtBoundaries_IsInclusive(int length, bool expectedIsInRange)
        {
            // Arrange
            string text = new string('a', length);

            // Act
            bool isInRange = AccountFieldValidator.IsLengthInRange(text, MinimumLength, MaximumLength);

            // Assert
            Assert.Equal(expectedIsInRange, isInRange);
        }

        /// <summary>A missing text is never inside the range.</summary>
        [Fact]
        public void IsLengthInRange_WithNullText_ReturnsFalse()
        {
            // Arrange
            string text = null;

            // Act
            bool isInRange = AccountFieldValidator.IsLengthInRange(text, MinimumLength, MaximumLength);

            // Assert
            Assert.False(isInRange);
        }

        /// <summary>The email is trimmed and lowercased with the invariant culture.</summary>
        /// <param name="email">Email as typed.</param>
        /// <param name="expectedEmail">Email after normalization.</param>
        [Theory]
        [InlineData("  Player@Example.TEST  ", "player@example.test")]
        [InlineData("player@example.test", "player@example.test")]
        [InlineData("\tPLAYER@EXAMPLE.TEST\n", "player@example.test")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void NormalizeEmail_WithText_ReturnsTrimmedLowercaseEmail(string email, string expectedEmail)
        {
            // Arrange

            // Act
            string normalizedEmail = AccountFieldValidator.NormalizeEmail(email);

            // Assert
            Assert.Equal(expectedEmail, normalizedEmail);
        }

        /// <summary>A missing email stays missing so that the validator can reject it.</summary>
        [Fact]
        public void NormalizeEmail_WithNull_ReturnsNull()
        {
            // Arrange
            string email = null;

            // Act
            string normalizedEmail = AccountFieldValidator.NormalizeEmail(email);

            // Assert
            Assert.Null(normalizedEmail);
        }
    }
}
