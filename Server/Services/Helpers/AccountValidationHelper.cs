using System.Linq;
using System.Text.RegularExpressions;

using TLMG.Contracts.Dtos;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Validaciones del servidor para los datos de registro; el cliente no es confiable.
    /// </summary>
    public static class AccountValidationHelper
    {
        private const int NicknameMinLength = 3;
        private const int NicknameMaxLength = 12;
        private const int EmailMinLength = 5;
        private const int EmailMaxLength = 254;
        private const int PasswordMinLength = 12;
        private const int PasswordMaxLength = 128;
        private const string NicknameInvalidErrorKey = "CreateAccount_NicknameFormatError";
        private const string EmailInvalidErrorKey = "CreateAccount_EmailFormatError";
        private const string PasswordInvalidErrorKey = "CreateAccount_PasswordPolicyError";
        private const string TermsNotAcceptedErrorKey = "CreateAccount_TermsError";

        // Se usa \z y no $ para rechazar un salto de línea al final.
        private static readonly Regex _nicknameRegex = new Regex(@"^[a-zA-Z0-9_]+\z", RegexOptions.Compiled);
        private static readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+\z", RegexOptions.Compiled);

        /// <summary>Valida todos los datos del registro.</summary>
        /// <param name="registration">Datos recibidos del cliente.</param>
        /// <returns>La clave del primer error encontrado; null si los datos son válidos.</returns>
        public static string ValidateRegistration(RegistrationData registration)
        {
            string errorKey = ValidateNickname(registration.Nickname);

            if (errorKey == null)
            {
                errorKey = ValidateEmail(registration.Email);
            }

            if (errorKey == null)
            {
                errorKey = ValidatePassword(registration.Password);
            }

            if (errorKey == null && !registration.HasAcceptedTerms)
            {
                errorKey = TermsNotAcceptedErrorKey;
            }

            return errorKey;
        }

        private static string ValidateNickname(string nickname)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(nickname, NicknameMinLength, NicknameMaxLength);
            bool isFormatValid = isLengthValid && _nicknameRegex.IsMatch(nickname);

            if (!isFormatValid)
            {
                errorKey = NicknameInvalidErrorKey;
            }

            return errorKey;
        }

        private static string ValidateEmail(string email)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(email, EmailMinLength, EmailMaxLength);
            bool isFormatValid = isLengthValid && _emailRegex.IsMatch(email);

            if (!isFormatValid)
            {
                errorKey = EmailInvalidErrorKey;
            }

            return errorKey;
        }

        private static string ValidatePassword(string password)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(password, PasswordMinLength, PasswordMaxLength);
            bool isStrong = isLengthValid && HasRequiredCharacterTypes(password);

            if (!isStrong)
            {
                errorKey = PasswordInvalidErrorKey;
            }

            return errorKey;
        }

        private static bool IsLengthInRange(string text, int minLength, int maxLength)
        {
            bool isInRange = false;

            if (text != null)
            {
                isInRange = text.Length >= minLength && text.Length <= maxLength;
            }

            return isInRange;
        }

        private static bool HasRequiredCharacterTypes(string password)
        {
            bool hasUppercase = password.Any(char.IsUpper);
            bool hasLowercase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialCharacter = password.Any(character => !char.IsLetterOrDigit(character));
            bool hasAllTypes = hasUppercase && hasLowercase && hasDigit && hasSpecialCharacter;

            return hasAllTypes;
        }
    }
}