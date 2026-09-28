using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de verificación mediante código numérico de 6 dígitos enviado por correo.
    /// </summary>
    public partial class VerificationScreen : UserControl
    {
        public const int MaximumVerificationAttempts = 3;
        public const int VerificationCodeExpirySeconds = 300;
        public const int CodeResendCooldownSeconds = 60;
        private const string SimulatedVerificationCode = "123456";
        private const int CountdownTimerIntervalSeconds = 1;
        private const int EmptyTextLength = 0;
        private const int SingleDigitLength = 1;
        private const int ArrayOffset = 1;
        private const int TimeExpired = 0;
        private const int InitialFailedAttempts = 0;
        private const int FirstTextBoxIndex = 0;
        private const string TimerFormat = "{0:00}:{1:00}";
        private const string ResendButtonFormat = "{0} ({1:00}s)";
        private readonly Action _onVerifiedCallbackAction;
        private readonly Action _onBackCallbackAction;
        private TextBox[] _verificationDigitTextBoxes;
        private Ellipse[] _attemptIndicatorEllipses;
        private DispatcherTimer _countdownDispatcherTimer;
        private int _remainingSecondsUntilExpiry = VerificationCodeExpirySeconds;
        private int _remainingSecondsUntilResend = CodeResendCooldownSeconds;
        private int _totalFailedAttempts = InitialFailedAttempts;

        public VerificationScreen(
            Action onVerifiedCallbackAction,
            Action onBackCallbackAction)
        {
            InitializeComponent();
            _onVerifiedCallbackAction = onVerifiedCallbackAction;
            _onBackCallbackAction = onBackCallbackAction;
            _verificationDigitTextBoxes = new[] { txtDigit1, txtDigit2, txtDigit3, txtDigit4, txtDigit5, txtDigit6 };
            _attemptIndicatorEllipses = new[] { ellAttempt1, ellAttempt2, ellAttempt3 };

            foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
            {
                currentDigitTextBox.TextChanged += DigitBox_TextChanged;
                currentDigitTextBox.PreviewKeyDown += DigitBox_PreviewKeyDown;
            }

            StartCountdownTimer();
        }

        private void DigitBox_TextChanged(object eventSender, TextChangedEventArgs eventArguments)
        {
            TextBox currentDigitTextBox = (TextBox)eventSender;
            lblFormMessage.Visibility = Visibility.Hidden;
            int currentTextBoxIndex = Array.IndexOf(_verificationDigitTextBoxes, currentDigitTextBox);

            bool isBoxFilled = currentDigitTextBox.Text.Length == SingleDigitLength;
            bool hasNextBox = currentTextBoxIndex < _verificationDigitTextBoxes.Length - ArrayOffset;

            if (isBoxFilled && hasNextBox)
            {
                _verificationDigitTextBoxes[currentTextBoxIndex + ArrayOffset].Focus();
            }

            UpdateVerifyButtonState();
        }

        private void DigitBox_PreviewKeyDown(object eventSender, KeyEventArgs eventArguments)
        {
            TextBox currentDigitTextBox = (TextBox)eventSender;
            int currentTextBoxIndex = Array.IndexOf(_verificationDigitTextBoxes, currentDigitTextBox);

            bool isBackspaceOnEmptyBox = eventArguments.Key == Key.Back && currentDigitTextBox.Text.Length == EmptyTextLength;
            bool hasPreviousTextBox = currentTextBoxIndex > FirstTextBoxIndex;

            if (isBackspaceOnEmptyBox && hasPreviousTextBox)
            {
                _verificationDigitTextBoxes[currentTextBoxIndex - ArrayOffset].Focus();
            }
        }

        private void UpdateVerifyButtonState()
        {
            bool areAllDigitsProvided = true;
            foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
            {
                if (string.IsNullOrEmpty(currentDigitTextBox.Text))
                {
                    areAllDigitsProvided = false;
                    break;
                }
            }

            btnVerifyCode.IsEnabled = areAllDigitsProvided;
        }

        private void StartCountdownTimer()
        {
            _countdownDispatcherTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(CountdownTimerIntervalSeconds)
            };
            _countdownDispatcherTimer.Tick += Timer_Tick;
            _countdownDispatcherTimer.Start();
            UpdateTimerInterfaceTexts();
        }

        private void Timer_Tick(object eventSender, EventArgs eventArguments)
        {
            if (_remainingSecondsUntilExpiry > TimeExpired)
            {
                _remainingSecondsUntilExpiry--;
            }

            if (_remainingSecondsUntilResend > TimeExpired)
            {
                _remainingSecondsUntilResend--;
            }

            UpdateTimerInterfaceTexts();

            if (_remainingSecondsUntilExpiry == TimeExpired)
            {
                _countdownDispatcherTimer.Stop();
                ShowCodeExpiredStatus();
            }
        }

        private void UpdateTimerInterfaceTexts()
        {
            TimeSpan currentRemainingTimeSpan = TimeSpan.FromSeconds(_remainingSecondsUntilExpiry);
            lblExpiration.Text = string.Format(
                TimerFormat,
                currentRemainingTimeSpan.Minutes,
                currentRemainingTimeSpan.Seconds);

            string originalButtonText = TryFindResource("Verification_ResendButton") as string;

            if (_remainingSecondsUntilResend > TimeExpired)
            {
                btnResendCode.IsEnabled = false;
                btnResendCode.Content = string.Format(
                    ResendButtonFormat,
                    originalButtonText,
                    _remainingSecondsUntilResend);
            }
            else
            {
                btnResendCode.IsEnabled = true;
                btnResendCode.Content = originalButtonText;
            }
        }

        private void ShowCodeExpiredStatus()
        {
            lblFormMessage.Text = TryFindResource("Verification_ExpiredCodeError") as string;
            lblFormMessage.Foreground = (Brush)FindResource("Brush_Error");
            lblFormMessage.Visibility = Visibility.Visible;

            foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
            {
                currentDigitTextBox.IsEnabled = false;
            }

            btnVerifyCode.IsEnabled = false;
        }

        private void UpdateAttemptIndicatorDots()
        {
            for (int currentEllipseIndex = 0; currentEllipseIndex < _attemptIndicatorEllipses.Length; currentEllipseIndex++)
            {
                _attemptIndicatorEllipses[currentEllipseIndex].Fill = currentEllipseIndex < _totalFailedAttempts
                    ? (Brush)FindResource("Brush_Error")
                    : (Brush)FindResource("Brush_Border");
            }
        }

        private async void ShowStatusMessage(TextBlock messageElement, string messageContent, bool isErrorMessage = true)
        {
            messageElement.Text = messageContent;
            messageElement.Foreground = isErrorMessage
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            messageElement.Visibility = Visibility.Visible;

            await Task.Delay(ValidationHelper.StatusMessageDurationMilliseconds);

            if (messageElement.Text == messageContent)
            {
                messageElement.Visibility = Visibility.Hidden;
            }
        }

        private void BtnVerifyCode_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            string concatenatedEnteredCode = string.Empty;
            foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
            {
                concatenatedEnteredCode += currentDigitTextBox.Text;
            }

            lblVerificationError.Visibility = Visibility.Hidden;

            if (concatenatedEnteredCode == SimulatedVerificationCode)
            {
                _countdownDispatcherTimer.Stop();
                ShowStatusMessage(lblFormMessage, TryFindResource("Verification_SuccessMessage") as string, false);
                _onVerifiedCallbackAction?.Invoke();
            }
            else
            {
                ProcessFailedVerificationAttempt();
            }
        }

        private void ProcessFailedVerificationAttempt()
        {
            _totalFailedAttempts++;
            UpdateAttemptIndicatorDots();

            if (_totalFailedAttempts >= MaximumVerificationAttempts)
            {
                _countdownDispatcherTimer.Stop();
                lblFormMessage.Text = TryFindResource("Verification_AttemptsExhaustedError") as string;
                lblFormMessage.Foreground = (Brush)FindResource("Brush_Error");
                lblFormMessage.Visibility = Visibility.Visible;

                foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
                {
                    currentDigitTextBox.IsEnabled = false;
                }

                btnVerifyCode.IsEnabled = false;
            }
            else
            {
                ShowStatusMessage(
                    lblVerificationError,
                    TryFindResource("Verification_IncorrectCodeError") as string,
                    true);
            }

            foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
            {
                currentDigitTextBox.Text = string.Empty;
            }

            _verificationDigitTextBoxes[FirstTextBoxIndex].Focus();
        }

        private void BtnResendCode_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            _remainingSecondsUntilExpiry = VerificationCodeExpirySeconds;
            _remainingSecondsUntilResend = CodeResendCooldownSeconds;
            _totalFailedAttempts = InitialFailedAttempts;
            UpdateAttemptIndicatorDots();

            lblFormMessage.Visibility = Visibility.Hidden;
            lblVerificationError.Visibility = Visibility.Hidden;

            foreach (TextBox currentDigitTextBox in _verificationDigitTextBoxes)
            {
                currentDigitTextBox.IsEnabled = true;
                currentDigitTextBox.Text = string.Empty;
            }

            btnVerifyCode.IsEnabled = false;

            if (!_countdownDispatcherTimer.IsEnabled)
            {
                _countdownDispatcherTimer.Start();
            }

            _verificationDigitTextBoxes[FirstTextBoxIndex].Focus();
        }

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            _countdownDispatcherTimer.Stop();
            _onBackCallbackAction?.Invoke();
        }
    }
}