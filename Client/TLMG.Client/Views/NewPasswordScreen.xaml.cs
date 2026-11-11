using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TLMG.Client.Navigation;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla de restablecimiento y configuración de una nueva contraseña.
    /// </summary>
    public partial class NewPasswordScreen : LocalizableScreen
    {
        private const bool IsErrorMessage = true;
        private const int FirstIndex = 0;
        private const int SuccessRedirectDelayMilliseconds = 500;

        private const string SuccessMessageKey = "NewPassword_SuccessMessage";
        private const string EmptyBarBrushKey = "Brush_Border";

        private readonly NewPasswordViewModel _viewModel;
        private readonly Border[] _strengthBars;
        private readonly DispatcherTimer _redirectTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="NewPasswordScreen"/> y sus temporizadores.
        /// </summary>
        public NewPasswordScreen()
        {
            InitializeComponent();

            _strengthBars = new Border[]
            {
                brdStrength1, brdStrength2, brdStrength3, brdStrength4, brdStrength5
            };

            _viewModel = new NewPasswordViewModel();
            _viewModel.PasswordAccepted += OnPasswordAccepted;
            _viewModel.BackRequested += OnBackRequested;
            _viewModel.ValidationFailed += OnValidationFailed;
            _viewModel.PasswordStrengthChanged += OnPasswordStrengthChanged;
            DataContext = _viewModel;

            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SuccessRedirectDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;

            Unloaded += OnScreenUnloaded;
        }

        protected override void UpdateDynamicLabels()
        {
            UpdatePasswordStrengthDisplay();
        }

        private void PwdNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.NewPassword = passwordBox.Password;
            }
        }

        private void PwdConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.ConfirmPassword = passwordBox.Password;
            }
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _redirectTimer.Stop();
        }

        private void OnPasswordStrengthChanged(object sender, EventArgs e)
        {
            UpdatePasswordStrengthDisplay();
        }

        private void OnValidationFailed(object sender, EventArgs e)
        {
            HideErrorMessages();

            ShowErrorKey(lblPasswordError, _viewModel.PasswordErrorKey);
            ShowErrorKey(lblMismatchError, _viewModel.ConfirmPasswordErrorKey);
            ShowErrorKey(lblFormMessage, _viewModel.FormErrorKey);
        }

        private void OnPasswordAccepted(object sender, EventArgs e)
        {
            HideErrorMessages();

            ShowStatusMessage(lblFormMessage, FindMessage(SuccessMessageKey), !IsErrorMessage);
            _redirectTimer.Start();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
        }

        private void OnRedirectTimerTick(object sender, EventArgs e)
        {
            _redirectTimer.Stop();

            NavigationManager navigationManager = ((App)Application.Current).Navigation;
            navigationManager.NavigateToWithoutHistory(ScreenId.Login);
            navigationManager.ClearHistory();
        }

        private void UpdatePasswordStrengthDisplay()
        {
            string brushKey = _viewModel.StrengthBrushKey;
            bool hasStrength = brushKey != null;

            Brush emptyBarBrush = (Brush)FindResource(EmptyBarBrushKey);
            Brush indicatorBrush = emptyBarBrush;

            if (hasStrength)
            {
                indicatorBrush = (Brush)FindResource(brushKey);
            }

            for (int barIndex = FirstIndex; barIndex < _strengthBars.Length; barIndex++)
            {
                if (barIndex < _viewModel.PasswordStrengthScore)
                {
                    _strengthBars[barIndex].Background = indicatorBrush;
                }
                else
                {
                    _strengthBars[barIndex].Background = emptyBarBrush;
                }
            }

            if (hasStrength)
            {
                lblStrengthLabel.Text = FindMessage(_viewModel.StrengthLabelKey);
                lblStrengthLabel.Foreground = indicatorBrush;
                lblStrengthLabel.Visibility = Visibility.Visible;
            }
            else
            {
                lblStrengthLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowErrorKey(TextBlock errorLabel, string errorKey)
        {
            if (errorKey != null)
            {
                string localizedMessage = ErrorMessageHelper.FindErrorMessage(this, errorKey);
                ShowStatusMessage(errorLabel, localizedMessage, IsErrorMessage);
            }
        }

        private void HideErrorMessages()
        {
            lblFormMessage.Visibility = Visibility.Hidden;
            lblPasswordError.Visibility = Visibility.Hidden;
            lblMismatchError.Visibility = Visibility.Hidden;
        }

        private string FindMessage(string resourceKey)
        {
            string localizedMessage = TryFindResource(resourceKey) as string ?? resourceKey;

            return localizedMessage;
        }
    }
}