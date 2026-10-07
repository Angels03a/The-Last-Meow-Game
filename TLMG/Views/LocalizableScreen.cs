using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        protected async void ShowStatusMessage(
            TextBlock messageElement,
            string messageContent,
            bool isErrorMessage = true)
        {
            messageElement.Text = messageContent;
            messageElement.Foreground = isErrorMessage
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            messageElement.Visibility = Visibility.Visible;

            await Task.Delay(ValidationHelper.StatusMessageDurationMilliseconds);

            if (messageElement.Text == messageContent)
            {
                messageElement.Visibility = Visibility.Collapsed;
            }
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