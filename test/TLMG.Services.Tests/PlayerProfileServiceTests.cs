using System;
using System.Collections.Generic;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of PlayerProfileService. The repository and the session identity are local doubles that record their
    /// calls. The validator and the avatar catalog are the real domain classes, because they are pure logic; the
    /// exact formats they accept are tested in TLMG.Domain.Tests, so here only the representative invalid values are
    /// used to prove that the service rejects before touching the repository.
    /// </summary>
    public class PlayerProfileServiceTests
    {
        private const string ValidToken = "valid-token";
        private const int PlayerUserId = 7;
        private const string Nickname = "Whiskers";
        private const string AvatarKey = "AVATAR_LION";

        private readonly ProfileRepositoryDouble _repository = new ProfileRepositoryDouble();
        private readonly SessionIdentityDouble _identity = new SessionIdentityDouble();

        public PlayerProfileServiceTests()
        {
            _identity.TokenToUserId[ValidToken] = PlayerUserId;
        }

        [Fact]
        public void GetProfile_WithValidSession_ReturnsTheStoredProfileOfThatPlayer()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            DateTime passwordChangedAtUtc = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            _repository.Lookup = new PlayerProfileLookup
            {
                Status = PlayerProfileLookupStatus.Found,
                Nickname = Nickname,
                Email = "whiskers@example.com",
                AvatarKey = AvatarKey,
                PasswordChangedAtUtc = passwordChangedAtUtc
            };

            // Act
            OperationPlayerProfileResult result = service.GetProfile(ValidToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            Assert.Equal(Nickname, result.Nickname);
            Assert.Equal("whiskers@example.com", result.Email);
            Assert.Equal(AvatarKey, result.AvatarKey);
            Assert.Equal(passwordChangedAtUtc, result.PasswordChangedAtUtc);
            Assert.Equal(new[] { PlayerUserId }, _repository.LookedUpUserIds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("AVATAR_OLD")]
        public void GetProfile_WithMissingOrUnknownStoredAvatar_ReturnsTheDefaultAvatar(string storedAvatarKey)
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.Lookup = CreateFoundLookup(storedAvatarKey);

            // Act
            OperationPlayerProfileResult result = service.GetProfile(ValidToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(AvatarCatalog.DefaultAvatarKey, result.AvatarKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown-token")]
        public void GetProfile_WithInvalidToken_RejectsWithInvalidSessionWithoutReadingTheProfile(string token)
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationPlayerProfileResult result = service.GetProfile(token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
            Assert.Empty(_repository.LookedUpUserIds);
        }

        [Fact]
        public void GetProfile_WhenTheProfileDoesNotExist_RejectsWithInvalidSessionAndNoData()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.Lookup = new PlayerProfileLookup { Status = PlayerProfileLookupStatus.NotFound };

            // Act
            OperationPlayerProfileResult result = service.GetProfile(ValidToken);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
            Assert.Null(result.Nickname);
            Assert.Null(result.Email);
            Assert.Null(result.AvatarKey);
        }

        [Fact]
        public void GetProfile_WhenTheQueryFails_RejectsWithDataModelErrorAndDoesNotLeakTheLookupData()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.Lookup = CreateFoundLookup(AvatarKey);
            _repository.Lookup.Status = PlayerProfileLookupStatus.DataFailure;

            // Act
            OperationPlayerProfileResult result = service.GetProfile(ValidToken);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Profile.DataModel, result.ErrorKey);
            Assert.Null(result.Nickname);
            Assert.Null(result.Email);
            Assert.Null(result.AvatarKey);
        }

        [Fact]
        public void GetProfile_WhenTheLookupStatusIsNotRecognized_RejectsWithUnexpectedError()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.Lookup = CreateFoundLookup(AvatarKey);
            _repository.Lookup.Status = (PlayerProfileLookupStatus)99;

            // Act
            OperationPlayerProfileResult result = service.GetProfile(ValidToken);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, result.ErrorKey);
            Assert.Null(result.Nickname);
        }

        [Fact]
        public void UpdateProfile_WithValidRequest_SavesTheProfileAndSynchronizesTheSessionNickname()
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, Nickname, AvatarKey));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            ProfileRepositoryDouble.NicknameCheck check = Assert.Single(_repository.NicknameChecks);
            Assert.Equal(PlayerUserId, check.UserId);
            Assert.Equal(Nickname, check.Nickname);
            ProfileRepositoryDouble.UpdateCall update = Assert.Single(_repository.Updates);
            Assert.Equal(PlayerUserId, update.UserId);
            Assert.Equal(Nickname, update.Update.Nickname);
            Assert.Equal(AvatarKey, update.Update.AvatarKey);
            SessionIdentityDouble.NicknameSync sync = Assert.Single(_identity.NicknameSyncs);
            Assert.Equal(ValidToken, sync.SessionToken);
            Assert.Equal(Nickname, sync.Nickname);
        }

        [Fact]
        public void UpdateProfile_WithNullRequest_RejectsWithUnexpectedErrorWithoutTouchingTheRepository()
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.Unexpected, result.ErrorKey);
            AssertNothingWrittenOrChecked();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("thirteenchars")]
        [InlineData("bad name")]
        public void UpdateProfile_WithInvalidNickname_RejectsWithNicknameFormatWithoutTouchingTheRepository(
            string nickname)
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, nickname, AvatarKey));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Profile.NicknameFormat, result.ErrorKey);
            AssertNothingWrittenOrChecked();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("AVATAR_UNKNOWN")]
        public void UpdateProfile_WithAvatarOutsideTheCatalog_RejectsWithAvatarErrorWithoutTouchingTheRepository(
            string avatarKey)
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, Nickname, avatarKey));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Profile.AvatarNotValid, result.ErrorKey);
            AssertNothingWrittenOrChecked();
        }

        [Fact]
        public void UpdateProfile_WithInvalidNicknameAndInvalidAvatar_ReportsTheNicknameErrorFirst()
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, "ab", "AVATAR_UNKNOWN"));

            // Assert
            Assert.Equal(ErrorKeys.Profile.NicknameFormat, result.ErrorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown-token")]
        public void UpdateProfile_WithInvalidToken_RejectsWithInvalidSessionWithoutTouchingTheRepository(string token)
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(token, Nickname, AvatarKey));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
            AssertNothingWrittenOrChecked();
        }

        [Fact]
        public void UpdateProfile_WithInvalidTokenAndInvalidNickname_ReportsTheNicknameErrorFirst()
        {
            // Arrange
            PlayerProfileService service = CreateService();

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest("unknown-token", "ab", AvatarKey));

            // Assert
            Assert.Equal(ErrorKeys.Profile.NicknameFormat, result.ErrorKey);
        }

        [Fact]
        public void UpdateProfile_WhenAnotherPlayerUsesTheNickname_RejectsWithNicknameTakenWithoutSaving()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.NicknameStatus = PlayerExistenceLookupStatus.Exists;

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, Nickname, AvatarKey));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Profile.NicknameTaken, result.ErrorKey);
            Assert.Single(_repository.NicknameChecks);
            Assert.Empty(_repository.Updates);
            Assert.Empty(_identity.NicknameSyncs);
        }

        [Fact]
        public void UpdateProfile_WhenTheNicknameCheckFails_RejectsWithDataModelErrorWithoutSaving()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.NicknameStatus = PlayerExistenceLookupStatus.DataFailure;

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, Nickname, AvatarKey));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Profile.DataModel, result.ErrorKey);
            Assert.Empty(_repository.Updates);
            Assert.Empty(_identity.NicknameSyncs);
        }

        [Theory]
        [InlineData(PlayerProfileUpdateStatus.NicknameTaken, ErrorKeys.Profile.NicknameTaken)]
        [InlineData(PlayerProfileUpdateStatus.PlayerNotFound, ErrorKeys.Common.InvalidSession)]
        [InlineData(PlayerProfileUpdateStatus.DataFailure, ErrorKeys.Profile.DataModel)]
        [InlineData((PlayerProfileUpdateStatus)99, ErrorKeys.Common.Unexpected)]
        public void UpdateProfile_WhenTheSaveIsRejected_ReturnsTheMatchingErrorWithoutChangingTheSession(
            PlayerProfileUpdateStatus updateStatus,
            string expectedErrorKey)
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _repository.UpdateStatus = updateStatus;

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, Nickname, AvatarKey));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorKey, result.ErrorKey);
            Assert.Single(_repository.Updates);
            Assert.Empty(_identity.NicknameSyncs);
        }

        [Fact]
        public void UpdateProfile_WhenTheSessionNicknameCannotBeSynchronized_StillReportsSuccessBecauseItWasSaved()
        {
            // Arrange
            PlayerProfileService service = CreateService();
            _identity.SyncSucceeds = false;

            // Act
            OperationResult result = service.UpdateProfile(CreateRequest(ValidToken, Nickname, AvatarKey));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorKey);
            Assert.Single(_repository.Updates);
            Assert.Single(_identity.NicknameSyncs);
        }

        private PlayerProfileService CreateService()
        {
            return new PlayerProfileService(_repository, _identity);
        }

        private static PlayerProfileLookup CreateFoundLookup(string avatarKey)
        {
            return new PlayerProfileLookup
            {
                Status = PlayerProfileLookupStatus.Found,
                Nickname = Nickname,
                Email = "whiskers@example.com",
                AvatarKey = avatarKey
            };
        }

        private static OperationUpdatePlayerProfileRequest CreateRequest(
            string token,
            string nickname,
            string avatarKey)
        {
            return new OperationUpdatePlayerProfileRequest
            {
                SessionToken = token,
                Nickname = nickname,
                AvatarKey = avatarKey
            };
        }

        private void AssertNothingWrittenOrChecked()
        {
            Assert.Empty(_repository.NicknameChecks);
            Assert.Empty(_repository.Updates);
            Assert.Empty(_identity.NicknameSyncs);
        }

        private sealed class ProfileRepositoryDouble : IPlayerProfileRepository
        {
            internal PlayerProfileLookup Lookup { get; set; }

            internal PlayerExistenceLookupStatus NicknameStatus { get; set; } = PlayerExistenceLookupStatus.NotFound;

            internal PlayerProfileUpdateStatus UpdateStatus { get; set; } = PlayerProfileUpdateStatus.Updated;

            internal List<int> LookedUpUserIds { get; } = new List<int>();

            internal List<NicknameCheck> NicknameChecks { get; } = new List<NicknameCheck>();

            internal List<UpdateCall> Updates { get; } = new List<UpdateCall>();

            public PlayerProfileLookup FindProfileByUserId(int userId)
            {
                LookedUpUserIds.Add(userId);

                return Lookup;
            }

            public PlayerExistenceLookupStatus FindNicknameExistenceStatusForOtherUsers(int userId, string nickname)
            {
                NicknameChecks.Add(new NicknameCheck(userId, nickname));

                return NicknameStatus;
            }

            public PlayerProfileUpdateStatus UpdateProfile(int userId, PlayerProfileUpdate profileUpdate)
            {
                Updates.Add(new UpdateCall(userId, profileUpdate));

                return UpdateStatus;
            }

            internal sealed class NicknameCheck
            {
                internal NicknameCheck(int userId, string nickname)
                {
                    UserId = userId;
                    Nickname = nickname;
                }

                internal int UserId { get; }

                internal string Nickname { get; }
            }

            internal sealed class UpdateCall
            {
                internal UpdateCall(int userId, PlayerProfileUpdate update)
                {
                    UserId = userId;
                    Update = update;
                }

                internal int UserId { get; }

                internal PlayerProfileUpdate Update { get; }
            }
        }

        private sealed class SessionIdentityDouble : ISessionIdentityProvider
        {
            internal Dictionary<string, int> TokenToUserId { get; } = new Dictionary<string, int>();

            internal bool SyncSucceeds { get; set; } = true;

            internal List<NicknameSync> NicknameSyncs { get; } = new List<NicknameSync>();

            public bool TryGetUserId(string sessionToken, out int userId)
            {
                userId = 0;

                return sessionToken != null && TokenToUserId.TryGetValue(sessionToken, out userId);
            }

            public bool TryUpdateNickname(string sessionToken, string nickname)
            {
                NicknameSyncs.Add(new NicknameSync(sessionToken, nickname));

                return SyncSucceeds;
            }

            internal sealed class NicknameSync
            {
                internal NicknameSync(string sessionToken, string nickname)
                {
                    SessionToken = sessionToken;
                    Nickname = nickname;
                }

                internal string SessionToken { get; }

                internal string Nickname { get; }
            }
        }
    }
}
