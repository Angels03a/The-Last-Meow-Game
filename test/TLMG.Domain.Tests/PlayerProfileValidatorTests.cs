using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>Tests of PlayerProfileValidator: nickname first, then avatar.</summary>
    public class PlayerProfileValidatorTests
    {
        private const string ValidNickname = "Player";
        private const string ValidAvatarKey = AvatarCatalog.DefaultAvatarKey;

        [Theory]
        [InlineData("abc", "AVATAR_CAT")]
        [InlineData("abcdefghijkl", "AVATAR_ALIEN")]
        [InlineData("Cat_1", "AVATAR_ZOMBIE")]
        public void ValidateProfileUpdate_WithValidNicknameAndAvatar_ReturnsEmpty(string nickname, string avatarKey)
        {
            // Arrange
            PlayerProfileUpdate update = new PlayerProfileUpdate { Nickname = nickname, AvatarKey = avatarKey };

            // Act
            string errorKey = PlayerProfileValidator.ValidateProfileUpdate(update);

            // Assert
            Assert.Equal(string.Empty, errorKey);
        }

        [Fact]
        public void ValidateProfileUpdate_WithNullUpdate_ReturnsAnError()
        {
            // Arrange
            PlayerProfileUpdate update = null;

            // Act
            string errorKey = PlayerProfileValidator.ValidateProfileUpdate(update);

            // Assert
            Assert.NotNull(errorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")]
        [InlineData("abcdefghijklm")]
        [InlineData("Cat One")]
        [InlineData("Gatñ")]
        public void ValidateProfileUpdate_WithInvalidNickname_ReturnsProfileNicknameFormatError(string nickname)
        {
            // Arrange
            PlayerProfileUpdate update = new PlayerProfileUpdate { Nickname = nickname, AvatarKey = ValidAvatarKey };

            // Act
            string errorKey = PlayerProfileValidator.ValidateProfileUpdate(update);

            // Assert
            Assert.Equal(ErrorKeys.Profile.NicknameFormat, errorKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("AVATAR_UNKNOWN")]
        [InlineData("AVATAR_CAT ")]
        public void ValidateProfileUpdate_WithInvalidAvatar_ReturnsAvatarNotValidError(string avatarKey)
        {
            // Arrange
            PlayerProfileUpdate update = new PlayerProfileUpdate { Nickname = ValidNickname, AvatarKey = avatarKey };

            // Act
            string errorKey = PlayerProfileValidator.ValidateProfileUpdate(update);

            // Assert
            Assert.Equal(ErrorKeys.Profile.AvatarNotValid, errorKey);
        }

        [Fact]
        public void ValidateProfileUpdate_WithInvalidNicknameAndAvatar_ReportsTheNicknameFirst()
        {
            // Arrange
            PlayerProfileUpdate update = new PlayerProfileUpdate { Nickname = "ab", AvatarKey = "AVATAR_UNKNOWN" };

            // Act
            string errorKey = PlayerProfileValidator.ValidateProfileUpdate(update);

            // Assert
            Assert.Equal(ErrorKeys.Profile.NicknameFormat, errorKey);
        }
    }
}
