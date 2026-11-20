using System;

namespace TLMG.Client.Models
{
    /// <summary>Display data for a notification authorized by the server.</summary>
    public sealed class GameNotification
    {
        public string TitleKey { get; set; }
        public string BodyKey { get; set; }
        public string Subject { get; set; }
        public string Icon { get; set; }
        public bool IsPersonal { get; set; }
        public bool IsSuccess { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>Builds notifications for display. The server decides game rules and network recipients.</summary>
    public static class GameNotificationFactory
    {
        public static GameNotification CreatePublic(
            string titleKey, string bodyKey, string subject, string icon, bool isSuccess)
        {
            return Create(titleKey, bodyKey, subject, icon, isSuccess, false);
        }

        public static GameNotification CreatePersonal(
            string titleKey, string bodyKey, string subject, string icon, bool isSuccess)
        {
            return Create(titleKey, bodyKey, subject, icon, isSuccess, true);
        }

        private static GameNotification Create(
            string titleKey, string bodyKey, string subject, string icon, bool isSuccess, bool isPersonal)
        {
            return new GameNotification
            {
                TitleKey = titleKey,
                BodyKey = bodyKey,
                Subject = subject,
                Icon = icon,
                IsSuccess = isSuccess,
                IsPersonal = isPersonal,
                Duration = TimeSpan.FromSeconds(5)
            };
        }
    }
}
