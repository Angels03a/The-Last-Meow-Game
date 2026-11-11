using System;

using Xunit;

using TLMG.Domain.Entities;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de PlayerAccountRegistrationValidator.</summary>
    public class PlayerAccountRegistrationValidatorTests
    {
        private const string SyntheticPassword = "Synthetic12!";

        private const string SyntheticEmail = "player@example.test";

        private const string PlayerNickname = "Player";

        /// <summary>Verifica ValidateEmail_WithFormats_ReturnsExpectedError.</summary>
        [Theory]
        [InlineData(null, "CreateAccount_EmailFormatError")]
        [InlineData("", "CreateAccount_EmailFormatError")]
        [InlineData("abcde@example.test", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef@example.test", null)]
        [InlineData("ABCDEF@example.test", null)]
        [InlineData("abc.def@example.test", null)]
        [InlineData("abcdef", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef@", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef@@example.test", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef@example", "CreateAccount_EmailFormatError")]
        [InlineData("abc_def@example.test", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef @example.test", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef@example.test\n", "CreateAccount_EmailFormatError")]
        [InlineData("abcdef@sub.example.test", null)]
        public void ValidateEmail_WithFormats_ReturnsExpectedError(string email, string expected)
        {
            // Arrange
            // Los correos son datos sintéticos de prueba.

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateEmail(email);

            // Assert
            Assert.Equal(expected, errorKey);
        }

        /// <summary>Verifica ValidateAccountPlayerRegistration_WithNickname_ReturnsExpectedError.</summary>
        [Theory]
        [InlineData(null, "CreateAccount_NicknameFormatError")]
        [InlineData("", "CreateAccount_NicknameFormatError")]
        [InlineData("ab", "CreateAccount_NicknameFormatError")]
        [InlineData("abc", null)]
        [InlineData("abcdefghijkl", null)]
        [InlineData("abcdefghijklm", "CreateAccount_NicknameFormatError")]
        [InlineData("Cat_1", null)]
        [InlineData("Cat One", "CreateAccount_NicknameFormatError")]
        [InlineData("Gatñ", "CreateAccount_NicknameFormatError")]
        [InlineData("Cat!", "CreateAccount_NicknameFormatError")]
        public void ValidateAccountPlayerRegistration_WithNickname_ReturnsExpectedError(
            string nickname,
            string expected)
        {
            // Arrange
            PlayerRegistration registration = new PlayerRegistration
            {
                Nickname = nickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Equal(expected, errorKey);
        }

        /// <summary>Verifica ValidateAccountPlayerRegistration_WithPassword_ReturnsExpectedError.</summary>
        [Theory]
        [InlineData(null, "CreateAccount_PasswordPolicyError")]
        [InlineData("", "CreateAccount_PasswordPolicyError")]
        [InlineData("Short1!", "CreateAccount_PasswordPolicyError")]
        [InlineData(SyntheticPassword, null)]
        [InlineData("synthetic12!", "CreateAccount_PasswordPolicyError")]
        [InlineData("SYNTHETIC12!", "CreateAccount_PasswordPolicyError")]
        [InlineData("Syntheticxx!", "CreateAccount_PasswordPolicyError")]
        [InlineData("Synthetic123", "CreateAccount_PasswordPolicyError")]
        public void ValidateAccountPlayerRegistration_WithPassword_ReturnsExpectedError(
            string password,
            string expected)
        {
            // Arrange
            PlayerRegistration registration = new PlayerRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = password,
                HasAcceptedTerms = true
            };

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Equal(expected, errorKey);
        }

        /// <summary>Verifica ValidateAccountPlayerRegistration_WithNullObject_ThrowsNullReference.</summary>
        [Fact]
        public void ValidateAccountPlayerRegistration_WithNullObject_ThrowsNullReference()
        {
            // Arrange
            PlayerRegistration registration = null;

            // Act
            Action validate = () => PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Throws<NullReferenceException>(validate);
        }

    }
}
