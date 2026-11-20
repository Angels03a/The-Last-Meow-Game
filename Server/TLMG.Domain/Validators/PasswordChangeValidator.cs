using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    public static class PasswordChangeValidator
    {
        public static string ValidateNewPassword(string newPassword)
        {
            string errorKey = null;

            if (PlayerAccountRegistrationValidator.ValidatePassword(newPassword) != null)
            {
                errorKey = ErrorKeys.Password.PolicyNotMet;
            }

            return errorKey;
        }
    }
}