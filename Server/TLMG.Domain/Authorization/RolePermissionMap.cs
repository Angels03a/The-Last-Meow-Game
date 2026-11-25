using System.Collections.Generic;

namespace TLMG.Domain.Authorization
{
    public static class RolePermissionMap
    {
        private static readonly Dictionary<AccountRole, HashSet<SystemPermission>> _permissionsByRole =
            CreatePermissionsByRole();

        public static bool IsGranted(AccountRole role, SystemPermission permission)
        {
            HashSet<SystemPermission> grantedPermissions;
            bool isGranted = _permissionsByRole.TryGetValue(role, out grantedPermissions)
                && grantedPermissions.Contains(permission);

            return isGranted;
        }

        private static Dictionary<AccountRole, HashSet<SystemPermission>> CreatePermissionsByRole()
        {
            HashSet<SystemPermission> guestPermissions = new HashSet<SystemPermission>
            {
                SystemPermission.RespondToFriendRequest,
                SystemPermission.AcceptRoomInvitation,
                SystemPermission.InviteFriendToRoom
            };
            HashSet<SystemPermission> registeredPermissions = new HashSet<SystemPermission>(guestPermissions)
            {
                SystemPermission.UseChat,
                SystemPermission.CreateRoom,
                SystemPermission.SendFriendRequest,
                SystemPermission.ManageProfile,
                SystemPermission.ChangePassword
            };
            Dictionary<AccountRole, HashSet<SystemPermission>> permissionsByRole =
                new Dictionary<AccountRole, HashSet<SystemPermission>>
                {
                    { AccountRole.Guest, guestPermissions },
                    { AccountRole.RegisteredPlayer, registeredPermissions }
                };

            return permissionsByRole;
        }
    }
}