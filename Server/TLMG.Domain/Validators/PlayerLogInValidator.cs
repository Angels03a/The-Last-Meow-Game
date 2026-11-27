using TLMG.Domain.Constants;
using TLMG.Domain.Entities;

namespace TLMG.Domain.Validators
{
    public static class PlayerLogInValidator
    {
        public static bool TryValidateLogIn(PlayerLogin playerLogIn, out string errorKey)
        {
            bool isValid = false;
            errorKey = ErrorKeys.Login.IncompleteFields;

            if (playerLogIn != null
                && !string.IsNullOrWhiteSpace(playerLogIn.Email)
                && !string.IsNullOrWhiteSpace(playerLogIn.Password))
            {
                if (AccountFieldValidator.IsEmailValid(playerLogIn.Email))
                {
                    isValid = true;
                    errorKey = string.Empty;
                }
                else
                {
                    errorKey = ErrorKeys.Login.EmailFormat;
                }
            }

            return isValid;
        }
    }
}
