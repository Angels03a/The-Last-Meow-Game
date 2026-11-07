using System.Text.RegularExpressions;

namespace TLMG.Domain.Constants
{
    /// <summary>
    /// Business rules for validations centralized in one place.
    /// It avoids magic numbers and loose strings in validators.
    /// </summary>
    public static class ValidationRules
    {
        public const int NicknameMinimumLength = 3;
        public const int NicknameMaximumLength = 12;

        public const int EmailMinimumLength = 5;
        public const int EmailMaximumLength = 254;

        public const int PasswordMinimumLength = 12;
        public const int PasswordMaximumLength = 128;
        public const int EmptyTextLength = 0;
        public const int InvalidCharacterIndex = -1;

        public static readonly Regex NicknameRegex = 
            new Regex(@"^[a-zA-Z0-9_]+\z", RegexOptions.Compiled);
        public static readonly Regex EmailRegex = 
            new Regex(@"^[a-zA-Z0-9.]{6,30}@[^@\s]+\.[^@\s]+\z", RegexOptions.Compiled);

        public const int ChatMaximumMessageLengthCharacters = 160;
        public const string ChatAllowedCharacters = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";
    }
}