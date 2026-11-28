using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Utilities;

namespace TLMG.Services.Tests
{
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
        public void RegisterPlayerAccount_WithValidRequest_DeniesWithoutStartingRegistration()
        {
            VerificationCodeRepositoryStub codes = new VerificationCodeRepositoryStub();
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);

            OperationResult result = service.RegisterPlayerAccount(CreateValidRequest());

            AssertRejectedWithoutWrites(result, ErrorKeys.Common.PermissionDenied);
            Assert.Empty(_repository.LookedUpEmails);
            Assert.Empty(_repository.LookedUpNicknames);
            Assert.Empty(codes.SavedPendingRegistrations);
            Assert.Empty(codes.SavedCodes);
        }

        [Fact]
        public void RequestVerificationCode_StoresNormalizedEmailAndHashedPasswordInPendingRecord()
        {
            VerificationCodeRepositoryStub codes = new VerificationCodeRepositoryStub();
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);
            PlayerAccountRegistration request = CreateRequest(
                PlayerNickname, "  Player@Example.TEST ", SyntheticPassword);

            service.RequestVerificationCode(request, CultureCode);

            PendingRegistrationRecord record = Assert.Single(codes.SavedPendingRegistrations);
            Assert.Equal(SyntheticEmail, record.Email);
            Assert.Equal(PlayerNickname, record.Nickname);
            Assert.True(new PasswordHasher().VerifyPassword(SyntheticPassword, record.PasswordHash));
            Assert.Empty(_repository.CreatedPlayers);
        }

        [Theory]
        [InlineData(null, SyntheticEmail, SyntheticPassword, true, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData("ab", SyntheticEmail, SyntheticPassword, true, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData(PlayerNickname, "abcde@example.test", SyntheticPassword, true, ErrorKeys.CreateAccount.EmailFormat)]
        [InlineData(PlayerNickname, SyntheticEmail, SyntheticPassword, false, ErrorKeys.CreateAccount.Terms)]
        public void RequestVerificationCode_WithInvalidField_RejectsBeforeAnyLookupOrWrite(
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
            OperationResult result = service.RequestVerificationCode(request, CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, expectedErrorKey);
            Assert.Empty(_repository.LookedUpEmails);
            Assert.Empty(_repository.LookedUpNicknames);
        }

        [Fact]
        public void RequestVerificationCode_WithNullRequest_RejectsWithIncompleteFields()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();

            // Act
            OperationResult result = service.RequestVerificationCode(null, CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.IncompleteFields);
            Assert.Empty(_repository.LookedUpEmails);
        }

        [Fact]
        public void RequestVerificationCode_WithTakenEmail_RejectsWithoutCheckingTheNicknameOrWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.EmailTaken);
            Assert.Equal(new[] { SyntheticEmail }, _repository.LookedUpEmails);
            Assert.Empty(_repository.LookedUpNicknames);
        }

        [Fact]
        public void RequestVerificationCode_WithTakenNickname_RejectsWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.NicknameStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.NicknameTaken);
            Assert.Equal(new[] { PlayerNickname }, _repository.LookedUpNicknames);
        }

        [Fact]
        public void RequestVerificationCode_WithTakenEmailAndNickname_ReportsTheEmail()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.Exists;
            _repository.NicknameStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.EmailTaken);
        }

        [Fact]
        public void RequestVerificationCode_WhenTheEmailLookupFails_RejectsWithDataModelErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.EmailStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.DataModel);
            Assert.Empty(_repository.LookedUpNicknames);
        }

        [Fact]
        public void RequestVerificationCode_WhenTheNicknameLookupFails_RejectsWithDataModelErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.NicknameStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.DataModel);
        }

        [Fact]
        public void CompleteRegistration_WhenSavingFails_ReturnsDataModelError()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateServiceWithCodes(CreatePendingCodes());
            _repository.SaveSucceeds = false;

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), "123456");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.CreateAccount.DataModel, result.ErrorKey);
            Assert.Single(_repository.CreatedPlayers);
        }

        [Fact]
        public void RequestVerificationCode_WhenHashingFails_ReturnsUnexpectedErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                _repository,
                new FailingPasswordHasherStub(),
                new AccountVerificationDispatcher(
                    new VerificationCodeStore(new VerificationCodeRepositoryStub()), null));

            // Act
            OperationResult result = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Common.Unexpected);
        }

        [Fact]
        public void RequestVerificationCode_WhenTheRepositoryThrows_PropagatesTheExceptionWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = CreateService();
            _repository.ThrowOnLookup = true;

            // Act
            void RequestCode()
            {
                service.RequestVerificationCode(CreateValidRequest(), CultureCode);
            }

            // Assert
            Assert.Throws<InvalidOperationException>(RequestCode);
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Registration_WhenExistenceStatusIsUnknown_RejectsBeforeCreatingOrSending(bool unknownEmail)
        {
            // Arrange
            VerificationCodeRepositoryStub codes = new VerificationCodeRepositoryStub();
            AccountVerificationDispatcher dispatcher = new AccountVerificationDispatcher(new VerificationCodeStore(codes), null);
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                _repository, new PasswordHasher(), dispatcher);
            _repository.EmailStatus = unknownEmail ? (PlayerExistenceLookupStatus)999 : PlayerExistenceLookupStatus.NotFound;
            _repository.NicknameStatus = unknownEmail ? PlayerExistenceLookupStatus.NotFound : (PlayerExistenceLookupStatus)999;

            // Act
            OperationResult request = service.RequestVerificationCode(CreateValidRequest(), CultureCode);

            // Assert
            AssertRejectedWithoutWrites(request, ErrorKeys.CreateAccount.DataModel);
            Assert.Empty(codes.SavedPendingRegistrations);
            Assert.Empty(codes.SavedCodes);
        }

        [Fact]
        public void RequestVerificationCode_WithoutConfiguredDispatcher_ReturnsUnexpectedErrorWithoutWriting()
        {
            // Arrange
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                _repository, new PasswordHasher(), null);

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
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                _repository, new PasswordHasher(), null);

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
            PlayerAccountRegistrationService service = new PlayerAccountRegistrationService(
                _repository, new PasswordHasher(), null);

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), "123456");

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.Common.Unexpected);
        }

        [Theory]
        [InlineData(VerificationCodeStatus.Invalid, ErrorKeys.Verification.IncorrectCode)]
        [InlineData(VerificationCodeStatus.Expired, ErrorKeys.Verification.ExpiredCode)]
        [InlineData(VerificationCodeStatus.NoActiveCode, ErrorKeys.Verification.ExpiredCode)]
        [InlineData(VerificationCodeStatus.TooManyAttempts, ErrorKeys.Verification.AttemptsExhausted)]
        [InlineData((VerificationCodeStatus)999, ErrorKeys.CreateAccount.DataModel)]
        public void CompleteRegistration_WithRejectedCode_DoesNotCreateAccount(VerificationCodeStatus status, string key)
        {
            // Arrange
            VerificationCodeRepositoryStub codes = CreatePendingCodes();
            codes.ValidationResultToReturn = new VerificationCodeValidationResult { Status = status };
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), "123456");

            // Assert
            AssertRejectedWithoutWrites(result, key);
            Assert.Empty(codes.DeletedPendingIds);
        }

        [Fact]
        public void CompleteRegistration_WithNullValidation_RejectsWithoutCreation()
        {
            // Arrange
            VerificationCodeRepositoryStub codes = CreatePendingCodes();
            codes.ValidationResultToReturn = null;
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), "123456");

            // Assert
            AssertRejectedWithoutWrites(result, ErrorKeys.CreateAccount.DataModel);
        }

        [Fact]
        public void CompleteRegistration_WithValidCode_CreatesAccountAndReturnsEmptyKey()
        {
            // Arrange
            VerificationCodeRepositoryStub codes = CreatePendingCodes();
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);

            // Act
            OperationResult result = service.CompleteRegistration(CreateValidRequest(), "123456");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.Single(_repository.CreatedPlayers);
            Assert.Single(codes.DeletedPendingIds);
        }

        [Fact]
        public void ResendVerificationCode_WhenSavingCodeFails_Rejects()
        {
            // Arrange
            VerificationCodeRepositoryStub codes = CreatePendingCodes();
            codes.SaveCodeSucceeds = false;
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);

            // Act
            OperationResult result = service.ResendVerificationCode(SyntheticEmail, CultureCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.CreateAccount.DataModel, result.ErrorKey);
            Assert.Single(codes.SavedCodes);
        }

        [Theory]
        [InlineData(PlayerExistenceLookupStatus.DataFailure)]
        [InlineData((PlayerExistenceLookupStatus)999)]
        [InlineData(PlayerExistenceLookupStatus.Exists)]
        public void Registration_WhenPendingLookupFails_RejectsWithoutCreatingOrSending(PlayerExistenceLookupStatus status)
        {
            // Arrange
            VerificationCodeRepositoryStub codes = new VerificationCodeRepositoryStub { PendingStatusToReturn = status };
            PlayerAccountRegistrationService service = CreateServiceWithCodes(codes);

            // Act
            OperationResult confirmation = service.CompleteRegistration(CreateValidRequest(), "123456");
            OperationResult resend = service.ResendVerificationCode(SyntheticEmail, CultureCode);

            // Assert
            AssertRejectedWithoutWrites(confirmation, ErrorKeys.CreateAccount.DataModel);
            AssertRejectedWithoutWrites(resend, ErrorKeys.CreateAccount.DataModel);
            Assert.Empty(codes.ValidatedCodes);
            Assert.Empty(codes.SavedCodes);
        }

        private static VerificationCodeRepositoryStub CreatePendingCodes()
        {
            return new VerificationCodeRepositoryStub
            {
                PendingRegistrationToReturn = new PendingRegistrationRecord
                {
                    PendingId = 1, Email = SyntheticEmail, Nickname = PlayerNickname,
                    PasswordHash = "stored-hash", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
                }
            };
        }

        private PlayerAccountRegistrationService CreateServiceWithCodes(VerificationCodeRepositoryStub codes)
        {
            return new PlayerAccountRegistrationService(_repository, new PasswordHasher(),
                new AccountVerificationDispatcher(new VerificationCodeStore(codes), null));
        }

        private PlayerAccountRegistrationService CreateService()
        {
            return CreateServiceWithCodes(new VerificationCodeRepositoryStub());
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
