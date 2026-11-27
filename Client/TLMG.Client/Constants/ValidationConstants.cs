using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TLMG.Client.Constants
{
    public static class ValidationConstants
    {
        public const int NicknameMinimumLength = 3;
        public const int NicknameMaximumLength = 12;

        public const int EmailMinimumLength = 5;
        public const int EmailMaximumLength = 254;

        public const int PasswordMinimumLength = 12;
        public const int PasswordMaximumLength = 64;
        public const int RequiredPasswordStrengthScore = 5;

        public const int VerificationCodeLength = 6;
        public const int VerificationDigitMaximumLength = 1;

        public const int ChatMessageMinimumLength = 1;
        public const int ChatMessageMaximumLength = 160;
        public const int MaximumChatMessageLength = ChatMessageMaximumLength;

        public const int ReportCommentMaximumLength = 300;
        public const int StatusMessageDurationMilliseconds = 4000;

        public const string AllowedChatCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";

        public static readonly IReadOnlyList<string> AllowedEmailDomains = new List<string>
        {
            "gmail.com",
            "outlook.com",
            "hotmail.com",
            "live.com",
            "estudiantes.uv.mx"
        };

        public static readonly IReadOnlyList<string> ForbiddenNicknameTerms = new List<string>
        {
            "admin",
            "administrator",
            "moderator",
            "system",
            "server",
            "tlmg",
            "guest",
            "null",
            "undefined",
            "unknown"
        };

        public static readonly Regex NicknameFormatRegex = new Regex(
            @"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

        public static readonly Regex EmailInputCharactersRegex = new Regex(
            @"^[a-zA-Z0-9@._\-]+$", RegexOptions.Compiled);

        public static readonly Regex EmailStructureRegex = new Regex(
            @"^[a-zA-Z0-9._\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

        public static readonly Regex DigitsOnlyRegex = new Regex(
            @"^[0-9]+$", RegexOptions.Compiled);

        public static readonly Regex UppercaseLetterRegex = new Regex(
            @"[A-Z]", RegexOptions.Compiled);

        public static readonly Regex LowercaseLetterRegex = new Regex(
            @"[a-z]", RegexOptions.Compiled);

        public static readonly Regex DigitRegex = new Regex(
            @"[0-9]", RegexOptions.Compiled);

        public static readonly Regex SpecialCharacterRegex = new Regex(
            @"[^a-zA-Z0-9\s]", RegexOptions.Compiled);
    }
}
