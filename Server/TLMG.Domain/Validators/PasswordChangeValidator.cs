using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    public static class PasswordChangeValidator
    {
        public static string ValidateNewPassword(string newPassword)
        {
            string errorKey = string.Empty;

            if (PlayerAccountRegistrationValidator.ValidatePassword(newPassword) != string.Empty)
            {
                errorKey = ErrorKeys.Password.PolicyNotMet;
            }

            return errorKey;
        }
    }
}
