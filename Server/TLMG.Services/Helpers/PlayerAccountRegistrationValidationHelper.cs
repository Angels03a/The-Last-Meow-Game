using System.Linq;
using System.Text.RegularExpressions;

using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Validaciones del servidor para los datos de registro; el cliente no es confiable.
    /// </summary>
    public static class PlayerAccountRegistrationValidationHelper
    {
        private const int NicknameminimumLength = 3;
        private const int NicknamemaximunLength = 12;
        private const int EmailminimumLength = 5;
        private const int EmailmaximunLength = 254;
        private const int PasswordminimumLength = 12;
        private const int PasswordmaximunLength = 128;
        private const string NicknameInvalidErrorKey = "CreateAccount_NicknameFormatError";
        private const string EmailInvalidErrorKey = "CreateAccount_EmailFormatError";
        private const string PasswordInvalidErrorKey = "CreateAccount_PasswordPolicyError";
        private const string TermsNotAcceptedErrorKey = "CreateAccount_TermsError";
        private static readonly Regex _nicknameRegex = new Regex(@"^[a-zA-Z0-9_]+\z", RegexOptions.Compiled);
        private static readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+\z", RegexOptions.Compiled);

        /// <summary>Valida todos los datos del registro.</summary>
        /// <param name="playerAccountRegistration">Datos recibidos del cliente.</param>
        /// <returns>La clave del primer error encontrado; null si los datos son válidos.</returns>
        public static string ValidateAccountPlayerRegistration(PlayerAccountRegistration playerAccountRegistration)
        {
            string errorKey = ValidateNickname(playerAccountRegistration.Nickname);

            if (errorKey == null)
            {
                errorKey = ValidateEmail(playerAccountRegistration.Email);
            }

            if (errorKey == null)
            {
                errorKey = ValidatePassword(playerAccountRegistration.Password);
            }

            if (errorKey == null && !playerAccountRegistration.HasAcceptedTerms)
            {
                errorKey = TermsNotAcceptedErrorKey;
            }

            return errorKey;
        }

        private static string ValidateNickname(string nickname)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(nickname, NicknameminimumLength, NicknamemaximunLength);
            bool isFormatValid = isLengthValid && _nicknameRegex.IsMatch(nickname);

            if (!isFormatValid)
            {
                errorKey = NicknameInvalidErrorKey;
            }

            return errorKey;
        }

        /// <summary>Valida la longitud y el formato de un correo.</summary>
        /// <param name="email">Correo recibido del cliente.</param>
        /// <returns>La clave del error encontrado; null si el correo es válido.</returns>
        public static string ValidateEmail(string email)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(email, EmailminimumLength, EmailmaximunLength);
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
            bool isLengthValid = IsLengthInRange(password, PasswordminimumLength, PasswordmaximunLength);
            bool isStrong = isLengthValid && HasRequiredCharacterTypes(password);

            if (!isStrong)
            {
                errorKey = PasswordInvalidErrorKey;
            }

            return errorKey;
        }

        private static bool IsLengthInRange(string text, int minimumLength, int maximunLength)
        {
            bool isInRange = false;

            if (text != null)
            {
                isInRange = text.Length >= minimumLength && text.Length <= maximunLength;
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