using System;
using System.Globalization;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Authorization
{
    public static class GuestIdentity
    {
        public const int MinimumGuestNumber = 1;

        public const int MaximumGuestNumber = 9999;

        private const int NoGuestNumber = 0;
        private const int GuestUserIdSign = -1;

        private const string NicknamePrefix = "Jugador ";

        private const string EnglishNicknamePrefix = "Player ";

        public static int CreateUserId(int guestNumber)
        {
            int userId = guestNumber * GuestUserIdSign;

            return userId;
        }

        public static string CreateNickname(int guestNumber)
        {
            string nickname = NicknamePrefix + guestNumber.ToString(CultureInfo.InvariantCulture);

            return nickname;
        }

        public static bool IsGuestUserId(int userId)
        {
            bool isGuest = userId < ValidationRules.NoUserId;

            return isGuest;
        }

        public static bool TryParseGuestNumber(string nickname, out int guestNumber)
        {
            string numberText = RemoveGuestPrefix(nickname);
            int parsedNumber = NoGuestNumber;
            bool isGuestNickname = numberText != null
                && int.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out parsedNumber)
                && parsedNumber >= MinimumGuestNumber
                && parsedNumber <= MaximumGuestNumber;

            guestNumber = NoGuestNumber;

            if (isGuestNickname)
            {
                guestNumber = parsedNumber;
            }

            return isGuestNickname;
        }

        private static string RemoveGuestPrefix(string nickname)
        {
            string numberText = null;

            if (nickname != null && nickname.StartsWith(NicknamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                numberText = nickname.Substring(NicknamePrefix.Length);
            }
            else if (nickname != null
                && nickname.StartsWith(EnglishNicknamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                numberText = nickname.Substring(EnglishNicknamePrefix.Length);
            }

            return numberText;
        }
    }
}