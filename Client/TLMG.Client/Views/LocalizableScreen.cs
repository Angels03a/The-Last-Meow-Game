using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla base que gestiona la suscripción al cambio de idioma y el refresco de textos dinámicos.
    /// </summary>
    public abstract class LocalizableScreen : UserControl
    {
        private readonly Dictionary<TextBlock, DispatcherTimer> _statusMessageTimers =
            new Dictionary<TextBlock, DispatcherTimer>();

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LocalizableScreen"/>.
        /// </summary>
        protected LocalizableScreen()
        {
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        /// <summary>
        /// Permite a las pantallas derivadas actualizar textos dinámicos al cargarse
        /// la vista o al cambiar el idioma de la aplicación.
        /// </summary>
        protected virtual void UpdateDynamicLabels()
        {
        }

        /// <summary>
        /// Muestra un mensaje temporal de estado en la interfaz de usuario.
        /// </summary>
        /// <param name="messageElement">Elemento de texto donde se desplegará el mensaje.</param>
        /// <param name="messageContent">Contenido textual del mensaje.</param>
        /// <param name="isErrorMessage">Indica si el mensaje representa un error o una confirmación exitosa.</param>
        protected void ShowStatusMessage(
            TextBlock messageElement,
            string messageContent,
            bool isErrorMessage = true)
        {
            messageElement.Text = messageContent;
            messageElement.Foreground = isErrorMessage
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            messageElement.Visibility = Visibility.Visible;

            if (!_statusMessageTimers.TryGetValue(messageElement, out DispatcherTimer statusTimer))
            {
                statusTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(ValidationHelper.StatusMessageDurationMilliseconds),
                    Tag = messageElement
                };
                statusTimer.Tick += OnStatusMessageTimerTick;
                _statusMessageTimers.Add(messageElement, statusTimer);
            }
            else
            {
                statusTimer.Stop();
            }

            statusTimer.Start();
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

            foreach (DispatcherTimer statusTimer in _statusMessageTimers.Values)
            {
                statusTimer.Stop();
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            UpdateDynamicLabels();
        }

        private void OnStatusMessageTimerTick(object sender, EventArgs e)
        {
            DispatcherTimer statusTimer = (DispatcherTimer)sender;
            statusTimer.Stop();

            TextBlock messageElement = (TextBlock)statusTimer.Tag;
            messageElement.Visibility = Visibility.Collapsed;
        }
    }
}