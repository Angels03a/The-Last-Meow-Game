using Xunit;

using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Utilities;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of PasswordResetDispatcher for the paths that never send an e-mail. EmailUtility is a concrete class that
    /// talks to SMTP and cannot be replaced, so these tests pass null for it: if a path tried to send a message it
    /// would fail with a NullReferenceException instead of contacting a server. The path that sends the code (account
    /// found and code saved) cannot be tested without a real SMTP server and is not covered here.
    /// </summary>
    public class PasswordResetDispatcherTests
    {
        private const string Email = "player@example.com";
        private const string CultureCode = "en-US";
        private const string PlainCode = "123456";
        private const int PlayerUserId = 7;

        private readonly PlayerRepositoryStub _players = new PlayerRepositoryStub();
        private readonly VerificationCodeRepositoryStub _codes = new VerificationCodeRepositoryStub();

        [Fact]
        public void SendResetCode_WhenTheAccountDoesNotExist_ReturnsTrueAndStoresNoCode()
        {
            // Arrange
            PasswordResetDispatcher dispatcher = CreateDispatcher();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.NotFound };

            // Act
            bool wasHandled = dispatcher.SendResetCode(Email, CultureCode);

            // Assert
            Assert.True(wasHandled);
            Assert.Equal(new[] { Email }, _players.LookedUpEmails);
            Assert.Empty(_codes.SavedCodes);
        }

        [Fact]
        public void SendResetCode_WhenTheLookupFails_ReturnsFalseAndStoresNoCode()
        {
            // Arrange
            PasswordResetDispatcher dispatcher = CreateDispatcher();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };

            // Act
            bool wasHandled = dispatcher.SendResetCode(Email, CultureCode);

            // Assert
            Assert.False(wasHandled);
            Assert.Empty(_codes.SavedCodes);
        }

        [Fact]
        public void SendResetCode_WhenTheCodeCannotBeSaved_ReturnsFalseWithoutSendingAnEmail()
        {
            // Arrange
            PasswordResetDispatcher dispatcher = CreateDispatcher();
            _players.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = PlayerUserId
            };
            _codes.SaveCodeSucceeds = false;

            // Act
            bool wasHandled = dispatcher.SendResetCode(Email, CultureCode);

            // Assert
            Assert.False(wasHandled);
            VerificationCodeRecord savedCode = Assert.Single(_codes.SavedCodes);
            Assert.Equal(PlayerUserId, savedCode.UserId);
            Assert.Equal(VerificationCodeType.PasswordReset, savedCode.Type);
        }

        [Fact]
        public void ValidateResetCode_WhenTheAccountDoesNotExist_ReportsNoActiveCodeWithoutValidating()
        {
            // Arrange
            PasswordResetDispatcher dispatcher = CreateDispatcher();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.NotFound };

            // Act
            VerificationCodeValidationResult result = dispatcher.ValidateResetCode(Email, PlainCode);

            // Assert
            Assert.Equal(VerificationCodeStatus.NoActiveCode, result.Status);
            Assert.Empty(_codes.ValidatedCodes);
        }

        [Fact]
        public void ValidateResetCode_WhenTheLookupFails_ReportsUnexpectedErrorWithoutValidating()
        {
            // Arrange
            PasswordResetDispatcher dispatcher = CreateDispatcher();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };

            // Act
            VerificationCodeValidationResult result = dispatcher.ValidateResetCode(Email, PlainCode);

            // Assert
            Assert.Equal(VerificationCodeStatus.UnexpectedError, result.Status);
            Assert.Empty(_codes.ValidatedCodes);
        }

        [Fact]
        public void ValidateResetCode_WhenTheAccountExists_ReturnsTheDatabaseResultForThatUser()
        {
            // Arrange
            PasswordResetDispatcher dispatcher = CreateDispatcher();
            _players.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = PlayerUserId
            };
            _codes.ValidationResultToReturn = new VerificationCodeValidationResult
            {
                Status = VerificationCodeStatus.Invalid,
                AttemptsFailed = 2
            };

            // Act
            VerificationCodeValidationResult result = dispatcher.ValidateResetCode(Email, PlainCode);

            // Assert
            Assert.Same(_codes.ValidationResultToReturn, result);
            VerificationCodeRecord validated = Assert.Single(_codes.ValidatedCodes);
            Assert.Equal(PlayerUserId, validated.UserId);
            Assert.Equal(VerificationCodeType.PasswordReset, validated.Type);
        }

        private PasswordResetDispatcher CreateDispatcher()
        {
            return new PasswordResetDispatcher(new VerificationCodeStore(_codes), null, _players);
        }
    }
}
