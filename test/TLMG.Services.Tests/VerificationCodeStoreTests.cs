using System;

using Xunit;

using TLMG.Domain.Repositories;
using TLMG.Services.Utilities;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of VerificationCodeStore. The repository is a local double that records the records it receives, so the
    /// tests check what the store hands to the database. Whether a code is valid or already used is decided by the
    /// database and is outside these tests.
    /// </summary>
    public class VerificationCodeStoreTests
    {
        private const int PlayerUserId = 7;
        private const int PendingId = 15;
        private const string PlainCode = "123456";
        private const string PlainCodeHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92";
        private const int CodeLifetimeMinutes = 5;
        private const int PendingRegistrationLifetimeMinutes = 15;

        private readonly VerificationCodeRepositoryStub _repository = new VerificationCodeRepositoryStub();

        [Theory]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
        [InlineData(PlainCode, PlainCodeHash)]
        public void ComputeCodeHash_WithKnownCode_ReturnsTheLowercaseHexSha256(string plainCode, string expectedHash)
        {
            // Arrange
            // No setup: the method is pure.

            // Act
            string hash = VerificationCodeStore.ComputeCodeHash(plainCode);

            // Assert
            Assert.Equal(expectedHash, hash);
        }

        [Fact]
        public void ComputeCodeHash_WithDifferentCodes_ReturnsDifferentHashes()
        {
            // Arrange
            string otherCode = "123457";

            // Act
            string hash = VerificationCodeStore.ComputeCodeHash(PlainCode);
            string otherHash = VerificationCodeStore.ComputeCodeHash(otherCode);

            // Assert
            Assert.NotEqual(hash, otherHash);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SavePasswordResetCode_WithAnyRepositoryResult_ReturnsThatResult(bool repositorySucceeds)
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.SaveCodeSucceeds = repositorySucceeds;

            // Act
            bool wasSaved = store.SavePasswordResetCode(PlayerUserId, PlainCode);

            // Assert
            Assert.Equal(repositorySucceeds, wasSaved);
            Assert.Single(_repository.SavedCodes);
        }

        [Fact]
        public void SavePasswordResetCode_WithPlainCode_StoresOnlyTheHashForTheUserWithAFiveMinuteLife()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            DateTime beforeUtc = DateTime.UtcNow;

            // Act
            store.SavePasswordResetCode(PlayerUserId, PlainCode);

            // Assert
            DateTime afterUtc = DateTime.UtcNow;
            VerificationCodeRecord record = Assert.Single(_repository.SavedCodes);
            Assert.Equal(PlayerUserId, record.UserId);
            Assert.False(record.PendingId.HasValue);
            Assert.Equal(VerificationCodeType.PasswordReset, record.Type);
            Assert.Equal(PlainCodeHash, record.CodeHash);
            Assert.InRange(record.GeneratedAtUtc, beforeUtc, afterUtc);
            Assert.Equal(TimeSpan.FromMinutes(CodeLifetimeMinutes), record.ExpiresAtUtc - record.GeneratedAtUtc);
        }

        [Fact]
        public void SaveRegistrationCode_WithPlainCode_StoresOnlyTheHashForThePendingRegistration()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();

            // Act
            bool wasSaved = store.SaveRegistrationCode(PendingId, PlainCode);

            // Assert
            Assert.True(wasSaved);
            VerificationCodeRecord record = Assert.Single(_repository.SavedCodes);
            Assert.False(record.UserId.HasValue);
            Assert.Equal(PendingId, record.PendingId);
            Assert.Equal(VerificationCodeType.Registration, record.Type);
            Assert.Equal(PlainCodeHash, record.CodeHash);
            Assert.Equal(TimeSpan.FromMinutes(CodeLifetimeMinutes), record.ExpiresAtUtc - record.GeneratedAtUtc);
        }

        [Theory]
        [InlineData(VerificationCodeStatus.Valid, 0)]
        [InlineData(VerificationCodeStatus.Invalid, 2)]
        [InlineData(VerificationCodeStatus.TooManyAttempts, 5)]
        [InlineData(VerificationCodeStatus.Expired, 1)]
        [InlineData(VerificationCodeStatus.NoActiveCode, 0)]
        public void ValidatePasswordResetCode_WithAnyRepositoryResult_ReturnsItUnchanged(
            VerificationCodeStatus status,
            int attemptsFailed)
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.ValidationResultToReturn = new VerificationCodeValidationResult
            {
                Status = status,
                AttemptsFailed = attemptsFailed
            };

            // Act
            VerificationCodeValidationResult result = store.ValidatePasswordResetCode(PlayerUserId, PlainCode);

            // Assert
            Assert.Same(_repository.ValidationResultToReturn, result);
            Assert.Equal(status, result.Status);
            Assert.Equal(attemptsFailed, result.AttemptsFailed);
        }

        [Fact]
        public void ValidatePasswordResetCode_WithPlainCode_AsksTheDatabaseToCheckTheHashOfTheUserAtTheCurrentTime()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            DateTime beforeUtc = DateTime.UtcNow;

            // Act
            store.ValidatePasswordResetCode(PlayerUserId, PlainCode);

            // Assert
            DateTime afterUtc = DateTime.UtcNow;
            VerificationCodeRecord record = Assert.Single(_repository.ValidatedCodes);
            Assert.Equal(PlayerUserId, record.UserId);
            Assert.False(record.PendingId.HasValue);
            Assert.Equal(VerificationCodeType.PasswordReset, record.Type);
            Assert.Equal(PlainCodeHash, record.CodeHash);
            Assert.InRange(Assert.Single(_repository.ValidationTimesUtc), beforeUtc, afterUtc);
            Assert.Empty(_repository.SavedCodes);
        }

        [Fact]
        public void ValidateRegistrationCode_WithPlainCode_AsksTheDatabaseToCheckTheHashOfThePendingRegistration()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();

            // Act
            VerificationCodeValidationResult result = store.ValidateRegistrationCode(PendingId, PlainCode);

            // Assert
            Assert.Same(_repository.ValidationResultToReturn, result);
            VerificationCodeRecord record = Assert.Single(_repository.ValidatedCodes);
            Assert.False(record.UserId.HasValue);
            Assert.Equal(PendingId, record.PendingId);
            Assert.Equal(VerificationCodeType.Registration, record.Type);
            Assert.Equal(PlainCodeHash, record.CodeHash);
        }

        [Fact]
        public void SavePendingRegistration_WithPlayerData_StoresThemWithAFifteenMinuteLifeAndReturnsTheId()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.PendingIdToReturn = PendingId;

            // Act
            int? pendingId = store.SavePendingRegistration("Whiskers", "whiskers@example.com", "stored-hash");

            // Assert
            Assert.Equal(PendingId, pendingId);
            PendingRegistrationRecord record = Assert.Single(_repository.SavedPendingRegistrations);
            Assert.Equal("Whiskers", record.Nickname);
            Assert.Equal("whiskers@example.com", record.Email);
            Assert.Equal("stored-hash", record.PasswordHash);
            Assert.Equal(
                TimeSpan.FromMinutes(PendingRegistrationLifetimeMinutes),
                record.ExpiresAtUtc - record.CreatedAtUtc);
        }

        [Fact]
        public void SavePendingRegistration_WhenTheRepositoryReturnsNoId_ReturnsNull()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.PendingIdToReturn = null;

            // Act
            int? pendingId = store.SavePendingRegistration("Whiskers", "whiskers@example.com", "stored-hash");

            // Assert
            Assert.False(pendingId.HasValue);
        }

        [Fact]
        public void FindValidPendingRegistration_WithUnexpiredRecord_ReturnsItAndDeletesNothing()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.PendingRegistrationToReturn = CreatePending(DateTime.UtcNow.AddHours(1));

            // Act
            PendingRegistrationRecord found = store.FindValidPendingRegistration("whiskers@example.com");

            // Assert
            Assert.Same(_repository.PendingRegistrationToReturn, found);
            Assert.Empty(_repository.DeletedPendingIds);
        }

        [Fact]
        public void FindValidPendingRegistration_WithExpiredRecord_DeletesItAndReturnsNull()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.PendingRegistrationToReturn = CreatePending(DateTime.UtcNow.AddHours(-1));

            // Act
            PendingRegistrationRecord found = store.FindValidPendingRegistration("whiskers@example.com");

            // Assert
            Assert.Null(found);
            Assert.Equal(new[] { PendingId }, _repository.DeletedPendingIds);
        }

        [Fact]
        public void FindValidPendingRegistration_WithoutRecord_ReturnsNullAndDeletesNothing()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();
            _repository.PendingRegistrationToReturn = null;

            // Act
            PendingRegistrationRecord found = store.FindValidPendingRegistration("whiskers@example.com");

            // Assert
            Assert.Null(found);
            Assert.Empty(_repository.DeletedPendingIds);
        }

        [Fact]
        public void DeletePendingRegistration_WithPendingId_AsksTheRepositoryToDeleteThatRecord()
        {
            // Arrange
            VerificationCodeStore store = CreateStore();

            // Act
            store.DeletePendingRegistration(PendingId);

            // Assert
            Assert.Equal(new[] { PendingId }, _repository.DeletedPendingIds);
        }

        private VerificationCodeStore CreateStore()
        {
            return new VerificationCodeStore(_repository);
        }

        private static PendingRegistrationRecord CreatePending(DateTime expiresAtUtc)
        {
            return new PendingRegistrationRecord
            {
                PendingId = PendingId,
                Email = "whiskers@example.com",
                ExpiresAtUtc = expiresAtUtc
            };
        }
    }
}
