using System.Collections.Generic;

namespace TLMG.Domain.Constants
{
    /// <summary>
    /// Single source of truth for the avatar keys the server accepts, because the database must never store
    /// client-controlled free text as an avatar.
    /// </summary>
    public static class AvatarCatalog
    {
        public const string DefaultAvatarKey = "AVATAR_CAT";

        private const string ZombieAvatarKey = "AVATAR_ZOMBIE";
        private const string SkullAvatarKey = "AVATAR_SKULL";
        private const string RocketAvatarKey = "AVATAR_ROCKET";
        private const string TigerAvatarKey = "AVATAR_TIGER";
        private const string LionAvatarKey = "AVATAR_LION";
        private const string FrogAvatarKey = "AVATAR_FROG";
        private const string AlienAvatarKey = "AVATAR_ALIEN";

        private static readonly HashSet<string> _validAvatarKeys = new HashSet<string>
        {
            DefaultAvatarKey,
            ZombieAvatarKey,
            SkullAvatarKey,
            RocketAvatarKey,
            TigerAvatarKey,
            LionAvatarKey,
            FrogAvatarKey,
            AlienAvatarKey
        };

        public static bool IsValidKey(string avatarKey)
        {
            bool isValidKey = _validAvatarKeys.Contains(avatarKey);

            return isValidKey;
        }

        /// <summary>Maps legacy or unknown stored values to the default avatar so clients only see valid keys.</summary>
        public static string NormalizeKey(string avatarKey)
        {
            string normalizedKey = DefaultAvatarKey;

            if (IsValidKey(avatarKey))
            {
                normalizedKey = avatarKey;
            }

            return normalizedKey;
        }
    }
}