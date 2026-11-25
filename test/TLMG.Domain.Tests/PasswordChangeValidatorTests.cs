using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of PasswordChangeValidator. The password rules themselves (12 to 128 characters with upper case,
    /// lower case, digit and special character) belong to PlayerAccountRegistrationValidator and are tested there;
    /// here only the error key of this flow and the boundaries it must keep are checked.
    /// </summary>
    public class PasswordChangeValidatorTests
    {
        private const string ValidPassword = "Synthetic12!";
        private const string PasswordPrefix = "Aa1!";
        private const int MinimumPasswordLength = 12;
        private const int MaximumPasswordLength = 128;

        [Theory]
        [InlineData(ValidPassword)]
        [InlineData("Aa1_aaaaaaaa")]
        public void ValidateNewPassword_WithValidPassword_ReturnsEmpty(string newPassword)
        {
            // Arrange

            // Act
            string errorKey = PasswordChangeValidator.ValidateNewPassword(newPassword);

            // Assert
            Assert.Equal(string.Empty, errorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("            ")]
        [InlineData("synthetic12!")]
        [InlineData("SYNTHETIC12!")]
        [InlineData("Syntheticxx!")]
        [InlineData("Synthetic123")]
        public void ValidateNewPassword_WithMissingOrIncompletePassword_ReturnsPolicyError(string newPassword)
        {
            // Arrange

            // Act
            string errorKey = PasswordChangeValidator.ValidateNewPassword(newPassword);

            // Assert
            Assert.Equal(ErrorKeys.Password.PolicyNotMet, errorKey);
        }

        [Theory]
        [InlineData(MinimumPasswordLength - 1, false)]
        [InlineData(MinimumPasswordLength, true)]
        [InlineData(MaximumPasswordLength, true)]
        [InlineData(MaximumPasswordLength + 1, false)]
        public void ValidateNewPassword_AtLengthBoundary_AcceptsOnlyTheRange(int length, bool isValid)
        {
            // Arrange
            string newPassword = PasswordPrefix + new string('x', length - PasswordPrefix.Length);

            // Act
            string errorKey = PasswordChangeValidator.ValidateNewPassword(newPassword);

            // Assert
            Assert.Equal(isValid, errorKey == string.Empty);
        }

        [Fact]
        public void ValidateNewPassword_WithRejectedPassword_UsesTheChangePasswordKeyNotTheRegistrationKey()
        {
            // Arrange
            const string shortPassword = "Short1!";

            // Act
            string errorKey = PasswordChangeValidator.ValidateNewPassword(shortPassword);

            // Assert
            Assert.NotEqual(ErrorKeys.CreateAccount.PasswordPolicy, errorKey);
            Assert.Equal(ErrorKeys.Password.PolicyNotMet, errorKey);
        }
    }
}
