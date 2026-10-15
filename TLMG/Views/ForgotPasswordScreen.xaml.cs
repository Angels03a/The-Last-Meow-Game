using System;
using System.Windows;
using System.Windows.Threading;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de recuperación de contraseña olvidada mediante envío de código de verificación.
    /// </summary>
    public partial class ForgotPasswordScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 1000;

        private DispatcherTimer _redirectTimer;

        public ForgotPasswordScreen()
        {
            InitializeComponent();
            Unloaded += OnScreenUnloaded;

            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ScreenTransitionDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _redirectTimer?.Stop();
        }

        private void TxtEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                lblEmailError.Visibility = Visibility.Collapsed;
            }
            else
            {
                ValidateAndDisplayEmailError(txtEmail.Text);
            }
        }

        private string ValidateEmail(string inputEmailAddress)
        {
            string errorResourceKey = ValidationHelper.GetEmailValidationErrorKey(inputEmailAddress, "ForgotPassword");
            return errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
        }

        private bool ValidateAndDisplayEmailError(string inputEmailAddress)
        {
            string emailValidationResult = ValidateEmail(inputEmailAddress);
            bool isEmailValid = emailValidationResult == null;

            if (isEmailValid)
            {
                lblEmailError.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowStatusMessage(lblEmailError, emailValidationResult, true);
            }

            return isEmailValid;
        }

        private void BtnSendCode_Click(object sender, RoutedEventArgs e)
        {
            string inputEmailAddress = txtEmail.Text;
            lblEmailError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(inputEmailAddress))
            {
                ShowStatusMessage(
                    lblFormMessage,
                    TryFindResource("ForgotPassword_EmailRequiredError") as string,
                    true);
            }
            else
            {
                if (ValidateAndDisplayEmailError(inputEmailAddress))
                {
                    ShowStatusMessage(
                        lblFormMessage,
                        TryFindResource("ForgotPassword_CodeSentMessage") as string,
                        false);
                    _redirectTimer.Start();
                }
            }
        }

        private void OnRedirectTimerTick(object sender, EventArgs e)
        {
            _redirectTimer.Stop();

            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new VerificationScreen(
                onVerifiedCallbackAction: () => {
                    applicationMainWindow.ShowScreen(new NewPasswordScreen());
                },
                onBackCallbackAction: () => {
                    applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
                }
            ));
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new LoginScreen());
        }
    }
}