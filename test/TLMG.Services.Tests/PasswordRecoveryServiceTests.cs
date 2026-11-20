using System.Collections.Generic;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Utilities;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of PasswordRecoveryService. The repositories are local doubles that record their calls. The dispatcher
    /// is the real class built over those doubles, with null instead of EmailUtility (see
    /// PasswordResetDispatcherTests), so only the paths that do not send an e-mail are covered. The hasher is the real
    /// one: hashing is pure logic. Whether a code is expired, exhausted or already used is decided by the database
    /// and is only mapped here from the status it returns.
    /// </summary>
    public class PasswordRecoveryServiceTests
    {
        private const string Email = "player@example.com";
        private const string CultureCode = "en-US";
        private const string PlainCode = "123456";
        private const string PlainCodeHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92";
        private const string NewPassword = "Brandnew12!x";
        private const int PlayerUserId = 7;

        private readonly PasswordHasher _hasher = new PasswordHasher();
        private readonly PlayerRepositoryStub _players = new PlayerRepositoryStub();
        private readonly VerificationCodeRepositoryStub _codes = new VerificationCodeRepositoryStub();
        private readonly PasswordRepositoryDouble _passwords = new PasswordRepositoryDouble();

        public PasswordRecoveryServiceTests()
        {
            _players.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = PlayerUserId
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("a@b")]
        public void RequestResetCode_WithInvalidEmail_RejectsWithEmailFormatWithoutLookingUpTheAccount(string email)
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.RequestResetCode(email, CultureCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Password.EmailFormat, result.ErrorKey);
            Assert.Empty(_players.LookedUpEmails);
            Assert.Empty(_codes.SavedCodes);
        }

        [Fact]
        public void RequestResetCode_WithUnknownEmail_ReturnsSuccessSoItDoesNotRevealTheAccount()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.NotFound };

            // Act
            OperationResult result = service.RequestResetCode(Email, CultureCode);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            Assert.Empty(_codes.SavedCodes);
        }

        [Fact]
        public void RequestResetCode_WithEmailInMixedCaseAndSpaces_LooksUpTheNormalizedEmail()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.NotFound };

            // Act
            service.RequestResetCode("  Player@Example.COM ", CultureCode);

            // Assert
            Assert.Equal(new[] { Email }, _players.LookedUpEmails);
        }

        [Fact]
        public void RequestResetCode_WhenTheAccountLookupFails_RejectsWithDataModelError()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };

            // Act
            OperationResult result = service.RequestResetCode(Email, CultureCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Password.DataModel, result.ErrorKey);
            Assert.Empty(_codes.SavedCodes);
        }

        [Fact]
        public void RequestResetCode_WhenTheCodeCannotBeSaved_RejectsWithDataModelError()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _codes.SaveCodeSucceeds = false;

            // Act
            OperationResult result = service.RequestResetCode(Email, CultureCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Password.DataModel, result.ErrorKey);
            Assert.Single(_codes.SavedCodes);
        }

        [Theory]
        [InlineData(null, PlainCode)]
        [InlineData("", PlainCode)]
        [InlineData("   ", PlainCode)]
        [InlineData(Email, null)]
        [InlineData(Email, "")]
        [InlineData(Email, "   ")]
        public void VerifyResetCode_WithMissingEmailOrCode_RejectsWithIncorrectCodeWithoutLookup(
            string email,
            string verificationCode)
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.VerifyResetCode(email, verificationCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Verification.IncorrectCode, result.ErrorKey);
            Assert.Empty(_players.LookedUpEmails);
            Assert.Empty(_codes.ValidatedCodes);
        }

        [Fact]
        public void VerifyResetCode_WithEmailAndCodeWithExtraSpaces_ValidatesTheNormalizedEmailAndTrimmedCode()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.VerifyResetCode("  Player@Example.COM ", " 123456 ");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            Assert.Equal(new[] { Email }, _players.LookedUpEmails);
            VerificationCodeRecord validated = Assert.Single(_codes.ValidatedCodes);
            Assert.Equal(PlayerUserId, validated.UserId);
            Assert.Equal(PlainCodeHash, validated.CodeHash);
        }

        [Theory]
        [InlineData(VerificationCodeStatus.Invalid, ErrorKeys.Verification.IncorrectCode)]
        [InlineData(VerificationCodeStatus.TooManyAttempts, ErrorKeys.Verification.AttemptsExhausted)]
        [InlineData(VerificationCodeStatus.Expired, ErrorKeys.Verification.ExpiredCode)]
        [InlineData(VerificationCodeStatus.NoActiveCode, ErrorKeys.Verification.ExpiredCode)]
        [InlineData(VerificationCodeStatus.UnexpectedError, ErrorKeys.Password.DataModel)]
        public void VerifyResetCode_WhenTheDatabaseRejectsTheCode_ReturnsTheMatchingError(
            VerificationCodeStatus status,
            string expectedErrorKey)
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _codes.ValidationResultToReturn = new VerificationCodeValidationResult { Status = status };

            // Act
            OperationResult result = service.VerifyResetCode(Email, PlainCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorKey, result.ErrorKey);
        }

        [Fact]
        public void VerifyResetCode_WithUnknownEmail_ReportsAnExpiredCodeWithoutValidating()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.NotFound };

            // Act
            OperationResult result = service.VerifyResetCode(Email, PlainCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Verification.ExpiredCode, result.ErrorKey);
            Assert.Empty(_codes.ValidatedCodes);
        }

        [Fact]
        public void VerifyResetCode_WhenTheAccountLookupFails_RejectsWithDataModelError()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _players.Credentials = new PlayerCredentials { Status = CredentialsLookupStatus.DataFailure };

            // Act
            OperationResult result = service.VerifyResetCode(Email, PlainCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Password.DataModel, result.ErrorKey);
            Assert.Empty(_codes.ValidatedCodes);
        }

        [Fact]
        public void ResetPassword_WithNullRequest_RejectsWithUnexpectedErrorWithoutWriting()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.ResetPassword(null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, result.ErrorKey);
            Assert.Empty(_passwords.Resets);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Short1!")]
        [InlineData("alllowercase12!")]
        [InlineData("NoSpecialChars123")]
        public void ResetPassword_WithPasswordThatBreaksThePolicy_RejectsWithPolicyErrorWithoutWriting(
            string newPassword)
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.ResetPassword(CreateRequest(Email, PlainCode, newPassword));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Password.PolicyNotMet, result.ErrorKey);
            Assert.Empty(_passwords.Resets);
        }

        [Fact]
        public void ResetPassword_WithWeakPasswordAndMissingCode_ReportsThePolicyErrorFirst()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.ResetPassword(CreateRequest(Email, null, "Short1!"));

            // Assert
            Assert.Equal(ErrorKeys.Password.PolicyNotMet, result.ErrorKey);
        }

        [Theory]
        [InlineData(null, PlainCode)]
        [InlineData("", PlainCode)]
        [InlineData("   ", PlainCode)]
        [InlineData(Email, null)]
        [InlineData(Email, "")]
        [InlineData(Email, "   ")]
        public void ResetPassword_WithMissingEmailOrCode_RejectsWithExpiredCodeWithoutWriting(
            string email,
            string verificationCode)
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.ResetPassword(CreateRequest(email, verificationCode, NewPassword));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Verification.ExpiredCode, result.ErrorKey);
            Assert.Empty(_passwords.Resets);
        }

        [Fact]
        public void ResetPassword_WithValidRequest_WritesTheHashOfTheNewPasswordForTheVerifiedCode()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            OperationResult result = service.ResetPassword(CreateRequest(Email, PlainCode, NewPassword));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            PasswordRepositoryDouble.ResetCall call = Assert.Single(_passwords.Resets);
            Assert.Equal(Email, call.Email);
            Assert.Equal(PlainCodeHash, call.CodeHash);
            Assert.NotEqual(NewPassword, call.NewPasswordHash);
            Assert.True(_hasher.VerifyPassword(NewPassword, call.NewPasswordHash));
        }

        [Fact]
        public void ResetPassword_WithEmailInMixedCaseAndCodeWithSpaces_WritesTheNormalizedEmailAndTrimmedCodeHash()
        {
            // Arrange
            PasswordRecoveryService service = CreateService();

            // Act
            service.ResetPassword(CreateRequest("  Player@Example.COM ", " 123456 ", NewPassword));

            // Assert
            PasswordRepositoryDouble.ResetCall call = Assert.Single(_passwords.Resets);
            Assert.Equal(Email, call.Email);
            Assert.Equal(PlainCodeHash, call.CodeHash);
        }

        [Theory]
        [InlineData(PasswordResetStatus.NoVerifiedCode, ErrorKeys.Verification.ExpiredCode)]
        [InlineData(PasswordResetStatus.UserNotFound, ErrorKeys.Verification.ExpiredCode)]
        [InlineData(PasswordResetStatus.DataFailure, ErrorKeys.Password.DataModel)]
        public void ResetPassword_WhenTheDatabaseRejectsTheReset_ReturnsTheMatchingError(
            PasswordResetStatus resetStatus,
            string expectedErrorKey)
        {
            // Arrange
            PasswordRecoveryService service = CreateService();
            _passwords.ResetStatus = resetStatus;

            // Act
            OperationResult result = service.ResetPassword(CreateRequest(Email, PlainCode, NewPassword));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorKey, result.ErrorKey);
            Assert.Single(_passwords.Resets);
        }

        private PasswordRecoveryService CreateService()
        {
            PasswordResetDispatcher dispatcher = new PasswordResetDispatcher(
                new VerificationCodeStore(_codes),
                null,
                _players);

            return new PasswordRecoveryService(_passwords, _hasher, dispatcher);
        }

        private static OperationResetPasswordRequest CreateRequest(
            string email,
            string verificationCode,
            string newPassword)
        {
            return new OperationResetPasswordRequest
            {
                Email = email,
                VerificationCode = verificationCode,
                NewPassword = newPassword
            };
        }

        private sealed class PasswordRepositoryDouble : IPasswordRepository
        {
            internal PasswordResetStatus ResetStatus { get; set; } = PasswordResetStatus.Reset;

            internal List<ResetCall> Resets { get; } = new List<ResetCall>();

            public PasswordHashLookup FindPasswordHashByUserId(int userId)
            {
                throw new System.NotSupportedException("Password changes are outside this class.");
            }

            public PasswordChangeStatus ChangePassword(int userId, string currentHash, string newHash)
            {
                throw new System.NotSupportedException("Password changes are outside this class.");
            }

            public PasswordResetStatus ResetPasswordWithVerifiedCode(
                string email,
                string codeHash,
                string newPasswordHash)
            {
                Resets.Add(new ResetCall(email, codeHash, newPasswordHash));

                return ResetStatus;
            }

            internal sealed class ResetCall
            {
                internal ResetCall(string email, string codeHash, string newPasswordHash)
                {
                    Email = email;
                    CodeHash = codeHash;
                    NewPasswordHash = newPasswordHash;
                }

                internal string Email { get; }

                internal string CodeHash { get; }

                internal string NewPasswordHash { get; }
            }
        }
    }
}
