using System.Linq;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;

namespace TLMG.Domain.Validators
{
    /// <summary>
    /// Validaciones del servidor para los datos de registro; el cliente no es confiable.
    /// </summary>
    public static class PlayerAccountRegistrationValidator
    {
        /// <summary>Valida todos los datos del registro.</summary>
        /// <param name="playerRegistration">Objeto con los datos capturados.</param>
        /// <returns>La clave del primer error encontrado; null si los datos son válidos.</returns>
        public static string ValidateAccountPlayerRegistration(PlayerRegistration playerRegistration)
        {
            string errorKey = ValidateNickname(playerRegistration.Nickname);

            if (errorKey == null)
            {
                errorKey = ValidateEmail(playerRegistration.Email);
            }

            if (errorKey == null)
            {
                errorKey = ValidatePassword(playerRegistration.Password);
            }

            if (errorKey == null && !playerRegistration.HasAcceptedTerms)
            {
                errorKey = ErrorKeys.CreateAccount.Terms;
            }

            return errorKey;
        }

        private static string ValidateNickname(string nickname)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(nickname, 
                ValidationRules.NicknameMinimumLength, ValidationRules.NicknameMaximumLength);
            bool isFormatValid = isLengthValid && ValidationRules.NicknameRegex.IsMatch(nickname);

            if (!isFormatValid)
            {
                errorKey = ErrorKeys.CreateAccount.NicknameFormat;
            }

            return errorKey;
        }

        /// <summary>Valida la longitud y el formato de un correo.</summary>
        /// <param name="email">Correo recibido del cliente.</param>
        /// <returns>La clave del error encontrado; null si el correo es válido.</returns>
        public static string ValidateEmail(string email)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(email, ValidationRules.EmailMinimumLength, 
                ValidationRules.EmailMaximumLength);
            bool isFormatValid = isLengthValid && ValidationRules.EmailRegex.IsMatch(email);

            if (!isFormatValid)
            {
                errorKey = ErrorKeys.CreateAccount.EmailFormat;
            }

            return errorKey;
        }

        private static string ValidatePassword(string password)
        {
            string errorKey = null;
            bool isLengthValid = IsLengthInRange(password, ValidationRules.PasswordMinimumLength, 
                ValidationRules.PasswordMaximumLength);
            bool isStrong = isLengthValid && HasRequiredCharacterTypes(password);

            if (!isStrong)
            {
                errorKey = ErrorKeys.CreateAccount.PasswordPolicy;
            }

            return errorKey;
        }

        private static bool IsLengthInRange(string text, int minimumLength, int maximumLength)
        {
            bool isInRange = false;

            if (text != null)
            {
                isInRange = text.Length >= minimumLength && text.Length <= maximumLength;
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