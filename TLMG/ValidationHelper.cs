using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TLMG
{
    /// <summary>
    /// Provee métodos centralizados para validación de datos de entrada y cálculo de contraseñas.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Longitud mínima permitida para un nickname en caracteres.
        /// </summary>
        public const int MinNicknameLengthCharacters = 3;

        /// <summary>
        /// Longitud máxima permitida para un nickname en caracteres.
        /// </summary>
        public const int MaxNicknameLengthCharacters = 12;

        /// <summary>
        /// Longitud mínima requerida para una contraseña en caracteres.
        /// </summary>
        public const int MinPasswordLengthCharacters = 12;

        /// <summary>
        /// Puntuación de robustez requerida para que una contraseña se considere segura.
        /// </summary>
        public const int RequiredPasswordStrengthScore = 5;

        /// <summary>
        /// Duración en milisegundos durante la cual se muestra un mensaje de estado en la interfaz de usuario.
        /// </summary>
        public const int StatusMessageDurationMilliseconds = 4000;

        private static readonly string[] _unavailableNicknames = { "admin", "whiskers" };
        private static readonly string[] _offensiveVocabularyWords = { "idiota", "estupido" };

        private static readonly string[] _permittedEmailDomains =
        {
            "outlook.com",
            "hotmail.com",
            "live.com",
            "gmail.com",
            "uv.mx"
        };

        /// <summary>
        /// Obtiene la lista de dominios de correo electrónico permitidos por el sistema.
        /// </summary>
        public static string[] PermittedEmailDomains => _permittedEmailDomains;

        /// <summary>
        /// Calcula la fuerza de una contraseña en una escala de 0 a 5 según los criterios de seguridad definidos.
        /// </summary>
        /// <param name="password">Cadena con la contraseña a evaluar.</param>
        /// <returns>Puntaje de robustez de la contraseña entre 0 y 5.</returns>
        public static int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return 0;
            }

            int passwordStrengthScore = 0;

            if (password.Length >= MinPasswordLengthCharacters)
            {
                passwordStrengthScore++;
            }

            if (Regex.IsMatch(password, "[A-Z]"))
            {
                passwordStrengthScore++;
            }

            if (Regex.IsMatch(password, "[a-z]"))
            {
                passwordStrengthScore++;
            }

            if (Regex.IsMatch(password, "[0-9]"))
            {
                passwordStrengthScore++;
            }

            if (Regex.IsMatch(password, @"[^A-Za-z0-9]"))
            {
                passwordStrengthScore++;
            }

            return passwordStrengthScore;
        }

        /// <summary>
        /// Valida una dirección de correo y retorna la clave de recurso de error correspondiente.
        /// </summary>
        /// <param name="emailAddress">Dirección de correo a validar.</param>
        /// <param name="screenPrefix">Prefijo de la pantalla que solicita la clave de error.</param>
        /// <returns>Clave del recurso de error si es inválido, o null si es válido.</returns>
        public static string GetEmailValidationErrorKey(string emailAddress, string screenPrefix)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return null;
            }

            if (Regex.IsMatch(emailAddress, @"[%&]"))
            {
                return $"{screenPrefix}_EmailCharactersError";
            }

            bool containsAtSign = emailAddress.Contains("@");
            bool containsDot = emailAddress.Contains(".");

            if (!containsAtSign || !containsDot)
            {
                return $"{screenPrefix}_EmailFormatError";
            }

            int atIndex = emailAddress.IndexOf('@');
            string extractedDomain = emailAddress.Substring(atIndex + 1).ToLower();

            if (!_permittedEmailDomains.Contains(extractedDomain))
            {
                return $"{screenPrefix}_EmailDomainError";
            }

            return null;
        }

        /// <summary>
        /// Valida el formato y contenido de un nickname y retorna la clave de recurso de error correspondiente.
        /// </summary>
        /// <param name="nickname">Cadena con el nickname a validar.</param>
        /// <returns>Clave del recurso de error si es inválido, o null si es válido.</returns>
        public static string GetNicknameValidationErrorKey(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return null;
            }

            bool isLengthValid = nickname.Length >= MinNicknameLengthCharacters
                && nickname.Length <= MaxNicknameLengthCharacters;
            bool hasOnlyValidCharacters = Regex.IsMatch(nickname, "^[a-zA-Z0-9_]+$");

            if (!isLengthValid || !hasOnlyValidCharacters)
            {
                return "CreateAccount_NicknameFormatError";
            }

            string lowercaseNickname = nickname.ToLower();
            if (_offensiveVocabularyWords.Any(offensiveWord => lowercaseNickname.Contains(offensiveWord)))
            {
                return "CreateAccount_NicknameOffensiveError";
            }

            if (_unavailableNicknames.Contains(lowercaseNickname))
            {
                return "CreateAccount_NicknameTakenError";
            }

            return null;
        }
    }
}
