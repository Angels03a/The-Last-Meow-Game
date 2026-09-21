using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TLMG
{

    /// Provee métodos centralizados para validación de datos de entrada y cálculo de contraseñas.
    public static class ValidationHelper
    {
        private static readonly string[] _permittedEmailDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };

        public static string[] PermittedEmailDomains => _permittedEmailDomains;

        /// Calcula la fuerza de una contraseña en una escala de 0 a 5 según los criterios de seguridad definidos.
        public static int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return 0;
            }

            int passwordStrengthScore = 0;

            if (password.Length >= 12)
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

        /// Valida una dirección de correo y retorna la clave de recurso de error correspondiente a la pantalla solicitante.
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
    }
}
