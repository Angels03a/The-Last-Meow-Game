using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TLMG.Client.Communication;
using TLMG.Client.Constants;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Validation;

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

        public ChangePasswordViewModel(
            INavigationManager navigation,
            IPasswordConnection passwordConnection,
            IProfileConnection profileConnection)
        {
            _navigation = navigation;
            _passwordConnection = passwordConnection;
            _profileConnection = profileConnection;

            CurrentPasswordError = new StatusMessage();
            NewPasswordError = new StatusMessage();
            ConfirmNewPasswordError = new StatusMessage();
            FormMessage = new StatusMessage();

            _updatePasswordCommand = new RelayCommand(ExecuteUpdatePassword, CanExecuteAction);
            _cancelCommand = new RelayCommand(ExecuteCancel, CanExecuteAction);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteAction);
        }

        public ICommand UpdatePasswordCommand => _updatePasswordCommand;

        public ICommand CancelCommand => _cancelCommand;

        public ICommand BackCommand => _backCommand;

        public StatusMessage CurrentPasswordError { get; }

        public StatusMessage NewPasswordError { get; }

        public StatusMessage ConfirmNewPasswordError { get; }

        public StatusMessage FormMessage { get; }

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
                FormMessage.ShowError(LanguageManager.FindText(profileResult.ErrorKey));
            }
        }

        protected override void OnDisposed()
        {
            CurrentPasswordError.Dispose();
            NewPasswordError.Dispose();
            ConfirmNewPasswordError.Dispose();
            FormMessage.Dispose();

            base.OnDisposed();
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
                FormMessage.ShowSuccess(LanguageManager.FindText(SuccessMessageKey));
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
            bool isPasswordStrong = InputValidator.CalculatePasswordStrength(NewPassword)
                >= ValidationConstants.RequiredPasswordStrengthScore;
            bool doPasswordsMatch = NewPassword == ConfirmNewPassword;
            bool isFormValid = false;

            if (!areFieldsFilled)
            {
                FormMessage.ShowError(LanguageManager.FindText(IncompleteFieldsErrorKey));
            }
            else if (!isPasswordStrong)
            {
                NewPasswordError.ShowError(LanguageManager.FindText(PolicyErrorKey));
            }
            else if (!doPasswordsMatch)
            {
                ConfirmNewPasswordError.ShowError(LanguageManager.FindText(MismatchErrorKey));
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
                CurrentPasswordError.ShowError(errorMessage);
            }
            else if (errorKey == ServerErrorKeys.PolicyNotMet || errorKey == ServerErrorKeys.SamePassword)
            {
                NewPasswordError.ShowError(errorMessage);
            }
            else
            {
                FormMessage.ShowError(errorMessage);
            }
        }

        private void ClearMessages()
        {
            CurrentPasswordError.Hide();
            NewPasswordError.Hide();
            ConfirmNewPasswordError.Hide();
            FormMessage.Hide();
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