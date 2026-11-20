using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of PlayerAccountRegistrationService with a repository double that records its calls. The format rules
    /// are tested in TLMG.Domain.Tests; here the service must reject invalid requests before it reads or writes
    /// anything, report duplicates and failures with the right key and never store the plain password.
    /// The e-mail verification flow cannot be completed in a unit test because AccountVerificationDispatcher is a
    /// concrete class that sends real e-mail; only its guard paths (no dispatcher configured) are covered.
    /// </summary>
    public class PlayerAccountRegistrationServiceTests
    {
        private const string SyntheticPassword = "Synthetic12!";
        private const string SyntheticEmail = "player@example.test";
        private const string PlayerNickname = "Player";
        private const string CultureCode = "es-MX";

        private readonly PlayerRepositoryStub _repository = new PlayerRepositoryStub
        {
            EmailStatus = PlayerExistenceLookupStatus.NotFound,
            NicknameStatus = PlayerExistenceLookupStatus.NotFound
        };

        [Fact]
        public void RegisterPlayerAccount_WithValidRequest_StoresTheAccountWithNormalizedEmailAndHashedPassword()
        {
            // Arrange
            PasswordHasher hasher = new PasswordHasher();
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(_repository, hasher);
            PlayerAccountRegistration request = CreateRequest(
                PlayerNickname, "  Player@Example.TEST ", SyntheticPassword);

            // Act
            OperationResult result = service.RegisterPlayerAccount(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            PlayerRepositoryStub.CreatedPlayerRecord created = Assert.Single(_repository.CreatedPlayers);
            Assert.Equal(PlayerNickname, created.Nickname);
            Assert.Equal(SyntheticEmail, created.Email);
            Assert.NotEqual(SyntheticPassword, created.PasswordHash);
            Assert.True(hasher.VerifyPassword(SyntheticPassword, created.PasswordHash));
        }

        [Theory]
        [InlineData(null, SyntheticEmail, SyntheticPassword, true, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData("ab", SyntheticEmail, SyntheticPassword, true, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData(PlayerNickname, "abcde@example.test", SyntheticPassword, true, ErrorKeys.CreateAccount.EmailFormat)]
        [InlineData(PlayerNickname, SyntheticEmail, "Short1!", true, ErrorKeys.CreateAccount.PasswordPolicy)]
        [InlineData(PlayerNickname, SyntheticEmail, SyntheticPassword, false, ErrorKeys.CreateAccount.Terms)]
        public void RegisterPlayerAccount_WithInvalidField_RejectsBeforeAnyLookupOrWrite(
            string nickname,
            string email,
            string password,
            bool hasAcceptedTerms,
            string expectedErrorKey)
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            PlayerAccountRegistration request = CreateRequest(nickname, email, password);
            request.HasAcceptedTerms = hasAcceptedTerms;

            // Act
            OperationResult result = service.RegisterPlayerAccount(request);

            // Assert
            AssertRejectedWithoutWrites(result, expectedErrorKey);
            Assert.Empty(_repository.LookedUpEmails);
            Assert.Empty(_repository.LookedUpNicknames);
        }

        [Fact]
        public void RegisterPlayerAccount_WithNullRequest_RejectsWithIncompleteFields()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.RegisterPlayerAccount(null);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.IncompleteFields);
            Assert.Empty(_repository.LookedUpEmails);
        }

        [Fact]
        public void RegisterPlayerAccount_WithTakenEmail_RejectsWithoutCheckingTheNicknameOrWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.EmailTaken);
            Assert.Equal(new[] { SyntheticEmail }, _repository.LookedUpEmails);
            Assert.Empty(_repository.LookedUpNicknames);
        }

        [Fact]
        public void RegisterPlayerAccount_WithTakenNickname_RejectsWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.NicknameStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.NicknameTaken);
            Assert.Equal(new[] { PlayerNickname }, _repository.LookedUpNicknames);
        }

        [Fact]
        public void RegisterPlayerAccount_WithTakenEmailAndNickname_ReportsTheEmail()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.Exists;
            _repository.NicknameStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.EmailTaken);
        }

        [Fact]
        public void RegisterPlayerAccount_WhenTheEmailLookupFails_RejectsWithDataModelErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.DataModel);
            Assert.Empty(_repository.LookedUpNicknames);
        }

        [Fact]
        public void RegisterPlayerAccount_WhenTheNicknameLookupFails_RejectsWithDataModelErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.NicknameStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.DataModel);
        }

        [Fact]
        public void RegisterPlayerAccount_WhenSavingFails_ReturnsDataModelError()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.SaveSucceeds = false;

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.CreateAccount.DataModel, result.ErrorKey);
            Assert.Single(_repository.CreatedPlayers);
        }

        [Fact]
        public void RegisterPlayerAccount_WhenHashingFails_ReturnsUnexpectedErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                _repository,
                new FailingPasswordHasherStub());

            // Act
            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Common.Unexpected);
        }

        [Fact]
        public void RegisterPlayerAccount_WhenTheRepositoryThrows_PropagatesTheExceptionWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.ThrowOnLookup = true;

            // Act
            Action register = () => service.RegisterPlayerAccount(CreateValidRequest());

            // Assert
            Assert.Throws<InvalidOperationException>(register);
            Assert.Empty(_repository.CreatedPlayers);
        }

        [Fact]
        public void RequestVerificationCode_WithInvalidRequest_RejectsBeforeAnyLookup()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            PlayerAccountRegistration request = CreateRequest(PlayerNickname, SyntheticEmail, "Short1!");

            // Act
            OperationResult result = service.RequestVerificationCode(request, CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.PasswordPolicy);
            Assert.Empty(_repository.LookedUpEmails);
        }

        [Fact]
        public void RequestVerificationCode_WithTakenEmail_RejectsWithEmailTaken()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.EmailTaken);
        }

        [Fact]
        public void RequestVerificationCode_WithoutConfiguredDispatcher_ReturnsUnexpectedErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Common.Unexpected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResendVerificationCode_WithMissingEmail_RejectsWithEmailFormat(string email)
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.ResendVerificationCode(email, CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.EmailFormat);
        }

        [Fact]
        public void ResendVerificationCode_WithoutConfiguredDispatcher_ReturnsUnexpectedError()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.ResendVerificationCode(SyntheticEmail, CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Common.Unexpected);
        }

        [Fact]
        public void CompleteRegistration_WithNullRequest_RejectsWithIncompleteFields()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.CompleteRegistration(null, "123456");

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.IncompleteFields);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CompleteRegistration_WithMissingCode_RejectsWithIncorrectCode(string verificationCode)
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), verificationCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Verification.IncorrectCode);
        }

        [Fact]
        public void CompleteRegistration_WithoutConfiguredDispatcher_ReturnsUnexpectedErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), "123456");

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Common.Unexpected);
        }

        private PlayerAccountRegistrationService CreateService()
        {
            return new PlayerAccountRegistrationService(_repository, new PasswordHasher());
        }

        private static PlayerAccountRegistration CreateValidRequest()
        {
            return CreateRequest(PlayerNickname, SyntheticEmail, SyntheticPassword);
        }

        private static PlayerAccountRegistration CreateRequest(string nickname, string email, string password)
        {
            return new PlayerAccountRegistration
            {
                Nickname = nickname,
                Email = email,
                Password = password,
                HasAcceptedTerms = true
            };
        }

        private void AssertRejectedWithoutWrites(OperationResult result, string expectedErrorKey)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorKey, result.ErrorKey);
            Assert.Empty(_repository.CreatedPlayers);
        }
    }
}
