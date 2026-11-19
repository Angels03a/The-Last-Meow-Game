using TLMG.Domain.Constants;
using TLMG.Domain.Entities;

namespace TLMG.Domain.Validators
{
    public static class PlayerLogInValidator
    {
        public static string ValidateLogIn(PlayerLogin playerLogIn)
        {
            string errorKey = null;

            if (playerLogIn == null
                || string.IsNullOrWhiteSpace(playerLogIn.Email)
                || string.IsNullOrWhiteSpace(playerLogIn.Password))
            {
                errorKey = ErrorKeys.Login.IncompleteFields;
            }
            else if (!AccountFieldValidator.IsEmailValid(playerLogIn.Email))
            {
                errorKey = ErrorKeys.Login.EmailFormat;
            }

            return errorKey;
        }
    }
}