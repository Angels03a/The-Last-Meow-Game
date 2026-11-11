using System;
using System.Windows.Input;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la pantalla de inicio de sesión.
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        private const string ScreenResourcePrefix = "Login";
        private const string IncompleteFieldsErrorKey = "Login_IncompleteFieldsError";
        private const string UnknownErrorKey = "Login_DataModelError";
        private const string SuccessMessageKey = "Login_SuccessMessage";
        private const int LoginAttemptDelayMilliseconds = 50;
        private const int RedirectDelayMilliseconds = 500;

        private readonly IAccountsConnection _accountsConnection;
        private readonly INavigationService _navigationService;
        private readonly RelayCommand _logInCommand;
        private readonly RelayCommand _playAsGuestCommand;
        private readonly RelayCommand _forgotPasswordCommand;
        private readonly RelayCommand _createAccountCommand;

        private string _email;
        private string _password;
        private bool _isPasswordVisible;
        private bool _isBusy;
        private DispatcherTimer _loginAttemptTimer;
        private DispatcherTimer _redirectTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginViewModel"/>.
        /// </summary>
        public LoginViewModel(
            IAccountsConnection accountsConnection,
            INavigationService navigationService)
        {
            _accountsConnection = accountsConnection;
            _navigationService = navigationService;

            EmailError = new StatusMessage();
            FormMessage = new StatusMessage();

            _logInCommand = new RelayCommand(ExecuteLogIn, CanExecuteLogIn);
            TogglePasswordVisibilityCommand = new RelayCommand(ExecuteTogglePasswordVisibility);
            _playAsGuestCommand = new RelayCommand(ExecutePlayAsGuest, CanExecutePlayAsGuest);
            _forgotPasswordCommand = new RelayCommand(ExecuteForgotPassword, CanExecuteForgotPassword);
            _createAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
        }

        /// <summary>
        /// Mensaje de error asociado al campo de correo electrónico.
        /// </summary>
        public StatusMessage EmailError { get; }

        /// <summary>
        /// Mensaje general del formulario (éxito o error).
        /// </summary>
        public StatusMessage FormMessage { get; }

        /// <summary>Comando para intentar el inicio de sesión.</summary>
        public ICommand LogInCommand => _logInCommand;

        /// <summary>Comando para alternar entre mostrar u ocultar la contraseña.</summary>
        public ICommand TogglePasswordVisibilityCommand { get; }

        /// <summary>Comando para acceder como invitado.</summary>
        public ICommand PlayAsGuestCommand => _playAsGuestCommand;

        /// <summary>Comando para navegar a la recuperación de contraseña.</summary>
        public ICommand ForgotPasswordCommand => _forgotPasswordCommand;

        /// <summary>Comando para navegar a la creación de cuenta.</summary>
        public ICommand CreateAccountCommand => _createAccountCommand;

        /// <summary>
        /// Correo electrónico capturado.
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        /// <summary>
        /// Contraseña capturada.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        /// <summary>
        /// Indica si la contraseña se muestra en texto claro.
        /// </summary>
        public bool IsPasswordVisible
        {
            get { return _isPasswordVisible; }
            set
            {
                bool wasChanged = SetProperty(ref _isPasswordVisible, value);
                if (wasChanged)
                {
                    OnPropertyChanged(nameof(IsPasswordHidden));
                }
            }
        }

        /// <summary>
        /// Indica si la contraseña se muestra enmascarada (inverso de <see cref="IsPasswordVisible"/>).
        /// </summary>
        public bool IsPasswordHidden
        {
            get { return !_isPasswordVisible; }
        }

        /// <summary>
        /// Indica si hay una operación en curso.
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                bool wasChanged = SetProperty(ref _isBusy, value);
                if (wasChanged)
                {
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Libera los recursos utilizados por el ViewModel.
        /// </summary>
        protected override void OnDisposed()
        {
            StopLoginAttemptTimer();
            StopRedirectTimer();

            EmailError.Dispose();
            FormMessage.Dispose();

            base.OnDisposed();
        }

        private void NotifyCommandsCanExecuteChanged()
        {
            _logInCommand.NotifyCanExecuteChanged();
            _playAsGuestCommand.NotifyCanExecuteChanged();
            _forgotPasswordCommand.NotifyCanExecuteChanged();
            _createAccountCommand.NotifyCanExecuteChanged();
        }

        private void ExecuteLogIn()
        {
            ClearMessages();
            bool isFormValid = ValidateForm();

            if (isFormValid)
            {
                IsBusy = true;
                StartLoginAttemptTimer();
            }
        }

        private bool CanExecuteLogIn()
        {
            bool canLogIn = !IsBusy;
            return canLogIn;
        }

        private void StartLoginAttemptTimer()
        {
            StopLoginAttemptTimer();
            _loginAttemptTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(LoginAttemptDelayMilliseconds)
            };
            _loginAttemptTimer.Tick += OnLoginAttemptTimerTick;
            _loginAttemptTimer.Start();
        }

        private void StopLoginAttemptTimer()
        {
            if (_loginAttemptTimer != null)
            {
                _loginAttemptTimer.Stop();
                _loginAttemptTimer.Tick -= OnLoginAttemptTimerTick;
                _loginAttemptTimer = null;
            }
        }

        private void OnLoginAttemptTimerTick(object sender, EventArgs eventArgs)
        {
            StopLoginAttemptTimer();

            OperationLogInResult logInResult = _accountsConnection.LogIn(Email, Password);

            ApplyLoginResult(logInResult);
            IsBusy = false;
        }

        private void ApplyLoginResult(OperationLogInResult logInResult)
        {
            if (logInResult.IsSuccess)
            {
                ShowSuccessAndRedirect();
            }
            else
            {
                ShowServerError(logInResult.ErrorKey);
            }
        }

        private void ShowSuccessAndRedirect()
        {
            string successText = LanguageManager.FindText(SuccessMessageKey);
            FormMessage.ShowSuccess(successText);
            StartRedirectTimer();
        }

        private void ShowServerError(string errorKey)
        {
            string effectiveKey = errorKey ?? UnknownErrorKey;
            string errorText = LanguageManager.FindText(effectiveKey);
            FormMessage.ShowError(errorText);
        }

        private void StartRedirectTimer()
        {
            StopRedirectTimer();
            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(RedirectDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;
            _redirectTimer.Start();
        }

        private void StopRedirectTimer()
        {
            if (_redirectTimer != null)
            {
                _redirectTimer.Stop();
                _redirectTimer.Tick -= OnRedirectTimerTick;
                _redirectTimer = null;
            }
        }

        private void OnRedirectTimerTick(object sender, EventArgs eventArgs)
        {
            StopRedirectTimer();
            _navigationService.NavigateTo(ScreenId.MainMenu);
        }

        private void ExecuteTogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ExecutePlayAsGuest()
        {
            _navigationService.NavigateTo(ScreenId.MainMenu);
        }

        private bool CanExecutePlayAsGuest()
        {
            bool canPlayAsGuest = !IsBusy;
            return canPlayAsGuest;
        }

        private void ExecuteForgotPassword()
        {
            _navigationService.NavigateTo(ScreenId.ForgotPassword);
        }

        private bool CanExecuteForgotPassword()
        {
            bool canForgotPassword = !IsBusy;
            return canForgotPassword;
        }

        private void ExecuteCreateAccount()
        {
            _navigationService.NavigateTo(ScreenId.CreateAccount);
        }

        private bool CanExecuteCreateAccount()
        {
            bool canCreateAccount = !IsBusy;
            return canCreateAccount;
        }

        private bool ValidateForm()
        {
            bool areRequiredFieldsFilled = ValidateRequiredFields();
            bool isEmailValid = ValidateEmailField();
            bool isFormValid = areRequiredFieldsFilled && isEmailValid;
            return isFormValid;
        }

        private bool ValidateRequiredFields()
        {
            bool hasEmail = !string.IsNullOrWhiteSpace(Email);
            bool hasPassword = !string.IsNullOrWhiteSpace(Password);
            bool areFilled = hasEmail && hasPassword;

            if (!areFilled)
            {
                string errorText = LanguageManager.FindText(IncompleteFieldsErrorKey);
                FormMessage.ShowError(errorText);
            }

            return areFilled;
        }

        private bool ValidateEmailField()
        {
            string emailErrorKey = ValidationHelper.GetEmailValidationErrorKey(
                Email, ScreenResourcePrefix);
            bool isValid = emailErrorKey == null;

            if (!isValid)
            {
                string errorText = LanguageManager.FindText(emailErrorKey);
                EmailError.ShowError(errorText);
            }

            return isValid;
        }

        private void ClearMessages()
        {
            EmailError.Hide();
            FormMessage.Hide();
        }
    }
}