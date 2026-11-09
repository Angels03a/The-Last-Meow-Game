using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado, validación y comandos de la pantalla de inicio de sesión.
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        private const string ScreenResourcePrefix = "Login";
        private const string IncompleteFieldsErrorKey = "Login_IncompleteFieldsError";
        private const string ConnectionErrorKey = "Login_NetworkError";
        private const string UnknownErrorKey = "Login_DataModelError";

        private string _email;
        private string _password;
        private bool _isPasswordVisible;
        private bool _isBusy;
        private string _emailErrorKey;
        private string _formErrorKey;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginViewModel"/>.
        /// </summary>
        public LoginViewModel()
        {
            LogInCommand = new RelayCommand(ExecuteLogIn, CanExecuteLogIn);
            TogglePasswordVisibilityCommand = new RelayCommand(ExecuteTogglePasswordVisibility);
            PlayAsGuestCommand = new RelayCommand(ExecutePlayAsGuest, CanExecutePlayAsGuest);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword, CanExecuteForgotPassword);
            CreateAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
        }

        /// <summary>
        /// Se produce cuando las credenciales son válidas localmente y se debe enviar la petición WCF.
        /// </summary>
        public event EventHandler LogInRequested;

        /// <summary>
        /// Se produce cuando el usuario solicita jugar como invitado.
        /// </summary>
        public event EventHandler GuestPlayRequested;

        /// <summary>
        /// Se produce cuando el usuario solicita recuperar su contraseña.
        /// </summary>
        public event EventHandler ForgotPasswordRequested;

        /// <summary>
        /// Se produce cuando el usuario solicita crear una cuenta nueva.
        /// </summary>
        public event EventHandler CreateAccountRequested;

        /// <summary>
        /// Se produce cuando falla la validación local o WCF reporta un error y la vista debe actualizar la interfaz.
        /// </summary>
        public event EventHandler ValidationFailed;

        /// <summary>
        /// Comando para intentar el inicio de sesión.
        /// </summary>
        public RelayCommand LogInCommand { get; }

        /// <summary>
        /// Comando para alternar entre mostrar u ocultar la contraseña.
        /// </summary>
        public RelayCommand TogglePasswordVisibilityCommand { get; }

        /// <summary>
        /// Comando para acceder sin cuenta.
        /// </summary>
        public RelayCommand PlayAsGuestCommand { get; }

        /// <summary>
        /// Comando para navegar a la recuperación de contraseña.
        /// </summary>
        public RelayCommand ForgotPasswordCommand { get; }

        /// <summary>
        /// Comando para navegar a la creación de cuenta.
        /// </summary>
        public RelayCommand CreateAccountCommand { get; }

        /// <summary>
        /// Correo electrónico capturado.
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        /// <summary>
        /// Contraseña copiada desde la vista (ya sea del PasswordBox o del TextBox visible).
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
            set { SetProperty(ref _isPasswordVisible, value); }
        }

        /// <summary>
        /// Indica si hay una operación asíncrona (WCF) en curso; deshabilita la interacción si es true.
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LogInCommand.NotifyCanExecuteChanged();
                    PlayAsGuestCommand.NotifyCanExecuteChanged();
                    ForgotPasswordCommand.NotifyCanExecuteChanged();
                    CreateAccountCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Clave de error específica del correo; null si no hay error.
        /// </summary>
        public string EmailErrorKey
        {
            get { return _emailErrorKey; }
            private set { SetProperty(ref _emailErrorKey, value); }
        }

        /// <summary>
        /// Clave de error general del formulario; null si no hay error.
        /// </summary>
        public string FormErrorKey
        {
            get { return _formErrorKey; }
            private set { SetProperty(ref _formErrorKey, value); }
        }

        /// <summary>
        /// Traduce un OperationLogInResult o SystemFault en claves de error para la vista.
        /// </summary>
        public void ApplyServerError(string errorKey)
        {
            ClearErrors();
            FormErrorKey = errorKey ?? UnknownErrorKey;
            ValidationFailed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Registra un fallo de red como error general.
        /// </summary>
        public void ReportConnectionFailure()
        {
            ClearErrors();
            FormErrorKey = ConnectionErrorKey;
            ValidationFailed?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteLogIn()
        {
            ClearErrors();
            bool isFormValid = ValidateForm();

            if (isFormValid)
            {
                LogInRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ValidationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanExecuteLogIn()
        {
            bool canLogIn = !IsBusy;
            return canLogIn;
        }

        private void ExecuteTogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ExecutePlayAsGuest()
        {
            GuestPlayRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecutePlayAsGuest()
        {
            bool canPlayAsGuest = !IsBusy;
            return canPlayAsGuest;
        }

        private void ExecuteForgotPassword()
        {
            ForgotPasswordRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteForgotPassword()
        {
            bool canForgotPassword = !IsBusy;
            return canForgotPassword;
        }

        private void ExecuteCreateAccount()
        {
            CreateAccountRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteCreateAccount()
        {
            bool canCreateAccount = !IsBusy;
            return canCreateAccount;
        }

        private bool ValidateForm()
        {
            bool hasEmail = !string.IsNullOrWhiteSpace(Email);
            bool hasPassword = !string.IsNullOrWhiteSpace(Password);
            bool areRequiredFieldsFilled = hasEmail && hasPassword;

            if (areRequiredFieldsFilled)
            {
                EmailErrorKey = ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);
            }
            else
            {
                FormErrorKey = IncompleteFieldsErrorKey;
            }

            bool isFormValid = !HasErrors();
            return isFormValid;
        }

        private bool HasErrors()
        {
            bool hasErrors = EmailErrorKey != null || FormErrorKey != null;
            return hasErrors;
        }

        private void ClearErrors()
        {
            EmailErrorKey = null;
            FormErrorKey = null;
        }
    }
}