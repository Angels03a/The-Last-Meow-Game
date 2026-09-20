using System;
using System.Windows;

namespace TLMG
{
    public static class LanguageManager
    {
        public static string CurrentCulture { get; private set; } = "es-MX";

        public static void ChangeLanguage(string cultureCode)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary oldDict = null;

            foreach (var dict in dictionaries)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("Strings."))
                {
                    oldDict = dict;
                    break;
                }
            }

            if (oldDict != null)
            {
                dictionaries.Remove(oldDict);
            }

            var newDict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/TLMG;component/Languages/Strings.{cultureCode}.xaml")
            };

            dictionaries.Add(newDict);
            CurrentCulture = cultureCode;
        }
    }
}