using System;
using TLMG.Client.Services;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Mensaje temporal de estado (error o éxito) que se oculta solo.
    /// </summary>
    public class StatusMessage : ViewModelBase
    {
        private readonly ITimer _hideTimer;
        private string _text;
        private bool _isError;
        private bool _isVisible;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="StatusMessage"/>.
        /// </summary>
        /// <param name="timerFactory">Fábrica de temporizadores.</param>
        public StatusMessage(ITimerFactory timerFactory)
        {
            _hideTimer = timerFactory.Create(
                TimeSpan.FromMilliseconds(ValidationHelper.StatusMessageDurationMilliseconds));
            _hideTimer.Tick += OnHideTimerTick;
        }

        /// <summary>
        /// Texto ya traducido del mensaje.
        /// </summary>
        public string Text
        {
            get { return _text; }
            private set { SetProperty(ref _text, value); }
        }

        /// <summary>
        /// Indica si el mensaje es un error; si no, es de éxito.
        /// </summary>
        public bool IsError
        {
            get { return _isError; }
            private set { SetProperty(ref _isError, value); }
        }

        /// <summary>
        /// Indica si el mensaje está visible.
        /// </summary>
        public bool IsVisible
        {
            get { return _isVisible; }
            private set { SetProperty(ref _isVisible, value); }
        }

        /// <summary>
        /// Muestra un mensaje de error.
        /// </summary>
        /// <param name="messageText">Texto traducido.</param>
        public void ShowError(string messageText)
        {
            Show(messageText, true);
        }

        /// <summary>
        /// Muestra un mensaje de éxito.
        /// </summary>
        /// <param name="messageText">Texto traducido.</param>
        public void ShowSuccess(string messageText)
        {
            Show(messageText, false);
        }

        /// <summary>
        /// Oculta el mensaje y detiene su temporizador.
        /// </summary>
        public void Hide()
        {
            _hideTimer.Stop();
            IsVisible = false;
        }

        protected override void OnDisposed()
        {
            _hideTimer.Stop();
            _hideTimer.Tick -= OnHideTimerTick;
        }

        private void Show(string messageText, bool isError)
        {
            Text = messageText;
            IsError = isError;
            IsVisible = true;
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void OnHideTimerTick(object sender, EventArgs e)
        {
            Hide();
        }
    }
}