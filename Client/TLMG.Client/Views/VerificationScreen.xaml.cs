using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla de verificación mediante código numérico de 6 dígitos enviado por correo.
    /// </summary>
    public partial class VerificationScreen : LocalizableScreen
    {
        private const bool IsErrorMessage = true;
        private const int FirstIndex = 0;
        private const int NextOffset = 1;
        private const int PreviousOffset = 1;
        private const int EmptyTextLength = 0;
        private const int SingleDigitLength = 1;
        private const int NoSecondsRemaining = 0;
        private const int CountdownTimerIntervalSeconds = 1;

        private const string ResendButtonFormat = "{0} ({1:00}s)";
        private const string ResendButtonKey = "Verification_ResendCodeButton";
        private const string SuccessMessageKey = "Verification_SuccessMessage";
        private const string ErrorBrushKey = "Brush_Error";
        private const string EmptyDotBrushKey = "Brush_Border";

        private readonly Action _onVerifiedCallbackAction;
        private readonly Action _onBackCallbackAction;
        private readonly VerificationViewModel _viewModel;
        private readonly TextBox[] _digitTextBoxes;
        private readonly Ellipse[] _attemptEllipses;
        private readonly DispatcherTimer _countdownTimer;

        /// <summary>
        /// Inicializa la pantalla de verificación con un ViewModel por defecto.
        /// </summary>
        /// <param name="onVerifiedCallbackAction">Acción al verificar el código correctamente.</param>
        /// <param name="onBackCallbackAction">Acción al regresar a la pantalla anterior.</param>
        public VerificationScreen(
            Action onVerifiedCallbackAction,
            Action onBackCallbackAction)
            : this(new VerificationViewModel(), onVerifiedCallbackAction, onBackCallbackAction)
        {
        }

        /// <summary>
        /// Inicializa la pantalla de verificación con su ViewModel y sus acciones de navegación.
        /// </summary>
        /// <param name="viewModel">ViewModel de verificación inyectado por la fábrica de pantallas.</param>
        /// <param name="onVerifiedCallbackAction">Acción al verificar el código correctamente.</param>
        /// <param name="onBackCallbackAction">Acción al regresar a la pantalla anterior.</param>
        public VerificationScreen(
            VerificationViewModel viewModel,
            Action onVerifiedCallbackAction,
            Action onBackCallbackAction)
        {
            InitializeComponent();

            _onVerifiedCallbackAction = onVerifiedCallbackAction;
            _onBackCallbackAction = onBackCallbackAction;
            _digitTextBoxes = new TextBox[] { txtDigit1, txtDigit2, txtDigit3, txtDigit4, txtDigit5, txtDigit6 };
            _attemptEllipses = new Ellipse[] { ellAttempt1, ellAttempt2, ellAttempt3 };

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(CountdownTimerIntervalSeconds)
            };
            _countdownTimer.Tick += OnCountdownTimerTick;

            _viewModel = viewModel;
            _viewModel.VerificationSucceeded += OnVerificationSucceeded;
            _viewModel.BackRequested += OnBackRequested;
            _viewModel.MessagesChanged += OnMessagesChanged;
            _viewModel.AttemptsChanged += OnAttemptsChanged;
            _viewModel.DigitsCleared += OnDigitsCleared;
            _viewModel.CountdownStateChanged += OnCountdownStateChanged;
            _viewModel.CountdownUpdated += OnCountdownUpdated;
            DataContext = _viewModel;

            Unloaded += OnScreenUnloaded;

            UpdateAttemptDots();
            _viewModel.StartCountdown();
        }

        protected override void UpdateDynamicLabels()
        {
            UpdateResendButtonContent();
        }

        private void DigitBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox currentDigitTextBox = (TextBox)sender;
            int currentIndex = Array.IndexOf(_digitTextBoxes, currentDigitTextBox);

            bool isBoxFilled = currentDigitTextBox.Text.Length == SingleDigitLength;
            bool hasNextBox = currentIndex < _digitTextBoxes.Length - NextOffset;

            lblFormMessage.Visibility = Visibility.Hidden;

            if (isBoxFilled && hasNextBox)
            {
                _digitTextBoxes[currentIndex + NextOffset].Focus();
            }
        }

        private void DigitBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox currentDigitTextBox = (TextBox)sender;
            int currentIndex = Array.IndexOf(_digitTextBoxes, currentDigitTextBox);

            bool isBackspaceOnEmptyBox = e.Key == Key.Back && currentDigitTextBox.Text.Length == EmptyTextLength;
            bool hasPreviousBox = currentIndex > FirstIndex;

            if (isBackspaceOnEmptyBox && hasPreviousBox)
            {
                _digitTextBoxes[currentIndex - PreviousOffset].Focus();
            }
        }

        private void OnCountdownTimerTick(object sender, EventArgs e)
        {
            _viewModel.Tick();
        }

        private void OnCountdownStateChanged(object sender, EventArgs e)
        {
            if (_viewModel.IsCountdownRunning)
            {
                _countdownTimer.Start();
            }
            else
            {
                _countdownTimer.Stop();
            }
        }

        private void OnCountdownUpdated(object sender, EventArgs e)
        {
            UpdateResendButtonContent();
        }

        private void OnAttemptsChanged(object sender, EventArgs e)
        {
            UpdateAttemptDots();
        }

        private void OnDigitsCleared(object sender, EventArgs e)
        {
            _digitTextBoxes[FirstIndex].Focus();
        }

        private void OnMessagesChanged(object sender, EventArgs e)
        {
            lblFormMessage.Visibility = Visibility.Hidden;
            lblVerificationError.Visibility = Visibility.Hidden;

            if (_viewModel.FormErrorKey != null)
            {
                ShowPersistentErrorMessage(_viewModel.FormErrorKey);
            }

            if (_viewModel.VerificationErrorKey != null)
            {
                ShowStatusMessage(
                    lblVerificationError,
                    FindMessage(_viewModel.VerificationErrorKey),
                    IsErrorMessage);
            }
        }

        private void OnVerificationSucceeded(object sender, EventArgs e)
        {
            ShowStatusMessage(lblFormMessage, FindMessage(SuccessMessageKey), !IsErrorMessage);
            _onVerifiedCallbackAction?.Invoke();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            _onBackCallbackAction?.Invoke();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _countdownTimer.Stop();
        }

        private void UpdateResendButtonContent()
        {
            string originalButtonText = FindMessage(ResendButtonKey);

            if (_viewModel.ResendSecondsRemaining > NoSecondsRemaining)
            {
                btnResendCode.Content = string.Format(
                    ResendButtonFormat,
                    originalButtonText,
                    _viewModel.ResendSecondsRemaining);
            }
            else
            {
                btnResendCode.Content = originalButtonText;
            }
        }

        private void UpdateAttemptDots()
        {
            Brush failedBrush = (Brush)FindResource(ErrorBrushKey);
            Brush emptyBrush = (Brush)FindResource(EmptyDotBrushKey);

            for (int dotIndex = FirstIndex; dotIndex < _attemptEllipses.Length; dotIndex++)
            {
                if (dotIndex < _viewModel.FailedAttempts)
                {
                    _attemptEllipses[dotIndex].Fill = failedBrush;
                }
                else
                {
                    _attemptEllipses[dotIndex].Fill = emptyBrush;
                }
            }
        }

        private void ShowPersistentErrorMessage(string errorKey)
        {
            lblFormMessage.Text = FindMessage(errorKey);
            lblFormMessage.Foreground = (Brush)FindResource(ErrorBrushKey);
            lblFormMessage.Visibility = Visibility.Visible;
        }

        private string FindMessage(string resourceKey)
        {
            string localizedMessage = TryFindResource(resourceKey) as string ?? resourceKey;

            return localizedMessage;
        }
    }
}