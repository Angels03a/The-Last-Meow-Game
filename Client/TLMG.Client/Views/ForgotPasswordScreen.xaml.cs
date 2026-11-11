using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TLMG.Client.Navigation;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla de recuperación de contraseña olvidada mediante envío de código de verificación.
    /// Delega la validación al ViewModel y gestiona la navegación.
    /// </summary>
    public partial class ForgotPasswordScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 500;
        private const string CodeSentMessageKey = "ForgotPassword_CodeSentMessage";

        private readonly ForgotPasswordViewModel _viewModel;
        private readonly DispatcherTimer _redirectTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ForgotPasswordScreen"/> y sus temporizadores.
        /// </summary>
        public ForgotPasswordScreen()
        {
            InitializeComponent();

            _viewModel = new ForgotPasswordViewModel();
            DataContext = _viewModel;

            _viewModel.CodeSentRequested += OnCodeSentRequested;
            _viewModel.ValidationFailed += OnValidationFailed;
            _viewModel.BackRequested += OnBackRequested;

            Unloaded += OnScreenUnloaded;

            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ScreenTransitionDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;
        }

        private void TxtEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.ValidateEmailOnly();

            bool isEmailValid = _viewModel.EmailErrorKey == null;
            if (isEmailValid)
            {
                lblEmailError.Visibility = Visibility.Collapsed;
            }
        }

        private void OnCodeSentRequested(object sender, EventArgs e)
        {
            lblEmailError.Visibility = Visibility.Collapsed;
            lblFormMessage.Visibility = Visibility.Collapsed;

            _viewModel.IsBusy = true;
            Mouse.OverrideCursor = Cursors.Wait;

            // TODO: Llamada futura al servidor
            ShowStatusMessage(lblFormMessage, ErrorMessageHelper.FindErrorMessage(this, CodeSentMessageKey), false);
            _redirectTimer.Start();

            UnlockUI();
        }

        private void OnValidationFailed(object sender, EventArgs e)
        {
            bool hasEmailError = _viewModel.EmailErrorKey != null;
            if (hasEmailError)
            {
                ShowStatusMessage(lblEmailError, ErrorMessageHelper.FindErrorMessage(this, _viewModel.EmailErrorKey), true);
            }

            bool hasFormError = _viewModel.FormErrorKey != null;
            if (hasFormError)
            {
                ShowStatusMessage(lblFormMessage, ErrorMessageHelper.FindErrorMessage(this, _viewModel.FormErrorKey), true);
            }
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            NavigationManager navigationManager = ((App)Application.Current).Navigation;
            navigationManager.NavigateToWithoutHistory(ScreenId.Login);
            navigationManager.ClearHistory();
        }

        private void OnRedirectTimerTick(object sender, EventArgs e)
        {
            _redirectTimer.Stop();

            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new VerificationScreen(
                onVerifiedCallbackAction: NavigateToNewPasswordScreen,
                onBackCallbackAction: NavigateToForgotPasswordScreen
            ));
        }

        private void NavigateToNewPasswordScreen()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new NewPasswordScreen());
        }

        private void NavigateToForgotPasswordScreen()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.CodeSentRequested -= OnCodeSentRequested;
            _viewModel.ValidationFailed -= OnValidationFailed;
            _viewModel.BackRequested -= OnBackRequested;

            _redirectTimer?.Stop();
        }

        private void UnlockUI()
        {
            Dispatcher.BeginInvoke(new Action(ReleaseUIAfterWCF), DispatcherPriority.ApplicationIdle);
        }

        private void ReleaseUIAfterWCF()
        {
            _viewModel.IsBusy = false;
            Mouse.OverrideCursor = null;
        }
    }
}