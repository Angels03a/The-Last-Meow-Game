using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of PlayerLogInValidator: the login only checks that both fields are present and that the email has a
    /// valid format. It does not apply the password policy, so accounts with older passwords can still sign in.
    /// </summary>
    public class PlayerLogInValidatorTests
    {
        private const string ValidEmail = "player@example.test";
        private const string AnyPassword = "x";

        [Fact]
        public void TryValidateLogIn_WithValidFields_ReturnsEmpty()
        {
            // Arrange
            PlayerLogin playerLogIn = new PlayerLogin { Email = ValidEmail, Password = AnyPassword };

            // Act
            bool isValid = PlayerLogInValidator.TryValidateLogIn(playerLogIn, out string errorKey);

            // Assert
            Assert.True(isValid);
            Assert.Equal(string.Empty, errorKey);
        }

        [Fact]
        public void TryValidateLogIn_WithNullRequest_ReturnsIncompleteFieldsError()
        {
            // Arrange
            PlayerLogin playerLogIn = null;

            // Act
            bool isValid = PlayerLogInValidator.TryValidateLogIn(playerLogIn, out string errorKey);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ErrorKeys.Login.IncompleteFields, errorKey);
        }

        [Theory]
        [InlineData(null, AnyPassword)]
        [InlineData("", AnyPassword)]
        [InlineData("   ", AnyPassword)]
        [InlineData(ValidEmail, null)]
        [InlineData(ValidEmail, "")]
        [InlineData(ValidEmail, "   ")]
        [InlineData(null, null)]
        public void TryValidateLogIn_WithMissingOrBlankField_ReturnsIncompleteFieldsError(string email, string password)
        {
            // Arrange
            PlayerLogin playerLogIn = new PlayerLogin { Email = email, Password = password };

            // Act
            bool isValid = PlayerLogInValidator.TryValidateLogIn(playerLogIn, out string errorKey);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ErrorKeys.Login.IncompleteFields, errorKey);
        }

        [Theory]
        [InlineData("abcdef")]
        [InlineData("abcde@example.test")]
        [InlineData("abc_def@example.test")]
        [InlineData("abcdef@example")]
        public void TryValidateLogIn_WithInvalidEmailFormat_ReturnsEmailFormatError(string email)
        {
            // Arrange
            PlayerLogin playerLogIn = new PlayerLogin { Email = email, Password = AnyPassword };

            // Act
            bool isValid = PlayerLogInValidator.TryValidateLogIn(playerLogIn, out string errorKey);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ErrorKeys.Login.EmailFormat, errorKey);
        }

        [Fact]
        public void TryValidateLogIn_WithInvalidEmailAndMissingPassword_ReportsIncompleteFieldsFirst()
        {
            // Arrange
            PlayerLogin playerLogIn = new PlayerLogin { Email = "abcdef", Password = string.Empty };

            // Act
            bool isValid = PlayerLogInValidator.TryValidateLogIn(playerLogIn, out string errorKey);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ErrorKeys.Login.IncompleteFields, errorKey);
        }

        [Fact]
        public void TryValidateLogIn_WithPasswordThatBreaksThePolicy_DoesNotRejectIt()
        {
            // Arrange
            PlayerLogin playerLogIn = new PlayerLogin { Email = ValidEmail, Password = "short" };

            // Act
            bool isValid = PlayerLogInValidator.TryValidateLogIn(playerLogIn, out string errorKey);

            // Assert
            Assert.True(isValid);
            Assert.Equal(string.Empty, errorKey);
        }
    }
}
