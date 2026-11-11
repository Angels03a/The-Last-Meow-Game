using System;

using Xunit;

using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de FriendNicknameValidator.</summary>
    public class FriendNicknameValidatorTests
    {
        /// <summary>Verifica ValidateNickname_WithValidNickname_ReturnsNoErrorAndNormalizedNickname.</summary>
        [Theory]
        [InlineData("abc", "abc")]
        [InlineData("Gato_01", "Gato_01")]
        [InlineData("abcdefghijkl", "abcdefghijkl")]
        [InlineData("  abc  ", "abc")]
        public void ValidateNickname_WithValidNickname_ReturnsNoErrorAndNormalizedNickname(string nickname,
            string expected)
        {
            // Arrange
            string normalizedNickname;

            // Act
            string errorKey = FriendNicknameValidator.ValidateNickname(nickname, out normalizedNickname);

            // Assert
            Assert.Null(errorKey);
            Assert.Equal(expected, normalizedNickname);
        }

        /// <summary>Verifica ValidateNickname_WithLengthBoundary_ReturnsExpectedError.</summary>
        [Theory]
        [InlineData(2, "Friends_NicknameFormatError")]
        [InlineData(3, null)]
        [InlineData(12, null)]
        [InlineData(13, "Friends_NicknameFormatError")]
        public void ValidateNickname_WithLengthBoundary_ReturnsExpectedError(int length, string expected)
        {
            // Arrange
            string nickname = new string('a', length);
            string normalizedNickname;

            // Act
            string errorKey = FriendNicknameValidator.ValidateNickname(nickname, out normalizedNickname);

            // Assert
            Assert.Equal(expected, errorKey);
        }

        /// <summary>Verifica ValidateNickname_WithEmail_ReturnsError.</summary>
        [Theory]
        [InlineData("a@b.com")]
        [InlineData("@abc")]
        [InlineData("abc@")]
        [InlineData("jugador@uv.mx")]
        public void ValidateNickname_WithEmail_ReturnsError(string nickname)
        {
            // Arrange
            string normalizedNickname;

            // Act
            string errorKey = FriendNicknameValidator.ValidateNickname(nickname, out normalizedNickname);

            // Assert
            Assert.Equal("Friends_NicknameFormatError", errorKey);
            Assert.Null(normalizedNickname);
        }

        /// <summary>Verifica ValidateNickname_WithEmptyOrBlankNickname_ReturnsError.</summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        [InlineData("\t")]
        public void ValidateNickname_WithEmptyOrBlankNickname_ReturnsError(string nickname)
        {
            // Arrange
            string normalizedNickname;

            // Act
            string errorKey = FriendNicknameValidator.ValidateNickname(nickname, out normalizedNickname);

            // Assert
            Assert.Equal("Friends_NicknameFormatError", errorKey);
            Assert.Null(normalizedNickname);
        }

        /// <summary>Verifica ValidateNickname_WithNullNickname_DoesNotThrowAndReturnsError.</summary>
        [Fact]
        public void ValidateNickname_WithNullNickname_DoesNotThrowAndReturnsError()
        {
            // Arrange
            string normalizedNickname = null;
            string errorKey = null;

            // Act
            Exception thrown = Record.Exception(
                () => errorKey = FriendNicknameValidator.ValidateNickname(null, out normalizedNickname));

            // Assert
            Assert.Null(thrown);
            Assert.Equal("Friends_NicknameFormatError", errorKey);
        }
    }
}
