using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace TLMG
{
    /// <summary>
    /// Gestiona la cultura, localización y actualización dinámica de diccionarios de recursos de idioma.
    /// </summary>
    public static class LanguageManager
    {
        private const string DefaultCultureCode = "es-MX";
        private const string EnglishCulturePrefix = "en";
        private const string EnglishCultureCode = "en-US";
        private const int NotFoundIndex = -1;
        private const int FirstIndex = 0;

        private static readonly Regex _languageDictionaryRegex = new Regex(
            @"Languages[/\\]Strings\.[a-zA-Z]{2}-[a-zA-Z]{2}\.xaml",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Obtiene el código de cultura del idioma actualmente activo en la aplicación.
        /// </summary>
        public static string CurrentCulture { get; private set; } = DefaultCultureCode;

        /// <summary>
        /// Evento que se desencadena cuando el idioma de la aplicación ha cambiado.
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// Inicializa el idioma de la aplicación según la cultura de la interfaz de usuario del sistema.
        /// </summary>
        public static void InitializeLanguage()
        {
            string systemCulture = CultureInfo.CurrentUICulture.Name;
            bool isEnglishSystemCulture = systemCulture.StartsWith(
                EnglishCulturePrefix,
                StringComparison.OrdinalIgnoreCase);
            string normalizedCulture = isEnglishSystemCulture
                ? EnglishCultureCode
                : DefaultCultureCode;

            // Establece la cultura en el gestor y en los hilos de ejecución.
            ApplyCulture(normalizedCulture);

            // Carga el diccionario inicial directamente para asegurar la configuración base.
            UpdateResourceDictionary(normalizedCulture);
        }

        /// <summary>
        /// Cambia el idioma actual de la aplicación, actualizando la cultura del hilo,
        /// el diccionario de recursos fusionado y repintando las ventanas activas.
        /// </summary>
        /// <param name="cultureCode">Código de cultura objetivo (por ejemplo, "es-MX" o "en-US").</param>
        public static void ChangeLanguage(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return;
            }

            if (string.Equals(CurrentCulture, cultureCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Actualiza la cultura de los hilos de ejecución actuales.
            ApplyCulture(cultureCode);

            // Actualiza el diccionario de recursos en los recursos globales de la aplicación.
            UpdateResourceDictionary(cultureCode);

            // Fuerza el refresco visual en todas las ventanas activas.
            RefreshActiveWindows();

            // Notifica a los observadores externos sobre el cambio de idioma.
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void ApplyCulture(string cultureCode)
        {
            CultureInfo targetCulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = targetCulture;
            Thread.CurrentThread.CurrentUICulture = targetCulture;
            CurrentCulture = cultureCode;
        }

        private static void UpdateResourceDictionary(string cultureCode)
        {
            if (Application.Current == null)
            {
                return;
            }

            Collection<ResourceDictionary> mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary newDictionary = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/TLMG;component/Languages/Strings.{cultureCode}.xaml")
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

        private static void RefreshActiveWindows()
        {
            if (Application.Current == null)
            {
                return;
            }

            foreach (Window activeWindow in Application.Current.Windows)
            {
                object currentContent = activeWindow.Content;
                activeWindow.Content = null;
                activeWindow.Content = currentContent;
            }
        }
    }
}