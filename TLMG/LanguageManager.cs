using System;
using System.Windows;

namespace TLMG
{
    public static class LanguageManager
    {
        public static string CurrentCulture { get; private set; } = "es-MX";

        public static void ChangeLanguage(string cultureCode)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Languages/Strings.{cultureCode}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
            CurrentCulture = cultureCode;
        }
    }
}