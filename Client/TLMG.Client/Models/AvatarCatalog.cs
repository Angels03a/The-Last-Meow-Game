using System.Collections.Generic;

namespace TLMG.Client.Models
{
    /// <summary>
    /// Maps the avatar keys stored by the server to the emoji shown on screen; the server never stores emoji
    /// because the database column cannot hold them.
    /// </summary>
    public static class AvatarCatalog
    {
        public const string DefaultAvatarKey = "AVATAR_CAT";
        public const string ZombieAvatarKey = "AVATAR_ZOMBIE";
        public const string SkullAvatarKey = "AVATAR_SKULL";
        public const string RocketAvatarKey = "AVATAR_ROCKET";
        public const string TigerAvatarKey = "AVATAR_TIGER";
        public const string LionAvatarKey = "AVATAR_LION";
        public const string FrogAvatarKey = "AVATAR_FROG";
        public const string AlienAvatarKey = "AVATAR_ALIEN";

        private const string CatEmoji = "🐱";
        private const string ZombieEmoji = "🧟";
        private const string SkullEmoji = "💀";
        private const string RocketEmoji = "🚀";
        private const string TigerEmoji = "🐯";
        private const string LionEmoji = "🦁";
        private const string FrogEmoji = "🐸";
        private const string AlienEmoji = "👾";

        private static readonly Dictionary<string, string> _emojisByKey = new Dictionary<string, string>
        {
            { DefaultAvatarKey, CatEmoji },
            { ZombieAvatarKey, ZombieEmoji },
            { SkullAvatarKey, SkullEmoji },
            { RocketAvatarKey, RocketEmoji },
            { TigerAvatarKey, TigerEmoji },
            { LionAvatarKey, LionEmoji },
            { FrogAvatarKey, FrogEmoji },
            { AlienAvatarKey, AlienEmoji }
        };

        /// <summary>Returns the emoji of an avatar key, or the default cat for null and unknown keys.</summary>
        /// <param name="avatarKey">Key received from the server.</param>
        /// <returns>The emoji to display; never null.</returns>
        public static string FindEmoji(string avatarKey)
        {
            string emoji;

            if (avatarKey == null || !_emojisByKey.TryGetValue(avatarKey, out emoji))
            {
                emoji = CatEmoji;
            }

            return emoji;
        }
    }
}