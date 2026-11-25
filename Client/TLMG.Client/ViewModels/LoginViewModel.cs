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
        private const string GuestNoSaveMessageKey = "Login_GuestNoSaveMessage";
        private const string GuestNoSaveTitleKey = "Login_GuestNoSaveTitle";
        private const string SpanishCultureCode = "es-MX";
        private const int LoginAttemptDelayMilliseconds = 50;
        private const int RedirectDelayMilliseconds = 500;

        private readonly IAccountsConnection _accountsConnection;
        private readonly INavigationManager _navigationManager;
        private readonly IDialogManager _dialogManager;
        private readonly RelayCommand _logInCommand;
        private readonly RelayCommand _playAsGuestCommand;
        private readonly RelayCommand _forgotPasswordCommand;
        private readonly RelayCommand _createAccountCommand;

        private string _email;
        private string _password;
        private bool _isEnglishActive;
        private bool _isPasswordVisible;
        private bool _isBusy;
        private DispatcherTimer _loginAttemptTimer;
        private DispatcherTimer _redirectTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginViewModel"/>.
        /// </summary>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        /// <param name="navigationManager">Administrador de navegación.</param>
        /// <param name="dialogManager">Administrador de diálogos.</param>
        public LoginViewModel(
            IAccountsConnection accountsConnection,
            INavigationManager navigationManager,
            IDialogManager dialogManager)
        {
            _accountsConnection = accountsConnection;
            _navigationManager = navigationManager;
            _dialogManager = dialogManager;

            EmailError = new StatusMessage();
            FormMessage = new StatusMessage();

            _logInCommand = new RelayCommand(ExecuteLogIn, CanExecuteLogIn);
            TogglePasswordVisibilityCommand = new RelayCommand(ExecuteTogglePasswordVisibility);
            _playAsGuestCommand = new RelayCommand(ExecutePlayAsGuest, CanExecutePlayAsGuest);
            _forgotPasswordCommand = new RelayCommand(ExecuteForgotPassword, CanExecuteForgotPassword);
            _createAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
            RetryRecoveryCommand = new RelayCommand(_accountsConnection.RetryMatchRecovery);
            ChangeToEnglishCommand = new RelayCommand(ExecuteChangeToEnglish);
            ChangeToSpanishCommand = new RelayCommand(ExecuteChangeToSpanish);

            UpdateActiveLanguage();
        }

        /// <summary>
        /// Mensaje de error asociado al campo de correo electrónico.
        /// </summary>
        public StatusMessage EmailError { get; }

        /// <summary>
        /// Mensaje general del formulario (éxito o error).
        /// </summary>
        public StatusMessage FormMessage { get; }

        public ICommand RetryRecoveryCommand { get; }

        public bool CanRetryRecovery => _accountsConnection.IsMatchRecoveryExhausted;

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

        /// <summary>Comando para cambiar la interfaz a inglés.</summary>
        public ICommand ChangeToEnglishCommand { get; }

        /// <summary>Comando para cambiar la interfaz a español.</summary>
        public ICommand ChangeToSpanishCommand { get; }

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
        /// Indica si el idioma activo es el inglés; si no, es el español.
        /// </summary>
        public bool IsEnglishActive
        {
            get { return _isEnglishActive; }
            private set { SetProperty(ref _isEnglishActive, value); }
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

        private bool IsRecoveryActive => _accountsConnection.IsMatchRecoveryPending || CanRetryRecovery;

        /// <summary>
        /// Empieza a escuchar el cambio de idioma para refrescar el botón activo. La vista lo llama al cargarse.
        /// </summary>
        public void Activate()
        {
            Deactivate();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            _accountsConnection.RecoveryStateChanged += OnRecoveryStateChanged;
            UpdateActiveLanguage();
        }

        /// <summary>
        /// Deja de escuchar el cambio de idioma. La vista lo llama al descargarse.
        /// </summary>
        public void Deactivate()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            _accountsConnection.RecoveryStateChanged -= OnRecoveryStateChanged;
        }

        /// <summary>
        /// Libera los recursos utilizados por el ViewModel.
        /// </summary>
        protected override void OnDisposed()
        {
            Deactivate();
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

        private void ExecuteChangeToEnglish()
        {
            LanguageManager.ChangeLanguage(LanguageManager.EnglishCultureCode);
            UpdateActiveLanguage();
        }

        private void ExecuteChangeToSpanish()
        {
            LanguageManager.ChangeLanguage(SpanishCultureCode);
            UpdateActiveLanguage();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            UpdateActiveLanguage();
            if (IsRecoveryActive)
            {
                OnRecoveryStateChanged(sender, eventArgs);
            }
        }

        private void UpdateActiveLanguage()
        {
            IsEnglishActive = LanguageManager.CurrentCulture == LanguageManager.EnglishCultureCode;
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
            bool canLogIn = !IsBusy && !IsRecoveryActive;
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
            if (IsRecoveryActive)
            {
                OnRecoveryStateChanged(this, EventArgs.Empty);
            }
            else
            {
                StartRedirectTimer();
            }
        }

        private void OnRecoveryStateChanged(object sender, EventArgs args)
        {
            OnPropertyChanged(nameof(CanRetryRecovery));
            NotifyCommandsCanExecuteChanged();
            if (IsRecoveryActive)
            {
                StopRedirectTimer();
                FormMessage.ShowSuccess(LanguageManager.FindText(CanRetryRecovery
                    ? "Recovery_LocalExhausted" : "Recovery_Connecting"));
            }
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
            if (!IsRecoveryActive)
            {
                _navigationManager.NavigateToWithoutHistory(ScreenId.MainMenu);
            }
        }

        private void ExecuteTogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ExecutePlayAsGuest()
        {
            bool hasConfirmed = _dialogManager.Confirm(GuestNoSaveMessageKey, GuestNoSaveTitleKey);

            if (hasConfirmed)
            {
                ClearMessages();
                OperationLogInResult guestResult = _accountsConnection.StartGuestSession();

                if (guestResult.IsSuccess)
                {
                    _navigationManager.NavigateToWithoutHistory(ScreenId.MainMenu);
                }
                else
                {
                    ShowServerError(guestResult.ErrorKey);
                }
            }
        }

        private bool CanExecutePlayAsGuest()
        {
            bool canPlayAsGuest = !IsBusy && !IsRecoveryActive;
            return canPlayAsGuest;
        }

        private void ExecuteForgotPassword()
        {
            _navigationManager.NavigateTo(ScreenId.ForgotPassword);
        }

        private bool CanExecuteForgotPassword()
        {
            bool canForgotPassword = !IsBusy && !IsRecoveryActive;
            return canForgotPassword;
        }

        private void ExecuteCreateAccount()
        {
            _navigationManager.NavigateTo(ScreenId.CreateAccount);
        }

        private bool CanExecuteCreateAccount()
        {
            bool canCreateAccount = !IsBusy && !IsRecoveryActive;
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