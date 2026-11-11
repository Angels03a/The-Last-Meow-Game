using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de PlayerLogInService.</summary>
    public class PlayerLogInServiceTests
    {
        private const string SyntheticPassword = "Synthetic12!";

        private const string SyntheticEmail = "player@example.test";

        private const string PlayerNickname = "Player";

        private const string IncorrectPassword = "Wrong12!";

        private const string InactiveUserState = "USER_INACTIVE";

        /// <summary>Verifica LogIn_WithInvalidFields_ReturnsExpectedError.</summary>
        [Theory]
        [InlineData(null, "p", "Login_IncompleteFieldsError")]
        [InlineData("", "p", "Login_IncompleteFieldsError")]
        [InlineData("  ", "p", "Login_IncompleteFieldsError")]
        [InlineData(SyntheticEmail, null, "Login_IncompleteFieldsError")]
        [InlineData(SyntheticEmail, "", "Login_IncompleteFieldsError")]
        [InlineData(SyntheticEmail, " ", "Login_IncompleteFieldsError")]
        [InlineData("bad", "p", "Login_EmailFormatError")]
        [InlineData("abcde@example.test", "p", "Login_EmailFormatError")]
        public void LogIn_WithInvalidFields_ReturnsExpectedError(string email, string password, string expected)
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PlayerLogInService service = new PlayerLogInService(
                repository,
                new PasswordHasher(),
                new SessionRegistry());
            OperationLogInRequest request = new OperationLogInRequest { Email = email, Password = password };

            // Act
            string errorKey = service.LogIn(request).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(expected, errorKey);
        }

        /// <summary>Verifica LogIn_WithValidAccount_ReturnsExpectedResult.</summary>
        [Fact]
        public void LogIn_WithValidAccount_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };

            // Act
            bool actual = service.LogIn(request).OperationResult.IsSuccess;

            // Assert
            Assert.True(actual);
        }

        /// <summary>Verifica LogIn_WithNotFound_ReturnsExpectedResult.</summary>
        [Fact]
        public void LogIn_WithNotFound_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };
            repository.Credentials.Status = CredentialsLookupStatus.NotFound;

            // Act
            string actual = service.LogIn(request).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Login.InvalidCredentials, actual);
        }

        /// <summary>Verifica LogIn_WithDataFailure_ReturnsExpectedResult.</summary>
        [Fact]
        public void LogIn_WithDataFailure_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };
            repository.Credentials.Status = CredentialsLookupStatus.DataFailure;

            // Act
            string actual = service.LogIn(request).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Login.DataModel, actual);
        }

        /// <summary>Verifica LogIn_WithInactiveAccount_ReturnsExpectedResult.</summary>
        [Fact]
        public void LogIn_WithInactiveAccount_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };
            repository.Credentials.State = InactiveUserState;

            // Act
            string actual = service.LogIn(request).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Login.InvalidCredentials, actual);
        }

        /// <summary>Verifica LogIn_WithWrongPassword_ReturnsExpectedResult.</summary>
        [Fact]
        public void LogIn_WithWrongPassword_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };
            request.Password = IncorrectPassword;

            // Act
            string actual = service.LogIn(request).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Login.InvalidCredentials, actual);
        }

        /// <summary>Verifica LogIn_WithUpdateFailure_ReturnsExpectedResult.</summary>
        [Fact]
        public void LogIn_WithUpdateFailure_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };
            repository.UpdateSucceeds = false;

            // Act
            string actual = service.LogIn(request).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Login.DataModel, actual);
        }

        /// <summary>Verifica LogIn_WithRepositoryException_PropagatesException.</summary>
        [Fact]
        public void LogIn_WithRepositoryException_PropagatesException()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };
            repository.ThrowOnLookup = true;

            // Act
            Action logIn = () => service.LogIn(request);

            // Assert
            Assert.Throws<InvalidOperationException>(logIn);
        }

        /// <summary>Verifica LogIn_WithNullRequest_ReturnsIncompleteFields.</summary>
        [Fact]
        public void LogIn_WithNullRequest_ReturnsIncompleteFields()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub();
            PasswordHasher hasher = new PasswordHasher();
            repository.Credentials = new PlayerCredentials
            {
                Status = CredentialsLookupStatus.Found,
                UserId = 1,
                Nickname = PlayerNickname,
                State = ValidationRules.ActiveUserState,
                PasswordHash = hasher.HashPassword(SyntheticPassword)
            };
            SessionRegistry registry = new SessionRegistry();
            PlayerLogInService service = new PlayerLogInService(repository, hasher, registry);
            OperationLogInRequest request = new OperationLogInRequest
            {
                Email = SyntheticEmail,
                Password = SyntheticPassword
            };

            // Act
            string errorKey = service.LogIn(null).OperationResult.ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Login.IncompleteFields, errorKey);
        }

    }
}
