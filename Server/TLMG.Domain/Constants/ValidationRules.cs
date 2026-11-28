using System.Text.RegularExpressions;

namespace TLMG.Domain.Constants
{
    /// <summary>
    /// Reglas de negocio centralizadas para validaciones.
    /// Evita números mágicos y cadenas sueltas.
    /// </summary>
    public static class ValidationRules
    {
        public const int EmptyTextLength = 0;
        public const int InvalidCharacterIndex = -1;
        public const int NoUserId = 0;

        public const string ActiveUserState = "USER_ACTIVE";
        public const string SuspendedUserState = "USER_SUSPENDED";
        public const string BannedUserState = "USER_BANNED";

        public const string DefaultProfileImage = "/img/default.png";
        public const int InitialStatisticValue = 0;

        public const string PendingFriendshipState = "FRIEND_PENDING";
        public const string AcceptedFriendshipState = "FRIEND_ACCEPTED";
        public const string RejectedFriendshipState = "FRIEND_REJECTED";

        public const int NicknameMinimumLength = 3;
        public const int NicknameMaximumLength = 12;

        public const int EmailMinimumLength = 5;
        public const int EmailMaximumLength = 254;

        public const int PasswordMinimumLength = 12;
        public const int PasswordMaximumLength = 128;

        public static readonly Regex NicknameRegex = 
            new Regex(@"^[a-zA-Z0-9_]+\z", RegexOptions.Compiled);
        public static readonly Regex EmailRegex = 
            new Regex(@"^[a-zA-Z0-9.]{6,30}@[^@\s]+\.[^@\s]+\z", RegexOptions.Compiled);

        public const int FourCardsPerPlayerCount = 4;
        public const int SixCardsPerPlayerCount = 6;
        public const int EightCardsPerPlayerCount = 8;

        public const int MinimumPlayersPerMatchCount = 3;
        public const int MaximumPlayersPerMatchCount = 6;

        /// <summary>Minimum base points, even when fewer than three players count (Rule 5.0.1).</summary>
        public const int MinimumBasePoints = 2;

        public const int PunishmentChoiceSeconds = 10;

        public const int ChatMaximumMessageLengthCharacters = 160;
        public const string ChatAllowedCharacters = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";
    }
}