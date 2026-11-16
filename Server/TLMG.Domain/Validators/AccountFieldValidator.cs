using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    public static class AccountFieldValidator
    {
        public static bool IsEmailValid(string email)
        {
            bool isLengthValid = IsLengthInRange(
                email,
                ValidationRules.EmailMinimumLength,
                ValidationRules.EmailMaximumLength);
            bool isEmailValid = isLengthValid && ValidationRules.EmailRegex.IsMatch(email);

            return isEmailValid;
        }

        public static bool IsLengthInRange(string text, int minimumLength, int maximumLength)
        {
            bool isInRange = false;

            if (text != null)
            {
                isInRange = text.Length >= minimumLength && text.Length <= maximumLength;
            }

            return isInRange;
        }

        public static string NormalizeEmail(string email)
        {
            string normalizedEmail = email;

            if (email != null)
            {
                normalizedEmail = email.Trim().ToLowerInvariant();
            }

            return normalizedEmail;
        }
    }
}