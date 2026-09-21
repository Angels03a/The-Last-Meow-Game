using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace TLMG
{
    public static class LanguageManager
    {
        public static string CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture.Name;

        // Evento para notificar al resto de la app cuando cambia el idioma
        public static event EventHandler LanguageChanged;

        public static void ChangeLanguage(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode) ||
                string.Equals(CurrentCulture, cultureCode, StringComparison.OrdinalIgnoreCase))
                return;

            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CurrentCulture = cultureCode;

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}