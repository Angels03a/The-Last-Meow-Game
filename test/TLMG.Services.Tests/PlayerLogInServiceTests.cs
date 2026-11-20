using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of PlayerLogInService with the real password hasher and the real session registry, and a repository
    /// double that records its calls. A rejected log-in must not create a session, replace an existing one or
    /// update the last log-in time.
    /// </summary>
    public class PlayerLogInServiceTests
    {
        private const string SyntheticPassword = "Synthetic12!";
        private const string SyntheticEmail = "player@example.test";
        private const string PlayerNickname = "Player";
        private const string IncorrectPassword = "Wrong12!";
        private const string InactiveUserState = "USER_INACTIVE";
        private const int PlayerUserId = 7;

        private readonly PasswordHasher _hasher = new PasswordHasher();
        private readonly PlayerRepositoryStub _repository = new PlayerRepositoryStub();
        private readonly SessionRegistry _registry = new SessionRegistry();

        [Fact]
        public void LogIn_WithValidAccount_CreatesSessionForThatPlayerAndRecordsTheLogIn()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();

            // Act
            ResultWithNotification<OperationLogInResult> result = service.LogIn(CreateRequest(SyntheticPassword));

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Null(result.OperationResult.ErrorKey);
            Assert.Equal(PlayerUserId, result.OperationResult.UserId);
            Assert.Equal(PlayerNickname, result.OperationResult.Nickname);
            Assert.True(_registry.TryFindSessionByToken(
                result.OperationResult.SessionToken, out PlayerSession session));
            Assert.Equal(PlayerUserId, session.UserId);
            Assert.Equal(PlayerNickname, session.Nickname);
            Assert.Equal(new[] { PlayerUserId }, _repository.UpdatedLogInUserIds);
            Assert.Empty(result.Notifications);
        }

        [Fact]
        public void LogIn_WithEmailInOtherCaseAndSpaces_LooksUpTheNormalizedEmail()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = "  PLAYER@Example.TEST  ",
                Password = SyntheticPassword
            };

            // Act
            OperationLogInResult result = service.LogIn(request).OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { SyntheticEmail }, _repository.LookedUpEmails);
        }

        [Theory]
        [InlineData(null, "p", ErrorKeys.Login.IncompleteFields)]
        [InlineData("", "p", ErrorKeys.Login.IncompleteFields)]
        [InlineData("  ", "p", ErrorKeys.Login.IncompleteFields)]
        [InlineData(SyntheticEmail, null, ErrorKeys.Login.IncompleteFields)]
        [InlineData(SyntheticEmail, "", ErrorKeys.Login.IncompleteFields)]
        [InlineData(SyntheticEmail, " ", ErrorKeys.Login.IncompleteFields)]
        [InlineData("bad", "p", ErrorKeys.Login.EmailFormat)]
        [InlineData("abcde@example.test", "p", ErrorKeys.Login.EmailFormat)]
        public void LogIn_WithInvalidFields_RejectsBeforeAnyLookup(
            string email,
            string password,
            string expectedErrorKey)
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            OperationLogInRequest request = new OperationLogInRequest { Email = email, Password = password };

            // Act
            OperationLogInResult result = service.LogIn(request).OperationResult;

            // Assert
            AssertRejectedWithoutSideEffects(result, expectedErrorKey);
            Assert.Empty(_repository.LookedUpEmails);
        }

        [Fact]
        public void LogIn_WithNullRequest_RejectsWithIncompleteFields()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();

            // Act
            OperationLogInResult result = service.LogIn(null).OperationResult;

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Login.IncompleteFields);
            Assert.Empty(_repository.LookedUpEmails);
        }

        [Fact]
        public void LogIn_WithUnknownEmail_RejectsWithInvalidCredentials()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            _repository.Credentials.Status = CredentialsLookupStatus.NotFound;

            // Act
            OperationLogInResult result = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult;

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Login.InvalidCredentials);
        }

        [Fact]
        public void LogIn_WithWrongPassword_RejectsWithSameErrorAsUnknownEmail()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();

            // Act
            OperationLogInResult result = service.LogIn(CreateRequest(IncorrectPassword)).OperationResult;

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Login.InvalidCredentials);
        }

        [Theory]
        [InlineData(InactiveUserState)]
        [InlineData("")]
        [InlineData(null)]
        public void LogIn_WithCorrectPasswordButAccountNotActive_RejectsWithInvalidCredentials(string accountState)
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            _repository.Credentials.State = accountState;

            // Act
            OperationLogInResult result = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult;

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Login.InvalidCredentials);
        }

        [Fact]
        public void LogIn_WhenCredentialsLookupFails_RejectsWithDataModelError()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            _repository.Credentials.Status = CredentialsLookupStatus.DataFailure;

            // Act
            OperationLogInResult result = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult;

            // Assert
            AssertRejectedWithoutSideEffects(result, ErrorKeys.Login.DataModel);
        }

        [Fact]
        public void LogIn_WhenLastLogInUpdateFails_RejectsWithDataModelErrorAndNoSession()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            _repository.UpdateSucceeds = false;

            // Act
            OperationLogInResult result = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult;

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Login.DataModel, result.ErrorKey);
            Assert.Null(result.SessionToken);
            Assert.Null(result.Nickname);
        }

        [Fact]
        public void LogIn_WhenTheRepositoryThrows_PropagatesTheExceptionWithoutSession()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            _repository.ThrowOnLookup = true;

            // Act
            Action logIn = () => service.LogIn(CreateRequest(SyntheticPassword));

            // Assert
            Assert.Throws<InvalidOperationException>(logIn);
            Assert.Empty(_repository.UpdatedLogInUserIds);
        }

        [Fact]
        public void LogIn_ForPlayerWithOpenSession_InvalidatesTheOldTokenAndIssuesANewOne()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            string oldToken = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult.SessionToken;

            // Act
            OperationLogInResult second = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult;

            // Assert
            Assert.True(second.IsSuccess);
            Assert.NotEqual(oldToken, second.SessionToken);
            Assert.False(_registry.TryFindSessionByToken(oldToken, out _));
            Assert.True(_registry.TryFindSessionByToken(second.SessionToken, out _));
        }

        [Fact]
        public void LogIn_WithWrongPasswordForPlayerWithOpenSession_KeepsTheOpenSession()
        {
            // Arrange
            PlayerLogInService service = CreateServiceWithActiveAccount();
            string openToken = service.LogIn(CreateRequest(SyntheticPassword)).OperationResult.SessionToken;

            // Act
            OperationLogInResult rejected = service.LogIn(CreateRequest(IncorrectPassword)).OperationResult;

            // Assert
            Assert.False(rejected.IsSuccess);
            Assert.True(_registry.TryFindSessionByToken(openToken, out _));
            Assert.Equal(new[] { PlayerUserId }, _repository.UpdatedLogInUserIds);
        }

        private PlayerLogInService CreateServiceWithActiveAccount()
        {
            _repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = PlayerUserId,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = _hasher.HashPassword(SyntheticPassword)
            };

            return new PlayerLogInService(_repository, _hasher, _registry);
        }

        private static OperationLogInRequest CreateRequest(string password)
        {
            return new OperationLogInRequest { Email = SyntheticEmail, Password = password };
        }

        private void AssertRejectedWithoutSideEffects(OperationLogInResult result, string expectedErrorKey)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorKey, result.ErrorKey);
            Assert.Null(result.SessionToken);
            Assert.Null(result.Nickname);
            Assert.Empty(_repository.UpdatedLogInUserIds);
        }
    }
}
