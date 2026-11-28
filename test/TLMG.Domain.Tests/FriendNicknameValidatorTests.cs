using Xunit;

using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of FriendNicknameValidator: the text is trimmed, must have 3 to 12 characters once trimmed and must not
    /// look like an email. The limits are written here on purpose and not read from ValidationRules.
    /// </summary>
    public class FriendNicknameValidatorTests
    {
        private const int MinimumNicknameLength = 3;
        private const int MaximumNicknameLength = 12;

        /// <summary>A valid nickname is accepted and returned without the surrounding blanks.</summary>
        /// <param name="nickname">Nickname as typed.</param>
        /// <param name="expectedNormalizedNickname">Nickname once trimmed.</param>
        [Theory]
        [InlineData("abc", "abc")]
        [InlineData("Gato_01", "Gato_01")]
        [InlineData("abcdefghijkl", "abcdefghijkl")]
        [InlineData("  abc  ", "abc")]
        [InlineData("\tabc\n", "abc")]
        [InlineData("  abcdefghijkl  ", "abcdefghijkl")]
        public void TryNormalizeNickname_WithValidNickname_ReturnsTrueAndTrimmedNickname(
            string nickname,
            string expectedNormalizedNickname)
        {
            // Arrange

            // Act
            bool isValid = FriendNicknameValidator.TryNormalizeNickname(nickname, out string normalizedNickname);

            // Assert
            Assert.True(isValid);
            Assert.Equal(expectedNormalizedNickname, normalizedNickname);
        }

        /// <summary>The length limits apply to the trimmed text and are inclusive.</summary>
        /// <param name="length">Length of the nickname without blanks.</param>
        [Theory]
        [InlineData(MinimumNicknameLength)]
        [InlineData(MaximumNicknameLength)]
        public void TryNormalizeNickname_AtLengthLimits_ReturnsTrueAndSameNickname(int length)
        {
            // Arrange
            string nickname = new string('a', length);

            // Act
            bool isValid = FriendNicknameValidator.TryNormalizeNickname(nickname, out string normalizedNickname);

            // Assert
            Assert.True(isValid);
            Assert.Equal(nickname, normalizedNickname);
        }

        /// <summary>Missing, blank, too short, too long and email-like nicknames are rejected.</summary>
        /// <param name="nickname">Nickname as typed.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        [InlineData("\t")]
        [InlineData("ab")]
        [InlineData("  ab  ")]
        [InlineData("abcdefghijklm")]
        [InlineData("  abcdefghijklm  ")]
        [InlineData("a@b.com")]
        [InlineData("@abc")]
        [InlineData("abc@")]
        [InlineData("jugador@uv.mx")]
        [InlineData("  a@b  ")]
        public void TryNormalizeNickname_WithInvalidNickname_ReturnsFalseAndEmptyNickname(string nickname)
        {
            // Arrange

            // Act
            bool isValid = FriendNicknameValidator.TryNormalizeNickname(nickname, out string normalizedNickname);

            // Assert
            Assert.False(isValid);
            Assert.Equal(string.Empty, normalizedNickname);
        }
    }
}
