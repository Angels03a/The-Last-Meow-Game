using System;
using System.Globalization;
using System.Threading;

namespace TLMG
{
    public static class LanguageManager
    {
        private const string DefaultCultureCode = "es-MX";

        public static string CurrentCulture { get; private set; } = DefaultCultureCode;

        // Evento para notificar al resto de la aplicación cuando cambia el idioma
        public static event EventHandler LanguageChanged;

        public static void InitializeLanguage()
        {
            string systemCulture = CultureInfo.CurrentUICulture.Name;
            string normalizedCulture = systemCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                ? "en-US"
                : DefaultCultureCode;

            ChangeLanguage(normalizedCulture);
        }

        public static void ChangeLanguage(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return;
            }

            if (string.Equals(CurrentCulture, cultureCode, StringComparison.OrdinalIgnoreCase) && Thread.CurrentThread.CurrentCulture.Name == cultureCode)
            {
                return;
            }

            CultureInfo targetCulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = targetCulture;
            Thread.CurrentThread.CurrentUICulture = targetCulture;
            CurrentCulture = cultureCode;

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}