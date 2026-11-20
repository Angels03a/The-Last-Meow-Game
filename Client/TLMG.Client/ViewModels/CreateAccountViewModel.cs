using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TLMG.Client.Communication;
using TLMG.Client.Constants;
using TLMG.Client.Navigation;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Validation;

namespace TLMG.Client.ViewModels
{
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

        public CreateAccountViewModel(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;

            NicknameError = new StatusMessage();
            EmailError = new StatusMessage();
            PasswordError = new StatusMessage();
            ConfirmPasswordError = new StatusMessage();
            FormMessage = new StatusMessage();

            _createAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        public ICommand CreateAccountCommand => _createAccountCommand;

        public ICommand BackCommand => _backCommand;

        public StatusMessage NicknameError { get; }

        public StatusMessage EmailError { get; }

        public StatusMessage PasswordError { get; }

        public StatusMessage ConfirmPasswordError { get; }

        public StatusMessage FormMessage { get; }

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

        protected override void OnDisposed()
        {
            NicknameError.Dispose();
            EmailError.Dispose();
            PasswordError.Dispose();
            ConfirmPasswordError.Dispose();
            FormMessage.Dispose();

            base.OnDisposed();
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

                IsBusy = false;

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
            bool isFormValid = false;

            if (areRequiredFieldsFilled)
            {
                string nicknameErrorKey = InputValidator.GetNicknameValidationErrorKey(Nickname);
                string emailErrorKey = InputValidator.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);
                string passwordErrorKey = GetPasswordErrorKey();
                string confirmPasswordErrorKey = GetConfirmPasswordErrorKey();
                string termsErrorKey = GetTermsErrorKey();

                ShowFieldError(NicknameError, nicknameErrorKey);
                ShowFieldError(EmailError, emailErrorKey);
                ShowFieldError(PasswordError, passwordErrorKey);
                ShowFieldError(ConfirmPasswordError, confirmPasswordErrorKey);
                ShowFieldError(FormMessage, termsErrorKey);

                isFormValid = nicknameErrorKey == null
                    && emailErrorKey == null
                    && passwordErrorKey == null
                    && confirmPasswordErrorKey == null
                    && termsErrorKey == null;
            }
            else
            {
                ShowFieldError(FormMessage, IncompleteFieldsErrorKey);
            }

            return isFormValid;
        }

        private string GetPasswordErrorKey()
        {
            int passwordStrengthScore = InputValidator.CalculatePasswordStrength(Password);
            bool isPasswordStrong = passwordStrengthScore >= ValidationConstants.RequiredPasswordStrengthScore;
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

        private void ApplyServerError(string errorKey)
        {
            ClearErrors();
            string resolvedErrorKey = errorKey ?? UnknownErrorKey;

            switch (resolvedErrorKey)
            {
                case NicknameFormatErrorKey:
                case NicknameOffensiveErrorKey:
                case NicknameTakenErrorKey:
                    ShowFieldError(NicknameError, resolvedErrorKey);
                    break;
                case EmailCharactersErrorKey:
                case EmailFormatErrorKey:
                case EmailDomainErrorKey:
                case EmailTakenErrorKey:
                    ShowFieldError(EmailError, resolvedErrorKey);
                    break;
                case PasswordPolicyErrorKey:
                    ShowFieldError(PasswordError, resolvedErrorKey);
                    break;
                default:
                    ShowFieldError(FormMessage, resolvedErrorKey);
                    break;
            }
        }

        private void ClearErrors()
        {
            NicknameError.Hide();
            EmailError.Hide();
            PasswordError.Hide();
            ConfirmPasswordError.Hide();
            FormMessage.Hide();
        }

        private void ShowFieldError(StatusMessage targetMessage, string errorKey)
        {
            if (errorKey == null)
            {
                targetMessage.Hide();
            }
            else
            {
                targetMessage.ShowError(LanguageManager.FindText(errorKey));
            }
        }
    }
}