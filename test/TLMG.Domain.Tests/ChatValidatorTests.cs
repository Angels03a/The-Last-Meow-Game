using System;

using Xunit;

using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de ChatValidator.</summary>
    public class ChatValidatorTests
    {

        /// <summary>Verifica ValidateText_WithAllowedText_ReturnsNormalizedText.</summary>
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
        [InlineData("hola mundo", "hola mundo")]
        [InlineData("  hola  ", "hola")]
        public void ValidateText_WithAllowedText_ReturnsNormalizedText(string text, string expected)
        {
            // Arrange
            string normalizedText;

            // Act
            string errorKey = ChatValidator.ValidateText(text, out normalizedText);

            // Assert
            Assert.Equal(expected, normalizedText);
        }

        /// <summary>Verifica ValidateText_WithInvalidText_ReturnsError.</summary>
        [Theory]
        [InlineData(null, "Lobby_ChatEmptyError")]
        [InlineData("", "Lobby_ChatEmptyError")]
        [InlineData("   ", "Lobby_ChatEmptyError")]
        [InlineData("ü", "Lobby_ChatCharactersError")]
        [InlineData("Ü", "Lobby_ChatCharactersError")]
        [InlineData("!", "Lobby_ChatCharactersError")]
        [InlineData("¡", "Lobby_ChatCharactersError")]
        [InlineData(".", "Lobby_ChatCharactersError")]
        [InlineData("\t", "Lobby_ChatCharactersError")]
        [InlineData("\n", "Lobby_ChatCharactersError")]
        [InlineData("\r", "Lobby_ChatCharactersError")]
        [InlineData("😀", "Lobby_ChatCharactersError")]
        [InlineData("á́", "Lobby_ChatCharactersError")]
        [InlineData("-", "Lobby_ChatCharactersError")]
        [InlineData("_", "Lobby_ChatCharactersError")]
        [InlineData("@", "Lobby_ChatCharactersError")]
        [InlineData("ç", "Lobby_ChatCharactersError")]
        public void ValidateText_WithInvalidText_ReturnsError(string text, string expected)
        {
            // Arrange
            string normalizedText;

            // Act
            string errorKey = ChatValidator.ValidateText(text, out normalizedText);

            // Assert
            Assert.Equal(expected, errorKey);
        }

        /// <summary>Verifica ValidateText_WithLengthBoundary_ReturnsExpectedError.</summary>
        [Theory]
        [InlineData(159, null)]
        [InlineData(160, null)]
        [InlineData(161, "Lobby_ChatTooLongError")]
        public void ValidateText_WithLengthBoundary_ReturnsExpectedError(int length, string expected)
        {
            // Arrange
            string text = new string('a', length);
            string normalizedText;

            // Act
            string errorKey = ChatValidator.ValidateText(text, out normalizedText);

            // Assert
            Assert.Equal(expected, errorKey);
        }

    }
}
