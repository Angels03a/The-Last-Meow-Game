using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

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

        private const string ConfirmAbandonMessageKey = "CreateAccount_ConfirmAbandonMessage";
        private const string ConfirmAbandonTitleKey = "CreateAccount_ConfirmAbandonTitle";

        private readonly INavigationManager _navigation;
        private readonly IDialogManager _dialogs;
        private readonly IAccountsConnection _accountsConnection;
        private readonly RelayCommand _createAccountCommand;
        private readonly RelayCommand _backCommand;

        private string _nickname;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private bool _hasAcceptedTerms;
        private bool _isBusy;

        private string _nicknameErrorMessage;
        private bool _hasNicknameError;

        private string _emailErrorMessage;
        private bool _hasEmailError;

        private string _passwordErrorMessage;
        private bool _hasPasswordError;

        private string _confirmPasswordErrorMessage;
        private bool _hasConfirmPasswordError;

        private string _formErrorMessage;
        private bool _hasFormError;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountViewModel"/>.
        /// </summary>
        public CreateAccountViewModel(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;

            _createAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        /// <summary>Comando que valida el formulario y solicita crear la cuenta.</summary>
        public ICommand CreateAccountCommand => _createAccountCommand;

        /// <summary>Comando que solicita volver a la pantalla anterior, previa confirmación.</summary>
        public ICommand BackCommand => _backCommand;

        public string Nickname
        {
            get { return _nickname; }
            set { SetProperty(ref _nickname, value); }
        }

        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { SetProperty(ref _confirmPassword, value); }
        }

        public bool HasAcceptedTerms
        {
            get { return _hasAcceptedTerms; }
            set { SetProperty(ref _hasAcceptedTerms, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _createAccountCommand.NotifyCanExecuteChanged();
                    _backCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string NicknameErrorMessage
        {
            get { return _nicknameErrorMessage; }
            private set { SetProperty(ref _nicknameErrorMessage, value); }
        }

        public bool HasNicknameError
        {
            get { return _hasNicknameError; }
            private set { SetProperty(ref _hasNicknameError, value); }
        }

        public string EmailErrorMessage
        {
            get { return _emailErrorMessage; }
            private set { SetProperty(ref _emailErrorMessage, value); }
        }

        public bool HasEmailError
        {
            get { return _hasEmailError; }
            private set { SetProperty(ref _hasEmailError, value); }
        }

        public string PasswordErrorMessage
        {
            get { return _passwordErrorMessage; }
            private set { SetProperty(ref _passwordErrorMessage, value); }
        }

        public bool HasPasswordError
        {
            get { return _hasPasswordError; }
            private set { SetProperty(ref _hasPasswordError, value); }
        }

        public string ConfirmPasswordErrorMessage
        {
            get { return _confirmPasswordErrorMessage; }
            private set { SetProperty(ref _confirmPasswordErrorMessage, value); }
        }

        public bool HasConfirmPasswordError
        {
            get { return _hasConfirmPasswordError; }
            private set { SetProperty(ref _hasConfirmPasswordError, value); }
        }

        public string FormErrorMessage
        {
            get { return _formErrorMessage; }
            private set { SetProperty(ref _formErrorMessage, value); }
        }

        public bool HasFormError
        {
            get { return _hasFormError; }
            private set { SetProperty(ref _hasFormError, value); }
        }

        private void ExecuteCreateAccount()
        {
            ClearErrors();
            bool isFormValid = ValidateForm();

            if (isFormValid)
            {
                IsBusy = true;

                PlayerAccountRegistration playerAccountRegistration = new PlayerAccountRegistration
                {
                    Nickname = Nickname,
                    Email = Email,
                    Password = Password,
                    HasAcceptedTerms = HasAcceptedTerms
                };

                OperationResult verificationRequestResult =
                    _accountsConnection.RequestAccountVerificationCode(
                        playerAccountRegistration,
                        LanguageManager.CurrentCulture);

                if (verificationRequestResult.IsSuccess)
                {
                    VerificationNavigationParameter parameter =
                        new VerificationNavigationParameter(
                            VerificationFlow.AccountRegistration,
                            playerAccountRegistration);
                    _navigation.NavigateTo(ScreenId.Verification, parameter);
                }
                else
                {
                    ApplyServerError(verificationRequestResult.ErrorKey);
                }

                IsBusy = false;
            }
        }

        private bool CanExecuteCreateAccount()
        {
            bool canCreateAccount = !IsBusy;
            return canCreateAccount;
        }

        private void ExecuteBack()
        {
            bool hasConfirmedAbandon = _dialogs.ConfirmWithResult(ConfirmAbandonMessageKey, ConfirmAbandonTitleKey)
                == DialogResult.Yes;

            if (hasConfirmedAbandon)
            {
                _navigation.GoBack();
            }
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
            string formErrorKey = null;

            if (areRequiredFieldsFilled)
            {
                UpdateNicknameError(ValidationHelper.GetNicknameValidationErrorKey(Nickname));
                UpdateEmailError(ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix));
                UpdatePasswordError(GetPasswordErrorKey());
                UpdateConfirmPasswordError(GetConfirmPasswordErrorKey());
                formErrorKey = GetTermsErrorKey();
            }
            else
            {
                formErrorKey = IncompleteFieldsErrorKey;
            }

            UpdateFormError(formErrorKey);

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
            bool hasFieldErrors = HasNicknameError || HasEmailError || HasPasswordError;
            bool hasOtherErrors = HasConfirmPasswordError || HasFormError;
            bool hasErrors = hasFieldErrors || hasOtherErrors;

            return hasErrors;
        }

        private void ApplyServerError(string errorKey)
        {
            ClearErrors();
            string resolvedErrorKey = errorKey ?? UnknownErrorKey;

            switch (resolvedErrorKey)
            {
                case NicknameFormatErrorKey:
                case NicknameOffensiveErrorKey:
                case NicknameTakenErrorKey:
                    UpdateNicknameError(resolvedErrorKey);
                    break;
                case EmailCharactersErrorKey:
                case EmailFormatErrorKey:
                case EmailDomainErrorKey:
                case EmailTakenErrorKey:
                    UpdateEmailError(resolvedErrorKey);
                    break;
                case PasswordPolicyErrorKey:
                    UpdatePasswordError(resolvedErrorKey);
                    break;
                default:
                    UpdateFormError(resolvedErrorKey);
                    break;
            }
        }

        private void ClearErrors()
        {
            UpdateNicknameError(null);
            UpdateEmailError(null);
            UpdatePasswordError(null);
            UpdateConfirmPasswordError(null);
            UpdateFormError(null);
        }

        private void UpdateNicknameError(string errorKey)
        {
            bool hasError = errorKey != null;
            HasNicknameError = hasError;
            NicknameErrorMessage = hasError ? LanguageManager.FindText(errorKey) : string.Empty;
        }

        private void UpdateEmailError(string errorKey)
        {
            bool hasError = errorKey != null;
            HasEmailError = hasError;
            EmailErrorMessage = hasError ? LanguageManager.FindText(errorKey) : string.Empty;
        }

        private void UpdatePasswordError(string errorKey)
        {
            bool hasError = errorKey != null;
            HasPasswordError = hasError;
            PasswordErrorMessage = hasError ? LanguageManager.FindText(errorKey) : string.Empty;
        }

        private void UpdateConfirmPasswordError(string errorKey)
        {
            bool hasError = errorKey != null;
            HasConfirmPasswordError = hasError;
            ConfirmPasswordErrorMessage = hasError ? LanguageManager.FindText(errorKey) : string.Empty;
        }

        private void UpdateFormError(string errorKey)
        {
            bool hasError = errorKey != null;
            HasFormError = hasError;
            FormErrorMessage = hasError ? LanguageManager.FindText(errorKey) : string.Empty;
        }
    }
}