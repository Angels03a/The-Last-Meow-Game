using System;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado, validación y comandos de la pantalla de registro de cuentas.
    /// </summary>
    public class CreateAccountViewModel : ViewModelBase
    {
        private const string ScreenResourcePrefix = "CreateAccount";

        private const string IncompleteFieldsErrorKey = "CreateAccount_IncompleteFieldsError";
        private const string NicknameFormatErrorKey = "CreateAccount_NicknameFormatError";
        private const string NicknameOffensiveErrorKey = "CreateAccount_NicknameOffensiveError";
        private const string NicknameTakenErrorKey = "CreateAccount_NicknameTakenError";
        private const string EmailCharactersErrorKey = "CreateAccount_EmailCharactersError";
        private const string EmailFormatErrorKey = "CreateAccount_EmailFormatError";
        private const string EmailDomainErrorKey = "CreateAccount_EmailDomainError";
        private const string EmailTakenErrorKey = "CreateAccount_EmailTakenError";
        private const string PasswordPolicyErrorKey = "CreateAccount_PasswordPolicyError";
        private const string MismatchErrorKey = "CreateAccount_MismatchError";
        private const string TermsErrorKey = "CreateAccount_TermsError";
        private const string ConnectionErrorKey = "CreateAccount_NetworkError";
        private const string UnknownErrorKey = "CreateAccount_DataModelError";

        private string _nickname;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private bool _hasAcceptedTerms;
        private bool _isBusy;
        private string _nicknameErrorKey;
        private string _emailErrorKey;
        private string _passwordErrorKey;
        private string _confirmPasswordErrorKey;
        private string _formErrorKey;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountViewModel"/>.
        /// </summary>
        public CreateAccountViewModel()
        {
            CreateAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
            BackCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        /// <summary>
        /// Se produce cuando el formulario es válido y la vista debe enviar el registro al servidor.
        /// </summary>
        public event EventHandler RegistrationRequested;

        /// <summary>
        /// Se produce cuando el jugador solicita volver al inicio de sesión.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>
        /// Se produce cuando hay claves de error que la vista debe mostrar.
        /// </summary>
        public event EventHandler ValidationFailed;

        /// <summary>
        /// Comando que valida el formulario y solicita el registro.
        /// </summary>
        public RelayCommand CreateAccountCommand { get; }

        /// <summary>
        /// Comando que solicita volver al inicio de sesión.
        /// </summary>
        public RelayCommand BackCommand { get; }

        /// <summary>
        /// Nickname capturado.
        /// </summary>
        public string Nickname
        {
            get { return _nickname; }
            set { SetProperty(ref _nickname, value); }
        }

        /// <summary>
        /// Correo electrónico capturado.
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        /// <summary>
        /// Contraseña copiada desde el PasswordBox de la vista.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        /// <summary>
        /// Confirmación de contraseña copiada desde el PasswordBox de la vista.
        /// </summary>
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { SetProperty(ref _confirmPassword, value); }
        }

        /// <summary>
        /// Indica si el jugador aceptó los términos y la política de privacidad.
        /// </summary>
        public bool HasAcceptedTerms
        {
            get { return _hasAcceptedTerms; }
            set { SetProperty(ref _hasAcceptedTerms, value); }
        }

        /// <summary>
        /// Indica si hay una solicitud en curso; mientras es verdadera el botón de registro se deshabilita.
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CreateAccountCommand.NotifyCanExecuteChanged();
                    BackCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Clave de error del nickname; null si no hay error.
        /// </summary>
        public string NicknameErrorKey
        {
            get { return _nicknameErrorKey; }
            private set { SetProperty(ref _nicknameErrorKey, value); }
        }

        /// <summary>
        /// Clave de error del correo; null si no hay error.
        /// </summary>
        public string EmailErrorKey
        {
            get { return _emailErrorKey; }
            private set { SetProperty(ref _emailErrorKey, value); }
        }

        /// <summary>
        /// Clave de error de la contraseña; null si no hay error.
        /// </summary>
        public string PasswordErrorKey
        {
            get { return _passwordErrorKey; }
            private set { SetProperty(ref _passwordErrorKey, value); }
        }

        /// <summary>
        /// Clave de error de la confirmación de contraseña; null si no hay error.
        /// </summary>
        public string ConfirmPasswordErrorKey
        {
            get { return _confirmPasswordErrorKey; }
            private set { SetProperty(ref _confirmPasswordErrorKey, value); }
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
        /// Asigna al campo correspondiente la clave de error que devolvió el servidor.
        /// </summary>
        /// <param name="errorKey">Clave de error del servidor; puede ser null.</param>
        public void ApplyServerError(string errorKey)
        {
            ClearErrors();

            string resolvedErrorKey = errorKey ?? UnknownErrorKey;

            switch (resolvedErrorKey)
            {
                case NicknameFormatErrorKey:
                case NicknameOffensiveErrorKey:
                case NicknameTakenErrorKey:
                    NicknameErrorKey = resolvedErrorKey;
                    break;
                case EmailCharactersErrorKey:
                case EmailFormatErrorKey:
                case EmailDomainErrorKey:
                case EmailTakenErrorKey:
                    EmailErrorKey = resolvedErrorKey;
                    break;
                case PasswordPolicyErrorKey:
                    PasswordErrorKey = resolvedErrorKey;
                    break;
                default:
                    FormErrorKey = resolvedErrorKey;
                    break;
            }

            ValidationFailed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Registra un fallo de conexión como error general del formulario.
        /// </summary>
        public void ReportConnectionFailure()
        {
            ClearErrors();
            FormErrorKey = ConnectionErrorKey;
            ValidationFailed?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteCreateAccount()
        {
            ClearErrors();

            bool isFormValid = ValidateForm();

            if (isFormValid)
            {
                RegistrationRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ValidationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanExecuteCreateAccount()
        {
            bool canCreateAccount = !IsBusy;
            return canCreateAccount;
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteBack()
        {
            bool canBack = !IsBusy;
            return canBack;
        }

        private bool ValidateForm()
        {
            bool hasNickname = !string.IsNullOrWhiteSpace(Nickname);
            bool hasEmail = !string.IsNullOrWhiteSpace(Email);
            bool hasPassword = !string.IsNullOrEmpty(Password);
            bool hasConfirmation = !string.IsNullOrEmpty(ConfirmPassword);
            bool areRequiredFieldsFilled = hasNickname && hasEmail && hasPassword && hasConfirmation;

            if (areRequiredFieldsFilled)
            {
                NicknameErrorKey = ValidationHelper.GetNicknameValidationErrorKey(Nickname);
                EmailErrorKey = ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);
                PasswordErrorKey = GetPasswordErrorKey();
                ConfirmPasswordErrorKey = GetConfirmPasswordErrorKey();
                FormErrorKey = GetTermsErrorKey();
            }
            else
            {
                FormErrorKey = IncompleteFieldsErrorKey;
            }

            bool isFormValid = !HasErrors();
            return isFormValid;
        }

        private string GetPasswordErrorKey()
        {
            int passwordStrengthScore = ValidationHelper.CalculatePasswordStrength(Password);
            bool isPasswordStrong = passwordStrengthScore >= ValidationHelper.RequiredPasswordStrengthScore;
            string errorKey = null;

            if (!isPasswordStrong)
            {
                errorKey = PasswordPolicyErrorKey;
            }

            return errorKey;
        }

        private string GetConfirmPasswordErrorKey()
        {
            bool doPasswordsMatch = Password == ConfirmPassword;
            string errorKey = null;

            if (!doPasswordsMatch)
            {
                errorKey = MismatchErrorKey;
            }

            return errorKey;
        }

        private string GetTermsErrorKey()
        {
            string errorKey = null;

            if (!HasAcceptedTerms)
            {
                errorKey = TermsErrorKey;
            }

            return errorKey;
        }

        private bool HasErrors()
        {
            bool hasFieldErrors = NicknameErrorKey != null || EmailErrorKey != null || PasswordErrorKey != null;
            bool hasOtherErrors = ConfirmPasswordErrorKey != null || FormErrorKey != null;
            bool hasErrors = hasFieldErrors || hasOtherErrors;

            return hasErrors;
        }

        private void ClearErrors()
        {
            NicknameErrorKey = null;
            EmailErrorKey = null;
            PasswordErrorKey = null;
            ConfirmPasswordErrorKey = null;
            FormErrorKey = null;
        }
    }
}