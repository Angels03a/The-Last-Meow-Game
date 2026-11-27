using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    public static class FriendNicknameValidator
    {
        private const char EmailMarker = '@';

        public static bool TryNormalizeNickname(string nickname, out string normalizedNickname)
        {
            normalizedNickname = string.Empty;
            bool isValid = false;

            if (nickname != null)
            {
                string trimmedNickname = nickname.Trim();
                isValid = trimmedNickname.Length >= ValidationRules.NicknameMinimumLength
                    && trimmedNickname.Length <= ValidationRules.NicknameMaximumLength
                    && trimmedNickname.IndexOf(EmailMarker) == ValidationRules.InvalidCharacterIndex;

                if (isValid)
                {
                    normalizedNickname = trimmedNickname;
                }
            }

            return isValid;
        }
    }
}
