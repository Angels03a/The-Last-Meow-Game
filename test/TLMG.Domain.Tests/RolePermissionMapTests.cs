using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using Xunit;

public class RolePermissionMapTests
{
    [Theory]
    [InlineData(SystemPermission.UseChat)]
    [InlineData(SystemPermission.CreateRoom)]
    [InlineData(SystemPermission.SendFriendRequest)]
    [InlineData(SystemPermission.ManageProfile)]
    [InlineData(SystemPermission.ChangePassword)]
    public void IsGranted_GuestWithRestrictedPermission_ReturnsFalse(SystemPermission restrictedPermission)
    {
        // Arrange
        AccountRole guestRole = AccountRole.Guest;

        // Act
        bool isGranted = RolePermissionMap.IsGranted(guestRole, restrictedPermission);

        // Assert
        Assert.False(isGranted);
    }

    [Theory]
    [InlineData(SystemPermission.RespondToFriendRequest)]
    [InlineData(SystemPermission.AcceptRoomInvitation)]
    [InlineData(SystemPermission.InviteFriendToRoom)]
    public void IsGranted_GuestWithAllowedPermission_ReturnsTrue(SystemPermission allowedPermission)
    {
        // Arrange
        AccountRole guestRole = AccountRole.Guest;

        // Act
        bool isGranted = RolePermissionMap.IsGranted(guestRole, allowedPermission);

        // Assert
        Assert.True(isGranted);
    }

    [Theory]
    [InlineData(SystemPermission.UseChat)]
    [InlineData(SystemPermission.CreateRoom)]
    [InlineData(SystemPermission.SendFriendRequest)]
    [InlineData(SystemPermission.RespondToFriendRequest)]
    [InlineData(SystemPermission.AcceptRoomInvitation)]
    [InlineData(SystemPermission.InviteFriendToRoom)]
    [InlineData(SystemPermission.ManageProfile)]
    [InlineData(SystemPermission.ChangePassword)]
    public void IsGranted_RegisteredPlayerWithAnyPermission_ReturnsTrue(SystemPermission permission)
    {
        // Arrange
        AccountRole registeredRole = AccountRole.RegisteredPlayer;

        // Act
        bool isGranted = RolePermissionMap.IsGranted(registeredRole, permission);

        // Assert
        Assert.True(isGranted);
    }
}

public class GuestIdentityTests
{
    [Fact]
    public void CreateNickname_WithMaximumGuestNumber_FitsNicknameMaximumLength()
    {
        // Arrange
        int maximumGuestNumber = GuestIdentity.MaximumGuestNumber;

        // Act
        string nickname = GuestIdentity.CreateNickname(maximumGuestNumber);

        // Assert
        Assert.True(nickname.Length <= ValidationRules.NicknameMaximumLength);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void IsGuestUserId_VariousIds_ClassifiesByIdSign(int userId, bool expectedIsGuest)
    {
        // Arrange
        int candidateUserId = userId;

        // Act
        bool isGuest = GuestIdentity.IsGuestUserId(candidateUserId);

        // Assert
        Assert.Equal(expectedIsGuest, isGuest);
    }
}