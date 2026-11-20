using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of ChatValidator: only spaces are trimmed, the trimmed text must not be empty, must have at most 160
    /// characters and may use only the closed list of characters of ValidationRules.ChatAllowedCharacters. The limit
    /// is written here on purpose and not read from ValidationRules.
    /// </summary>
    public class ChatValidatorTests
    {
        private const int MaximumMessageLength = 160;
        private const string AllowedLetterText = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Each character of the closed list is accepted: letters, digits, accented vowels, the letter Ñ, space,
        /// comma and both question marks. Spaces around the text are removed, the ones inside are kept.
        /// </summary>
        /// <param name="text">Text as typed.</param>
        /// <param name="expectedNormalizedText">Text returned by the validator.</param>
        [Theory]
        [InlineData("a", "a")]
        [InlineData("Z", "Z")]
        [InlineData("0", "0")]
        [InlineData("Á", "Á")]
        [InlineData("É", "É")]
        [InlineData("Í", "Í")]
        [InlineData("Ó", "Ó")]
        [InlineData("Ú", "Ú")]
        [InlineData("á", "á")]
        [InlineData("é", "é")]
        [InlineData("í", "í")]
        [InlineData("ó", "ó")]
        [InlineData("ú", "ú")]
        [InlineData("Ñ", "Ñ")]
        [InlineData("ñ", "ñ")]
        [InlineData(",", ",")]
        [InlineData("?", "?")]
        [InlineData("¿", "¿")]
        [InlineData(AllowedLetterText, AllowedLetterText)]
        [InlineData("0123456789", "0123456789")]
        [InlineData("hola mundo", "hola mundo")]
        [InlineData("hola   mundo", "hola   mundo")]
        [InlineData("  hola  ", "hola")]
        [InlineData("¿Qué, tal?", "¿Qué, tal?")]
        public void ValidateText_WithAllowedText_ReturnsNullAndNormalizedText(
            string text,
            string expectedNormalizedText)
        {
            // Arrange

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Null(errorKey);
            Assert.Equal(expectedNormalizedText, normalizedText);
        }

        /// <summary>A missing text or one with only spaces is empty.</summary>
        /// <param name="text">Text as typed.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void ValidateText_WithMissingOrOnlySpaces_ReturnsChatEmptyError(string text)
        {
            // Arrange

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatEmpty, errorKey);
            Assert.Null(normalizedText);
        }

        /// <summary>
        /// Any character outside the closed list is rejected, wherever it is: other letters, symbols, control
        /// characters (only spaces are trimmed, so a tab at the edge counts), emoji and a combining mark.
        /// </summary>
        /// <param name="text">Text as typed.</param>
        [Theory]
        [InlineData("ü")]
        [InlineData("Ü")]
        [InlineData("ç")]
        [InlineData("!")]
        [InlineData("¡")]
        [InlineData(".")]
        [InlineData("-")]
        [InlineData("_")]
        [InlineData("@")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("😀")]
        [InlineData("á́")]
        [InlineData("\tabc")]
        [InlineData("abc\n")]
        [InlineData("hola\nmundo")]
        [InlineData("hola!")]
        [InlineData("¡hola")]
        public void ValidateText_WithCharacterOutsideTheList_ReturnsChatCharactersError(string text)
        {
            // Arrange

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatCharacters, errorKey);
            Assert.Null(normalizedText);
        }

        /// <summary>Texts of up to 160 characters are accepted unchanged.</summary>
        /// <param name="length">Length of the text.</param>
        [Theory]
        [InlineData(1)]
        [InlineData(MaximumMessageLength - 1)]
        [InlineData(MaximumMessageLength)]
        public void ValidateText_UpToMaximumLength_ReturnsNullAndSameText(int length)
        {
            // Arrange
            string text = new string('a', length);

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Null(errorKey);
            Assert.Equal(text, normalizedText);
        }

        /// <summary>Texts of more than 160 characters are rejected.</summary>
        /// <param name="length">Length of the text.</param>
        [Theory]
        [InlineData(MaximumMessageLength + 1)]
        [InlineData(MaximumMessageLength * 10)]
        public void ValidateText_AboveMaximumLength_ReturnsChatTooLongError(int length)
        {
            // Arrange
            string text = new string('a', length);

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatTooLong, errorKey);
            Assert.Null(normalizedText);
        }

        /// <summary>The length is measured after removing the spaces around the text.</summary>
        [Fact]
        public void ValidateText_WithMaximumLengthAndSurroundingSpaces_ReturnsNullAndTrimmedText()
        {
            // Arrange
            string trimmedText = new string('a', MaximumMessageLength);
            string text = "   " + trimmedText + "   ";

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Null(errorKey);
            Assert.Equal(trimmedText, normalizedText);
        }

        /// <summary>One character over the limit is rejected even when spaces surround the text.</summary>
        [Fact]
        public void ValidateText_AboveMaximumLengthWithSurroundingSpaces_ReturnsChatTooLongError()
        {
            // Arrange
            string text = "   " + new string('a', MaximumMessageLength + 1) + "   ";

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatTooLong, errorKey);
            Assert.Null(normalizedText);
        }

        /// <summary>A text that is too long and has a character outside the list reports the length.</summary>
        [Fact]
        public void ValidateText_TooLongWithCharacterOutsideTheList_ReturnsChatTooLongError()
        {
            // Arrange
            string text = new string('a', MaximumMessageLength) + "!";

            // Act
            string errorKey = ChatValidator.ValidateText(text, out string normalizedText);

            // Assert
            Assert.Equal(ErrorKeys.Lobby.ChatTooLong, errorKey);
            Assert.Null(normalizedText);
        }
    }
}
