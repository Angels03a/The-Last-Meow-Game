using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TLMG.Views
{
    public partial class VerificationScreen : UserControl
    {
        private const string SimulatedCode = "123456"; // TODO: sustituir por el código real generado por el servidor cuando exista esa capa
        private const int MaxAttempts = 3;
        private readonly Action _onVerified;
        private readonly Action _onBack;
        private TextBox[] _digitBoxes;
        private Ellipse[] _attemptDots;
        private DispatcherTimer _timer;
        private int _secondsUntilExpiry = 300;
        private int _secondsUntilResend = 60;
        private int _failedAttempts = 0;

        public VerificationScreen(Action onVerified, Action onBack)
        {
            InitializeComponent();
            _onVerified = onVerified;
            _onBack = onBack;
            _digitBoxes = new[] { txtDigit1, txtDigit2, txtDigit3, txtDigit4, txtDigit5, txtDigit6 };
            _attemptDots = new[] { elpAttempt1, elpAttempt2, elpAttempt3 };

            foreach (var box in _digitBoxes)
            {
                box.TextChanged += DigitBox_TextChanged;
                box.PreviewKeyDown += DigitBox_PreviewKeyDown;
            }

            StartTimer();
        }

        private void DigitBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            lblError.Visibility = Visibility.Collapsed;
            int index = Array.IndexOf(_digitBoxes, box);

            if (box.Text.Length == 1 && index < _digitBoxes.Length - 1)
                _digitBoxes[index + 1].Focus();

            UpdateVerifyButtonState();
        }

        private void DigitBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var box = (TextBox)sender;
            int index = Array.IndexOf(_digitBoxes, box);

            if (e.Key == Key.Back && box.Text.Length == 0 && index > 0)
                _digitBoxes[index - 1].Focus();
        }

        private void UpdateVerifyButtonState()
        {
            bool allFilled = true;
            foreach (var box in _digitBoxes)
                if (string.IsNullOrEmpty(box.Text))
                {
                    allFilled = false;
                    break;
                }

            btnVerifyCode.IsEnabled = allFilled;
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            UpdateTimerTexts();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_secondsUntilExpiry > 0)
            {
                _secondsUntilExpiry--;
                if (_secondsUntilExpiry == 0)
                {
                    _timer.Stop();
                    ShowCodeExpired();
                    return;
                }
            }

            if (_secondsUntilResend > 0)
                _secondsUntilResend--;

            UpdateTimerTexts();
        }

        private void UpdateTimerTexts()
        {
            TimeSpan timeRemaining = TimeSpan.FromSeconds(_secondsUntilExpiry);
            lblExpiration.Text = string.Format("{0:00}:{1:00}", timeRemaining.Minutes, timeRemaining.Seconds);

            string buttonText = (string)Application.Current.Resources["Verification_ResendButton"];
            if (_secondsUntilResend > 0)
            {
                btnResendCode.IsEnabled = false;
                btnResendCode.Content = string.Format("{0} ({1:00}s)", buttonText, _secondsUntilResend);
            }
            else
            {
                btnResendCode.IsEnabled = true;
                btnResendCode.Content = buttonText;
            }
        }

        private void ShowCodeExpired()
        {
            lblError.Text = (string)Application.Current.Resources["Verification_ExpiredCodeError"];
            lblError.Visibility = Visibility.Visible;

            foreach (var box in _digitBoxes)
                box.IsEnabled = false;

            btnVerifyCode.IsEnabled = false;
        }

        private void UpdateAttemptDots()
        {
            for (int i = 0; i < _attemptDots.Length; i++)
            {
                _attemptDots[i].Fill = i < _failedAttempts
                    ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                    : new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x4A));
            }
        }

        private void BtnVerifyCode_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = "";
            foreach (var box in _digitBoxes)
                enteredCode += box.Text;

            if (enteredCode == SimulatedCode)
            {
                _timer.Stop();
                MessageBox.Show((string)Application.Current.Resources["Verification_SuccessMessage"]);
                _onVerified?.Invoke();
                return;
            }

            _failedAttempts++;
            UpdateAttemptDots();

            if (_failedAttempts >= MaxAttempts)
            {
                _timer.Stop();
                lblError.Text = (string)Application.Current.Resources["Verification_AttemptsExhaustedError"];

                foreach (var box in _digitBoxes)
                    box.IsEnabled = false;

                btnVerifyCode.IsEnabled = false;
            }
            else
            {
                lblError.Text = (string)Application.Current.Resources["Verification_IncorrectCodeError"];
            }

            lblError.Visibility = Visibility.Visible;

            foreach (var box in _digitBoxes)
                box.Text = "";

            _digitBoxes[0].Focus();
        }

        private void BtnResendCode_Click(object sender, RoutedEventArgs e)
        {
            _secondsUntilExpiry = 300;
            _secondsUntilResend = 60;
            _failedAttempts = 0;
            UpdateAttemptDots();

            lblError.Visibility = Visibility.Collapsed;

            foreach (var box in _digitBoxes)
            {
                box.IsEnabled = true;
                box.Text = "";
            }

            btnVerifyCode.IsEnabled = false;

            if (!_timer.IsEnabled)
                _timer.Start();

            _digitBoxes[0].Focus();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _onBack?.Invoke();
        }
    }
}