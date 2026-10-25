using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace TLMG.Client
{
    /// <summary>
    /// Gestiona la cultura, localización y actualización dinámica de diccionarios de recursos de idioma.
    /// </summary>
    internal static class LanguageManager
    {
        private const string DefaultCultureCode = "es-MX";

        /// <summary>
        /// Código de la cultura en inglés (en-US).
        /// </summary>
        public const string EnglishCultureCode = "en-US";
        private const string EnglishCulturePrefix = "en";
        private const int NotFoundIndex = -1;
        private const int FirstIndex = 0;

        private static readonly Regex _languageDictionaryRegex = new Regex(
            @"Languages[/\\]Strings\.[a-zA-Z]{2}-[a-zA-Z]{2}\.xaml",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Evento notificado cuando se produce un cambio en el idioma de la aplicación.
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// Obtiene el código de cultura actualmente activo en la aplicación.
        /// </summary>
        public static string CurrentCulture { get; private set; } = DefaultCultureCode;

        /// <summary>
        /// Inicializa el idioma de la aplicación según la cultura de interfaz del sistema
        /// (inglés o español por defecto).
        /// </summary>
        public static void InitializeLanguage()
        {
            string systemCulture = CultureInfo.CurrentUICulture.Name;
            bool isEnglishSystemCulture = systemCulture.StartsWith(
                EnglishCulturePrefix,
                StringComparison.OrdinalIgnoreCase);

            string normalizedCulture = isEnglishSystemCulture ? EnglishCultureCode : DefaultCultureCode;

            ApplyCulture(normalizedCulture);
            UpdateResourceDictionary(normalizedCulture);
        }

        /// <summary>
        /// Cambia el idioma activo de la aplicación aplicando la nueva cultura
        /// y actualizando los diccionarios de recursos.
        /// </summary>
        /// <param name="cultureCode">Código de cultura a aplicar (por ejemplo, "es-MX" o "en-US").</param>
        public static void ChangeLanguage(string cultureCode)
        {
            bool isCultureCodeProvided = !string.IsNullOrWhiteSpace(cultureCode);
            bool isDifferentFromCurrent =
                !string.Equals(CurrentCulture, cultureCode, StringComparison.OrdinalIgnoreCase);

            if (isCultureCodeProvided && isDifferentFromCurrent)
            {
                ApplyCulture(cultureCode);
                UpdateResourceDictionary(cultureCode);
                RefreshActiveWindows();
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static void ApplyCulture(string cultureCode)
        {
            CultureInfo targetCulture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = targetCulture;
            CultureInfo.CurrentUICulture = targetCulture;
            CurrentCulture = cultureCode;
        }

        private static void UpdateResourceDictionary(string cultureCode)
        {
            if (Application.Current != null)
            {
                Collection<ResourceDictionary> mergedDictionaries = Application.Current.Resources.MergedDictionaries;
                ResourceDictionary newDictionary = new ResourceDictionary
                {
                    Source = new Uri(
                        $"pack://application:,,,/TLMG.Client;component/Languages/Strings.{cultureCode}.xaml")
                };

                int targetIndex = NotFoundIndex;
                for (int dictionaryIndex = FirstIndex; dictionaryIndex < mergedDictionaries.Count; dictionaryIndex++)
                {
                    ResourceDictionary currentDictionary = mergedDictionaries[dictionaryIndex];
                    if (currentDictionary.Source != null &&
                        _languageDictionaryRegex.IsMatch(currentDictionary.Source.OriginalString))
                    {
                        targetIndex = dictionaryIndex;
                        break;
                    }
                }

                if (targetIndex != NotFoundIndex)
                {
                    mergedDictionaries[targetIndex] = newDictionary;
                }
                else
                {
                    mergedDictionaries.Add(newDictionary);
                }
            }
        }

        private static void RefreshActiveWindows()
        {
            if (Application.Current != null)
            {
                foreach (Window activeWindow in Application.Current.Windows)
                {
                    object currentContent = activeWindow.Content;
                    activeWindow.Content = null;
                    activeWindow.Content = currentContent;
                }
            }
        }
    }
}