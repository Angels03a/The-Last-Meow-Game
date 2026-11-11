using System;

using Xunit;

using TLMG.Services.Accounts;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de PasswordHasher.</summary>
    public class PasswordHasherTests
    {
        private const string SyntheticPassword = "Synthetic12!";

        /// <summary>Verifica HashPassword_WithMissingPassword_ReturnsNull.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void HashPassword_WithMissingPassword_ReturnsNull(string password)
        {
            // Arrange
            PasswordHasher hasher = new PasswordHasher();

            // Act
            string hash = hasher.HashPassword(password);

            // Assert
            Assert.Null(hash);
        }

        /// <summary>Verifica VerifyPassword_WithOwnHash_ReturnsTrue.</summary>
        [Theory]
        [InlineData(SyntheticPassword)]
        [InlineData("a")]
        [InlineData(" ")]
        [InlineData("ñ猫")]
        [InlineData("longer synthetic password 123!")]
        public void VerifyPassword_WithOwnHash_ReturnsTrue(string password)
        {
            // Arrange
            PasswordHasher hasher = new PasswordHasher();
            string hash = hasher.HashPassword(password);

            // Act
            bool verified = hasher.VerifyPassword(password, hash);

            // Assert
            Assert.True(verified);
        }

        /// <summary>Verifica VerifyPassword_WithMalformedHash_ReturnsFalse.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("v0$a$b")]
        [InlineData("v1$!$!")]
        [InlineData("v1$AA==$AA==")]
        [InlineData("v1$$")]
        [InlineData("v1$a")]
        [InlineData("v1$a$b$c")]
        public void VerifyPassword_WithMalformedHash_ReturnsFalse(string hash)
        {
            // Arrange
            PasswordHasher hasher = new PasswordHasher();

            // Act
            bool verified = hasher.VerifyPassword("synthetic", hash);

            // Assert
            Assert.False(verified);
        }

        /// <summary>Verifica VerifyPassword_WithWrongPassword_ReturnsFalse.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("wrong")]
        [InlineData("synthetic12!")]
        public void VerifyPassword_WithWrongPassword_ReturnsFalse(string password)
        {
            // Arrange
            PasswordHasher hasher = new PasswordHasher();
            string hash = hasher.HashPassword(SyntheticPassword);

            // Act
            bool verified = hasher.VerifyPassword(password, hash);

            // Assert
            Assert.False(verified);
        }

        /// <summary>Verifica HashPassword_WithSameInput_UsesDifferentSalts.</summary>
        [Fact]
        public void HashPassword_WithSameInput_UsesDifferentSalts()
        {
            // Arrange
            PasswordHasher hasher = new PasswordHasher();
            string firstHash = hasher.HashPassword(SyntheticPassword);

            // Act
            string secondHash = hasher.HashPassword(SyntheticPassword);

            // Assert
            Assert.NotEqual(firstHash, secondHash);
        }

    }
}
