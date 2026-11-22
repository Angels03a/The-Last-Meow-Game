using TLMG.Domain.Constants;
using TLMG.Domain.Entities;

namespace TLMG.Domain.Validators
{
    public static class PlayerProfileValidator
    {
        public static string ValidateProfileUpdate(PlayerProfileUpdate profileUpdate)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (profileUpdate != null)
            {
                errorKey = ValidateNickname(profileUpdate.Nickname);

                if (errorKey == string.Empty)
                {
                    errorKey = ValidateAvatar(profileUpdate.AvatarKey);
                }
            }

            return errorKey;
        }

        private static string ValidateNickname(string nickname)
        {
            string errorKey = string.Empty;

            if (!AccountFieldValidator.IsNicknameValid(nickname))
            {
                errorKey = ErrorKeys.Profile.NicknameFormat;
            }

            return errorKey;
        }

        private static string ValidateAvatar(string avatarKey)
        {
            string errorKey = string.Empty;

            if (!AvatarCatalog.IsValidKey(avatarKey))
            {
                errorKey = ErrorKeys.Profile.AvatarNotValid;
            }

            return errorKey;
        }
    }
}
