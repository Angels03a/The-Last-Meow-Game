using System.Collections.Generic;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of PasswordChangeService. The repository and the session identity are local doubles that record their
    /// calls; the password hasher is the real one, because hashing is pure logic and a fake would only repeat what
    /// the test expects. The format of the new password is tested in TLMG.Domain.Tests.
    /// </summary>
    public class PasswordChangeServiceTests
    {
        private const string ValidToken = "valid-token";
        private const int PlayerUserId = 7;
        private const string CurrentPassword = "Current123!x";
        private const string NewPassword = "Brandnew12!x";
        private const string WrongPassword = "Wrongone12!x";

        private readonly PasswordHasher _hasher = new PasswordHasher();
        private readonly PasswordRepositoryDouble _repository = new PasswordRepositoryDouble();
        private readonly SessionIdentityDouble _identity = new SessionIdentityDouble();
        private readonly string _storedHash;

        public PasswordChangeServiceTests()
        {
            _storedHash = _hasher.HashPassword(CurrentPassword);
            _repository.Lookup = new PasswordHashLookup
            {
                Status = CredentialsLookupStatus.Found,
                PasswordHash = _storedHash
            };
            _identity.TokenToUserId[ValidToken] = PlayerUserId;
        }

        [Fact]
        public void ChangePassword_WithValidRequest_WritesTheNewHashAgainstTheStoredOne()
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, NewPassword));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.Equal(new[] { PlayerUserId }, _repository.LookedUpUserIds);
            PasswordRepositoryDouble.ChangeCall call = Assert.Single(_repository.Changes);
            Assert.Equal(PlayerUserId, call.UserId);
            Assert.Equal(_storedHash, call.CurrentHash);
            Assert.NotEqual(NewPassword, call.NewHash);
            Assert.True(_hasher.VerifyPassword(NewPassword, call.NewHash));
            Assert.False(_hasher.VerifyPassword(CurrentPassword, call.NewHash));
        }

        [Fact]
        public void ChangePassword_WithNullRequest_RejectsWithoutAnyLookupOrWrite()
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(null);

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.NotNull(result.ErrorKey);
            Assert.Empty(_repository.LookedUpUserIds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Short1!")]
        [InlineData("alllowercase12!")]
        public void ChangePassword_WithInvalidNewPassword_RejectsWithPolicyErrorWithoutLookup(string newPassword)
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, newPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.PolicyNotMet, result.ErrorKey);
            Assert.Empty(_repository.LookedUpUserIds);
        }

        [Fact]
        public void ChangePassword_WithInvalidNewPasswordAndInvalidToken_ReportsThePolicyErrorFirst()
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(CreateRequest("unknown", CurrentPassword, "Short1!"));

            // Assert
            Assert.Equal(ErrorKeys.Password.PolicyNotMet, result.ErrorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown-token")]
        public void ChangePassword_WithInvalidToken_RejectsWithInvalidSessionWithoutLookingUpTheHash(string token)
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(token, CurrentPassword, NewPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
            Assert.Empty(_repository.LookedUpUserIds);
        }

        [Fact]
        public void ChangePassword_WhenTheUserIsNotFound_RejectsWithInvalidSession()
        {
            // Arrange
            PasswordChangeService service = CreateService();
            _repository.Lookup = new PasswordHashLookup { Status = CredentialsLookupStatus.NotFound };

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, NewPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
        }

        [Fact]
        public void ChangePassword_WhenReadingTheHashFails_RejectsWithDataModelError()
        {
            // Arrange
            PasswordChangeService service = CreateService();
            _repository.Lookup = new PasswordHashLookup { Status = CredentialsLookupStatus.DataFailure };

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, NewPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.DataModel, result.ErrorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangePassword_WhenFoundHashIsMissing_RejectsWithoutVerifyingOrWriting(string hash)
        {
            // Arrange
            _repository.Lookup = new PasswordHashLookup { Status = CredentialsLookupStatus.Found, PasswordHash = hash };
            PasswordChangeService service = new PasswordChangeService(_repository, new RejectingHasher(), _identity);

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, NewPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.DataModel, result.ErrorKey);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ChangePassword_WhenLookupIsNullOrUnknown_RejectsWithoutVerifyingOrWriting(bool isNull)
        {
            // Arrange
            _repository.Lookup = isNull ? null : new PasswordHashLookup
            {
                Status = (CredentialsLookupStatus)999,
                PasswordHash = _storedHash
            };
            PasswordChangeService service = new PasswordChangeService(_repository, new RejectingHasher(), _identity);

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, NewPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.DataModel, result.ErrorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(WrongPassword)]
        public void ChangePassword_WithWrongCurrentPassword_RejectsWithoutUpdatingTheHash(string currentPassword)
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, currentPassword, NewPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.CurrentPasswordIncorrect, result.ErrorKey);
        }

        [Fact]
        public void ChangePassword_WithNewPasswordEqualToTheCurrentOne_RejectsWithSamePasswordError()
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(
                CreateRequest(ValidToken, CurrentPassword, CurrentPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.SamePassword, result.ErrorKey);
        }

        [Fact]
        public void ChangePassword_WithWrongCurrentPasswordAndNewEqualToStored_ReportsTheWrongCurrentPassword()
        {
            // Arrange
            PasswordChangeService service = CreateService();

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, WrongPassword, CurrentPassword));

            // Assert
            AssertRejectedWithoutWrite(result);
            Assert.Equal(ErrorKeys.Password.CurrentPasswordIncorrect, result.ErrorKey);
        }

        [Theory]
        [InlineData(PasswordChangeStatus.StaleCredentials, ErrorKeys.Password.CurrentPasswordIncorrect)]
        [InlineData(PasswordChangeStatus.UserNotFound, ErrorKeys.Common.InvalidSession)]
        [InlineData(PasswordChangeStatus.DataFailure, ErrorKeys.Password.DataModel)]
        [InlineData((PasswordChangeStatus)999, ErrorKeys.Password.DataModel)]
        public void ChangePassword_WhenTheWriteIsRejected_ReturnsTheMatchingError(
            PasswordChangeStatus writeStatus,
            string expectedErrorKey)
        {
            // Arrange
            PasswordChangeService service = CreateService();
            _repository.ChangeStatus = writeStatus;

            // Act
            OperationResult result = service.ChangePassword(CreateRequest(ValidToken, CurrentPassword, NewPassword));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorKey, result.ErrorKey);
            Assert.Single(_repository.Changes);
        }

        private PasswordChangeService CreateService()
        {
            return new PasswordChangeService(_repository, _hasher, _identity);
        }

        private static OperationChangePasswordRequest CreateRequest(
            string token,
            string currentPassword,
            string newPassword)
        {
            return new OperationChangePasswordRequest
            {
                SessionToken = token,
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };
        }

        private void AssertRejectedWithoutWrite(OperationResult result)
        {
            Assert.False(result.IsSuccess);
            Assert.Empty(_repository.Changes);
        }

        private sealed class RejectingHasher : IPasswordHasher
        {
            public string HashPassword(string password)
            {
                throw new System.InvalidOperationException("Hashing must not occur after a rejected lookup.");
            }

            public bool VerifyPassword(string password, string storedHash)
            {
                throw new System.InvalidOperationException("Verification must not occur after a rejected lookup.");
            }
        }

        private sealed class PasswordRepositoryDouble : IPasswordRepository
        {
            internal PasswordHashLookup Lookup { get; set; }

            internal PasswordChangeStatus ChangeStatus { get; set; } = PasswordChangeStatus.Changed;

            internal List<int> LookedUpUserIds { get; } = new List<int>();

            internal List<ChangeCall> Changes { get; } = new List<ChangeCall>();

            public PasswordHashLookup FindPasswordHashByUserId(int userId)
            {
                LookedUpUserIds.Add(userId);

                return Lookup;
            }

            public PasswordChangeStatus ChangePassword(int userId, string currentHash, string newHash)
            {
                Changes.Add(new ChangeCall(userId, currentHash, newHash));

                return ChangeStatus;
            }

            public PasswordResetStatus ResetPasswordWithVerifiedCode(
                string email,
                string codeHash,
                string newPasswordHash)
            {
                throw new System.NotSupportedException("The password reset flow is outside this class.");
            }

            internal sealed class ChangeCall
            {
                internal ChangeCall(int userId, string currentHash, string newHash)
                {
                    UserId = userId;
                    CurrentHash = currentHash;
                    NewHash = newHash;
                }

                internal int UserId { get; }

                internal string CurrentHash { get; }

                internal string NewHash { get; }
            }
        }

        private sealed class SessionIdentityDouble : ISessionIdentityProvider
        {
            internal Dictionary<string, int> TokenToUserId { get; } = new Dictionary<string, int>();

            public bool TryGetUserId(string sessionToken, out int userId)
            {
                userId = 0;

                return sessionToken != null && TokenToUserId.TryGetValue(sessionToken, out userId);
            }

            public bool TryUpdateNickname(string sessionToken, string nickname)
            {
                throw new System.NotSupportedException("Nickname changes are outside this class.");
            }
        }
    }
}
