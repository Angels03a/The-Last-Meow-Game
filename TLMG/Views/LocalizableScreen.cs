using System;
using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla base que gestiona la suscripción al cambio de idioma y el refresco de textos dinámicos.
    /// </summary>
    public abstract class LocalizableScreen : UserControl
    {
        protected LocalizableScreen()
        {
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }
        protected virtual void UpdateDynamicLabels()
        {
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateDynamicLabels();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            UpdateDynamicLabels();
        }
    }
}