using Xunit;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of AvatarCatalog. The eight keys are written here on purpose: the catalog is defined by the code, so
    /// adding or removing an avatar must be a conscious change that updates this list.
    /// </summary>
    public class AvatarCatalogTests
    {
        [Theory]
        [InlineData("AVATAR_CAT")]
        [InlineData("AVATAR_ZOMBIE")]
        [InlineData("AVATAR_SKULL")]
        [InlineData("AVATAR_ROCKET")]
        [InlineData("AVATAR_TIGER")]
        [InlineData("AVATAR_LION")]
        [InlineData("AVATAR_FROG")]
        [InlineData("AVATAR_ALIEN")]
        public void IsValidKey_WithCatalogKey_ReturnsTrueAndNormalizeKeepsIt(string avatarKey)
        {
            // Arrange

            // Act
            bool isValid = AvatarCatalog.IsValidKey(avatarKey);
            string normalizedKey = AvatarCatalog.NormalizeKey(avatarKey);

            // Assert
            Assert.True(isValid);
            Assert.Equal(avatarKey, normalizedKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("AVATAR_UNKNOWN")]
        [InlineData("AVATAR_CAT ")]
        [InlineData("AVATAR")]
        public void IsValidKey_WithKeyOutsideTheCatalog_ReturnsFalseAndNormalizeUsesTheDefault(string avatarKey)
        {
            // Arrange

            // Act
            bool isValid = AvatarCatalog.IsValidKey(avatarKey);
            string normalizedKey = AvatarCatalog.NormalizeKey(avatarKey);

            // Assert
            Assert.False(isValid);
            Assert.Equal(AvatarCatalog.DefaultAvatarKey, normalizedKey);
        }

        [Fact]
        public void DefaultAvatarKey_Always_IsTheCatAvatarAndIsValid()
        {
            // Arrange

            // Act
            string defaultKey = AvatarCatalog.DefaultAvatarKey;

            // Assert
            Assert.Equal("AVATAR_CAT", defaultKey);
            Assert.True(AvatarCatalog.IsValidKey(defaultKey));
        }
    }
}
