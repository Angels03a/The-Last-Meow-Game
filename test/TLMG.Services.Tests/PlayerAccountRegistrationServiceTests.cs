using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas de PlayerAccountRegistrationService.</summary>
    public class PlayerAccountRegistrationServiceTests
    {
        private const string SyntheticPassword = "Synthetic12!";

        private const string SyntheticEmail = "player@example.test";

        private const string PlayerNickname = "Player";

        /// <summary>Verifica RegisterPlayerAccount_WithValidRequest_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithValidRequest_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithDuplicateEmail_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithDuplicateEmail_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            repository.EmailStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.EmailTaken, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithDuplicateNickname_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithDuplicateNickname_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            repository.NicknameStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.NicknameTaken, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithEmailLookupFailure_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithEmailLookupFailure_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            repository.EmailStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.DataModel, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithNicknameLookupFailure_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithNicknameLookupFailure_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            repository.NicknameStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.DataModel, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithSaveFailure_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithSaveFailure_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            repository.SaveSucceeds = false;

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.DataModel, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithTermsRejected_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithTermsRejected_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            request.HasAcceptedTerms = false;

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.Terms, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithInvalidNickname_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithInvalidNickname_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            request.Nickname = "!";

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.NicknameFormat, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithInvalidEmail_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithInvalidEmail_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            request.Email = "bad";

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.EmailFormat, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithInvalidPassword_ReturnsExpectedResult.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithInvalidPassword_ReturnsExpectedResult()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            request.Password = "short";

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.PasswordPolicy, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithNullRequest_ReturnsIncompleteFields.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithNullRequest_ReturnsIncompleteFields()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };

            // Act
            string errorKey = service.RegisterPlayerAccount(null).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.IncompleteFields, errorKey);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithRepositoryException_PropagatesException.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithRepositoryException_PropagatesException()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new PasswordHasher());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };
            repository.ThrowOnLookup = true;

            // Act
            Action register = () => service.RegisterPlayerAccount(request);

            // Assert
            Assert.Throws<InvalidOperationException>(register);
        }

        /// <summary>Verifica RegisterPlayerAccount_WithCryptographicFailure_ReturnsUnexpectedError.</summary>
        [Fact]
        public void RegisterPlayerAccount_WithCryptographicFailure_ReturnsUnexpectedError()
        {
            // Arrange
            PlayerRepositoryStub repository = new PlayerRepositoryStub
            {
                EmailStatus = PlayerExistenceLookupStatus.NotFound,
                NicknameStatus = PlayerExistenceLookupStatus.NotFound
            };
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                repository,
                new FailingPasswordHasherStub());
            PlayerAccountRegistration request = new PlayerAccountRegistration
            {
                Nickname = PlayerNickname,
                Email = SyntheticEmail,
                Password = SyntheticPassword,
                HasAcceptedTerms = true
            };

            // Act
            string errorKey = service.RegisterPlayerAccount(request).ErrorKey;

            // Assert
            Assert.Equal(ErrorKeys.Common.Unexpected, errorKey);
        }

    }
}
