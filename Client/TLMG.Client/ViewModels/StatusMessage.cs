using System;
using System.Windows.Threading;

namespace TLMG.Client.ViewModels
{
    public class StatusMessage : ViewModelBase
    {
        private const int DefaultDurationMilliseconds = 4000;

        private readonly DispatcherTimer _hideTimer;
        private string _text;
        private bool _isError;
        private bool _isVisible;

        public StatusMessage()
        {
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DefaultDurationMilliseconds)
            };
            _hideTimer.Tick += OnHideTimerTick;
        }

        public string Text
        {
            get { return _text; }
            private set { SetProperty(ref _text, value); }
        }

        public bool IsError
        {
            get { return _isError; }
            private set { SetProperty(ref _isError, value); }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            private set { SetProperty(ref _isVisible, value); }
        }

        public void ShowError(string messageText)
        {
            Show(messageText, true);
        }

        public void ShowSuccess(string messageText)
        {
            Show(messageText, false);
        }

        public void ShowPersistentError(string messageText)
        {
            _hideTimer.Stop();
            Text = messageText;
            IsError = true;
            IsVisible = true;
        }

        public void Hide()
        {
            _hideTimer.Stop();
            IsVisible = false;
        }

        protected override void OnDisposed()
        {
            _hideTimer.Stop();
            _hideTimer.Tick -= OnHideTimerTick;

            base.OnDisposed();
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