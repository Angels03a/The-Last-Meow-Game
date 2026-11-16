using TLMG.Domain.Constants;
using TLMG.Domain.Entities;

namespace TLMG.Domain.Validators
{
    public static class PlayerAccountRegistrationValidator
    {
        public static string ValidateAccountPlayerRegistration(PlayerRegistration playerRegistration)
        {
            string errorKey = ErrorKeys.CreateAccount.IncompleteFields;

            if (playerRegistration != null)
            {
                errorKey = ValidateNickname(playerRegistration.Nickname);

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
            }

            return errorKey;
        }

        private static string ValidateNickname(string nickname)
        {
            string errorKey = null;
            bool isLengthValid = AccountFieldValidator.IsLengthInRange(
                nickname,
                ValidationRules.NicknameMinimumLength,
                ValidationRules.NicknameMaximumLength);
            bool isFormatValid = isLengthValid && ValidationRules.NicknameRegex.IsMatch(nickname);

            if (!isFormatValid)
            {
                errorKey = ErrorKeys.CreateAccount.NicknameFormat;
            }

            return errorKey;
        }

        private static string ValidateEmail(string email)
        {
            string errorKey = null;

            if (!AccountFieldValidator.IsEmailValid(email))
            {
                errorKey = ErrorKeys.CreateAccount.EmailFormat;
            }

            return errorKey;
        }

        private static string ValidatePassword(string password)
        {
            string errorKey = null;
            bool isLengthValid = AccountFieldValidator.IsLengthInRange(
                password,
                ValidationRules.PasswordMinimumLength,
                ValidationRules.PasswordMaximumLength);
            bool isStrong = isLengthValid && HasRequiredCharacterTypes(password);

            if (!isStrong)
            {
                errorKey = ErrorKeys.CreateAccount.PasswordPolicy;
            }

            return errorKey;
        }

        private static bool HasRequiredCharacterTypes(string password)
        {
            bool hasUppercase = false;
            bool hasLowercase = false;
            bool hasDigit = false;
            bool hasSpecialCharacter = false;

            foreach (char character in password)
            {
                if (char.IsUpper(character))
                {
                    hasUppercase = true;
                }
                else if (char.IsLower(character))
                {
                    hasLowercase = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }
                else if (!char.IsLetterOrDigit(character))
                {
                    hasSpecialCharacter = true;
                }
            }

            bool hasAllTypes = hasUppercase && hasLowercase && hasDigit && hasSpecialCharacter;

            return hasAllTypes;
        }
    }
}