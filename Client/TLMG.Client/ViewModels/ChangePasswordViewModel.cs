using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private const string IncompleteFieldsErrorKey = "ChangePassword_IncompleteFieldsError";
        private const string PolicyErrorKey = "ChangePassword_PolicyError";
        private const string MismatchErrorKey = "ChangePassword_MismatchError";
        private const string SuccessMessageKey = "ChangePassword_SuccessMessage";

        private readonly INavigationManager _navigation;
        private readonly IPasswordConnection _passwordConnection;
        private readonly IProfileConnection _profileConnection;
        private readonly RelayCommand _updatePasswordCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _backCommand;

        private string _avatarEmoji = AvatarCatalog.FindEmoji(null);
        private string _nickname;
        private string _email;
        private string _currentPassword;
        private string _newPassword;
        private string _confirmNewPassword;
        private bool _isBusy;
        private string _currentPasswordErrorMessage;
        private bool _hasCurrentPasswordError;
        private string _newPasswordErrorMessage;
        private bool _hasNewPasswordError;
        private string _confirmNewPasswordErrorMessage;
        private bool _hasConfirmNewPasswordError;
        private string _formMessage;
        private bool _hasFormMessage;

        public ChangePasswordViewModel(
            INavigationManager navigation,
            IPasswordConnection passwordConnection,
            IProfileConnection profileConnection)
        {
            _navigation = navigation;
            _passwordConnection = passwordConnection;
            _profileConnection = profileConnection;

            _updatePasswordCommand = new RelayCommand(ExecuteUpdatePassword, CanExecuteAction);
            _cancelCommand = new RelayCommand(ExecuteCancel, CanExecuteAction);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteAction);
        }

        public ICommand UpdatePasswordCommand => _updatePasswordCommand;

        public ICommand CancelCommand => _cancelCommand;

        public ICommand BackCommand => _backCommand;

        public string AvatarEmoji
        {
            get { return _avatarEmoji; }
            private set { SetProperty(ref _avatarEmoji, value); }
        }

        public string Nickname
        {
            get { return _nickname; }
            private set { SetProperty(ref _nickname, value); }
        }

        public string Email
        {
            get { return _email; }
            private set { SetProperty(ref _email, value); }
        }

        public string CurrentPassword
        {
            get { return _currentPassword; }
            set { SetProperty(ref _currentPassword, value); }
        }

        public string NewPassword
        {
            get { return _newPassword; }
            set { SetProperty(ref _newPassword, value); }
        }

        public string ConfirmNewPassword
        {
            get { return _confirmNewPassword; }
            set { SetProperty(ref _confirmNewPassword, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _updatePasswordCommand.NotifyCanExecuteChanged();
                    _cancelCommand.NotifyCanExecuteChanged();
                    _backCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string CurrentPasswordErrorMessage
        {
            get { return _currentPasswordErrorMessage; }
            private set { SetProperty(ref _currentPasswordErrorMessage, value); }
        }

        public bool HasCurrentPasswordError
        {
            get { return _hasCurrentPasswordError; }
            private set { SetProperty(ref _hasCurrentPasswordError, value); }
        }

        public string NewPasswordErrorMessage
        {
            get { return _newPasswordErrorMessage; }
            private set { SetProperty(ref _newPasswordErrorMessage, value); }
        }

        public bool HasNewPasswordError
        {
            get { return _hasNewPasswordError; }
            private set { SetProperty(ref _hasNewPasswordError, value); }
        }

        public string ConfirmNewPasswordErrorMessage
        {
            get { return _confirmNewPasswordErrorMessage; }
            private set { SetProperty(ref _confirmNewPasswordErrorMessage, value); }
        }

        public bool HasConfirmNewPasswordError
        {
            get { return _hasConfirmNewPasswordError; }
            private set { SetProperty(ref _hasConfirmNewPasswordError, value); }
        }

        public string FormMessage
        {
            get { return _formMessage; }
            private set { SetProperty(ref _formMessage, value); }
        }

        public bool HasFormMessage
        {
            get { return _hasFormMessage; }
            private set { SetProperty(ref _hasFormMessage, value); }
        }

        public void Activate()
        {
            OperationPlayerProfileResult profileResult = _profileConnection.GetPlayerProfile();

            if (profileResult.IsSuccess)
            {
                Nickname = profileResult.Nickname;
                Email = profileResult.Email;
                AvatarEmoji = AvatarCatalog.FindEmoji(profileResult.AvatarKey);
            }
            else
            {
                ShowFormMessage(profileResult.ErrorKey);
            }
        }

        private void ExecuteUpdatePassword()
        {
            ClearMessages();

            if (ValidateForm())
            {
                SendPasswordChange();
            }
        }

        private void SendPasswordChange()
        {
            IsBusy = true;
            OperationResult changeResult = _passwordConnection.ChangePassword(CurrentPassword, NewPassword);
            IsBusy = false;

            if (changeResult.IsSuccess)
            {
                ClearPasswords();
                ShowFormMessage(SuccessMessageKey);
            }
            else
            {
                ShowServerError(changeResult.ErrorKey);
            }
        }

        private bool ValidateForm()
        {
            bool areFieldsFilled = !string.IsNullOrWhiteSpace(CurrentPassword)
                && !string.IsNullOrWhiteSpace(NewPassword)
                && !string.IsNullOrWhiteSpace(ConfirmNewPassword);
            bool isPasswordStrong = ValidationHelper.CalculatePasswordStrength(NewPassword)
                >= ValidationHelper.RequiredPasswordStrengthScore;
            bool doPasswordsMatch = NewPassword == ConfirmNewPassword;
            bool isFormValid = false;

            if (!areFieldsFilled)
            {
                ShowFormMessage(IncompleteFieldsErrorKey);
            }
            else if (!isPasswordStrong)
            {
                NewPasswordErrorMessage = LanguageManager.FindText(PolicyErrorKey);
                HasNewPasswordError = true;
            }
            else if (!doPasswordsMatch)
            {
                ConfirmNewPasswordErrorMessage = LanguageManager.FindText(MismatchErrorKey);
                HasConfirmNewPasswordError = true;
            }
            else
            {
                isFormValid = true;
            }

            return isFormValid;
        }

        private void ShowServerError(string errorKey)
        {
            string errorMessage = LanguageManager.FindText(errorKey);

            if (errorKey == ServerErrorKeys.CurrentPasswordIncorrect)
            {
                CurrentPasswordErrorMessage = errorMessage;
                HasCurrentPasswordError = true;
            }
            else if (errorKey == ServerErrorKeys.PolicyNotMet || errorKey == ServerErrorKeys.SamePassword)
            {
                NewPasswordErrorMessage = errorMessage;
                HasNewPasswordError = true;
            }
            else
            {
                FormMessage = errorMessage;
                HasFormMessage = true;
            }
        }

        private void ShowFormMessage(string messageKey)
        {
            FormMessage = LanguageManager.FindText(messageKey);
            HasFormMessage = true;
        }

        private void ClearMessages()
        {
            HasCurrentPasswordError = false;
            HasNewPasswordError = false;
            HasConfirmNewPasswordError = false;
            HasFormMessage = false;
        }

        private void ClearPasswords()
        {
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
        }

        private void ExecuteCancel()
        {
            _navigation.GoBack();
        }

        private void ExecuteBack()
        {
            _navigation.GoBack();
        }

        private bool CanExecuteAction()
        {
            bool canExecute = !IsBusy;
            return canExecute;
        }

        private static class ServerErrorKeys
        {
            public const string CurrentPasswordIncorrect = "ChangePassword_CurrentPasswordError";
            public const string PolicyNotMet = "ChangePassword_PolicyError";
            public const string SamePassword = "ChangePassword_SamePasswordError";
        }
    }
}