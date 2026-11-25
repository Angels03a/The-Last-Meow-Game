namespace TLMG.Client
{
    internal static class DisplayNameHelper
    {
        private const int NoUserId = 0;
        private const string GuestNicknameFormatKey = "Common_GuestNicknameFormat";

        internal static string Resolve(int userId, string nickname)
        {
            string displayName = nickname;

            if (userId < NoUserId)
            {
                string guestFormat = LanguageManager.FindText(GuestNicknameFormatKey);
                displayName = string.Format(guestFormat, -userId);
            }

            return displayName;
        }
    }
}