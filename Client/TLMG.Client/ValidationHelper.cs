using System.Linq;
using System.Text.RegularExpressions;

namespace TLMG.Client
{
    /// <summary>
    /// Provee métodos centralizados para validación de datos de entrada y cálculo de contraseñas.
    /// </summary>
    internal static class ValidationHelper
    {

        /// <summary>Cantidad mínima de caracteres que acepta un nickname.</summary>
        public const int MinNicknameLengthCharacters = 3;
        /// <summary>Cantidad máxima de caracteres que acepta un nickname.</summary>
        public const int MaxNicknameLengthCharacters = 12;
        private const int MinPasswordLengthCharacters = 12;
        /// <summary>Puntaje de fuerza que debe alcanzar una contraseña nueva, en la escala de 0 a 5.</summary>
        public const int RequiredPasswordStrengthScore = 5;
        /// <summary>Tiempo que un mensaje de estado permanece visible antes de ocultarse.</summary>
        public const int StatusMessageDurationMilliseconds = 4000;
        private static readonly string[] _unavailableNicknames = { "admin", "whiskers" };
        private static readonly string[] _offensiveVocabularyWords = { "idiota", "estúpido" };

        private static readonly string[] _permittedEmailDomains =
        {
            "outlook.com",
            "hotmail.com",
            "live.com",
            "gmail.com",
            "uv.mx"
        };

        /// <summary>
        /// Calcula la fuerza de una contraseña en una escala de 0 a 5 según los criterios de seguridad definidos.
        /// </summary>
        public static int CalculatePasswordStrength(string password)
        {
            int passwordStrengthScore = 0;

            if (!string.IsNullOrEmpty(password))
            {
                bool hasMinimumLength = password.Length >= MinPasswordLengthCharacters;
                bool hasUppercaseLetter = Regex.IsMatch(password, "[A-Z]");
                bool hasLowercaseLetter = Regex.IsMatch(password, "[a-z]");
                bool hasDigit = Regex.IsMatch(password, "[0-9]");
                bool hasSpecialCharacter = Regex.IsMatch(password, @"[^A-Za-z0-9]");

                passwordStrengthScore =
                    (hasMinimumLength ? 1 : 0) +
                    (hasUppercaseLetter ? 1 : 0) +
                    (hasLowercaseLetter ? 1 : 0) +
                    (hasDigit ? 1 : 0) +
                    (hasSpecialCharacter ? 1 : 0);
            }

            return passwordStrengthScore;
        }

        /// <summary>
        /// Valida una dirección de correo y retorna la clave de recurso de error correspondiente.
        /// </summary>
        public static string GetEmailValidationErrorKey(string emailAddress, string screenPrefix)
        {
            string validationErrorKey = null;

            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                bool hasForbiddenCharacters = Regex.IsMatch(emailAddress, @"[%&]");
                bool hasMissingAtOrDot = !emailAddress.Contains("@") || !emailAddress.Contains(".");

                if (hasForbiddenCharacters)
                {
                    validationErrorKey = $"{screenPrefix}_EmailCharactersError";
                }
                else if (hasMissingAtOrDot)
                {
                    validationErrorKey = $"{screenPrefix}_EmailFormatError";
                }
                else
                {
                    int atIndex = emailAddress.IndexOf('@');
                    string extractedDomain = emailAddress.Substring(atIndex + 1).ToLower();

                    if (!_permittedEmailDomains.Contains(extractedDomain))
                    {
                        validationErrorKey = $"{screenPrefix}_EmailDomainError";
                    }
                }
            }

            return validationErrorKey;
        }

        /// <summary>
        /// Valida el formato y contenido de un nickname y retorna la clave de recurso de error correspondiente.
        /// </summary>
        public static string GetNicknameValidationErrorKey(string nickname)
        {
            string validationErrorKey = null;

            if (!string.IsNullOrEmpty(nickname))
            {
                bool isLengthValid = nickname.Length >= MinNicknameLengthCharacters
                    && nickname.Length <= MaxNicknameLengthCharacters;
                bool hasOnlyValidCharacters = Regex.IsMatch(nickname, "^[a-zA-Z0-9_]+$");
                bool isFormatInvalid = !isLengthValid || !hasOnlyValidCharacters;

                string lowercaseNickname = nickname.ToLower();
                bool hasOffensiveVocabulary = _offensiveVocabularyWords.Any(
                    offensiveWord => lowercaseNickname.Contains(offensiveWord));
                bool isNicknameUnavailable = _unavailableNicknames.Contains(lowercaseNickname);

                if (isFormatInvalid)
                {
                    validationErrorKey = "CreateAccount_NicknameFormatError";
                }
                else if (hasOffensiveVocabulary)
                {
                    validationErrorKey = "CreateAccount_NicknameOffensiveError";
                }
                else if (isNicknameUnavailable)
                {
                    validationErrorKey = "CreateAccount_NicknameTakenError";
                }
            }

            return validationErrorKey;
        }
    }
}
