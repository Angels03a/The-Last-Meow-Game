using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Validation;

namespace TLMG.Client.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private const string LastPasswordChangeFormatKey = "Profile_LastPasswordChange";

        private readonly INavigationManager _navigationManager;
        private readonly IProfileConnection _profileConnection;

        private bool _isEditingAvatar;
        private bool _isEditingNickname;
        private string _currentAvatarKey = AvatarCatalog.DefaultAvatarKey;
        private string _currentAvatar = AvatarCatalog.FindEmoji(AvatarCatalog.DefaultAvatarKey);
        private string _displayNickname;
        private string _email;
        private string _editingNickname;
        private string _lastPasswordChangeLabel;
        private int _daysSincePasswordChange;

        public ProfileViewModel(INavigationManager navigationManager, IProfileConnection profileConnection)
        {
            _navigationManager = navigationManager;
            _profileConnection = profileConnection;
            NicknameError = new StatusMessage();

            BackCommand = new RelayCommand(ExecuteBack);
            ToggleAvatarEditModeCommand = new RelayCommand(ExecuteToggleAvatarEditMode);
            SelectAvatarCommand = new RelayCommand<string>(ExecuteSelectAvatar);
            BeginNicknameEditCommand = new RelayCommand(ExecuteBeginNicknameEdit);
            SaveNicknameCommand = new RelayCommand(ExecuteSaveNickname);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        }

        public ICommand BackCommand { get; }

        public ICommand ToggleAvatarEditModeCommand { get; }

        public ICommand SelectAvatarCommand { get; }

        public ICommand BeginNicknameEditCommand { get; }

        public ICommand SaveNicknameCommand { get; }

        public ICommand ChangePasswordCommand { get; }

        public bool IsEditingAvatar
        {
            get { return _isEditingAvatar; }
            private set { SetProperty(ref _isEditingAvatar, value); }
        }

        public bool IsEditingNickname
        {
            get { return _isEditingNickname; }
            private set { SetProperty(ref _isEditingNickname, value); }
        }

        public string CurrentAvatar
        {
            get { return _currentAvatar; }
            private set { SetProperty(ref _currentAvatar, value); }
        }

        public string DisplayNickname
        {
            get { return _displayNickname; }
            private set { SetProperty(ref _displayNickname, value); }
        }

        public string Email
        {
            get { return _email; }
            private set { SetProperty(ref _email, value); }
        }

        public string EditingNickname
        {
            get { return _editingNickname; }
            set { SetProperty(ref _editingNickname, value); }
        }

        public StatusMessage NicknameError { get; }

        public string LastPasswordChangeLabel
        {
            get { return _lastPasswordChangeLabel; }
            private set { SetProperty(ref _lastPasswordChangeLabel, value); }
        }

        public void Activate()
        {
            Deactivate();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateLastPasswordChangeLabel();
            LoadProfile();
        }

        public void Deactivate()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        protected override void OnDisposed()
        {
            Deactivate();
            NicknameError.Dispose();

            base.OnDisposed();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            UpdateLastPasswordChangeLabel();
        }

        private void UpdateLastPasswordChangeLabel()
        {
            string passwordFormat = LanguageManager.FindText(LastPasswordChangeFormatKey);
            LastPasswordChangeLabel = string.Format(passwordFormat, _daysSincePasswordChange);
        }

        private void LoadProfile()
        {
            OperationPlayerProfileResult profileResult = _profileConnection.GetPlayerProfile();

            if (profileResult.IsSuccess)
            {
                NicknameError.Hide();
                DisplayNickname = profileResult.Nickname;
                Email = profileResult.Email;
                ApplyAvatar(profileResult.AvatarKey);
                _daysSincePasswordChange = (DateTime.UtcNow - profileResult.PasswordChangedAtUtc).Days;
                UpdateLastPasswordChangeLabel();
            }
            else
            {
                ShowProfileError(profileResult.ErrorKey);
            }
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteToggleAvatarEditMode()
        {
            IsEditingAvatar = !IsEditingAvatar;
        }

        private void ExecuteSelectAvatar(string avatarKey)
        {
            if (!string.IsNullOrEmpty(avatarKey))
            {
                string errorKey = SaveProfile(DisplayNickname, avatarKey);

                if (errorKey == string.Empty)
                {
                    NicknameError.Hide();
                    ApplyAvatar(avatarKey);
                    IsEditingAvatar = false;
                }
                else
                {
                    ShowProfileError(errorKey);
                }
            }
        }

        private void ExecuteBeginNicknameEdit()
        {
            EditingNickname = DisplayNickname;
            NicknameError.Hide();
            IsEditingNickname = true;
        }

        private void ExecuteSaveNickname()
        {
            string errorKey = InputValidator.GetNicknameValidationErrorKey(EditingNickname);

            if (errorKey == null)
            {
                errorKey = SaveProfile(EditingNickname, _currentAvatarKey);
            }

            if (errorKey == string.Empty)
            {
                NicknameError.Hide();
                DisplayNickname = EditingNickname;
                IsEditingNickname = false;
            }
            else
            {
                ShowProfileError(errorKey);
            }
        }

        private void ExecuteChangePassword()
        {
            _navigationManager.NavigateTo(ScreenId.ChangePassword);
        }

        private string SaveProfile(string nickname, string avatarKey)
        {
            string errorKey = string.Empty;
            OperationResult updateResult = _profileConnection.UpdatePlayerProfile(nickname, avatarKey);

            if (updateResult == null || !updateResult.IsSuccess)
            {
                errorKey = updateResult?.ErrorKey;
                if (string.IsNullOrWhiteSpace(errorKey) || (System.Windows.Application.Current != null
                    && !(System.Windows.Application.Current.TryFindResource(errorKey) is string)))
                {
                    errorKey = "Common_UnexpectedError";
                }
            }

            return errorKey;
        }

        private void ApplyAvatar(string avatarKey)
        {
            _currentAvatarKey = avatarKey;
            CurrentAvatar = AvatarCatalog.FindEmoji(avatarKey);
        }

        private void ShowProfileError(string errorKey)
        {
            NicknameError.ShowError(LanguageManager.FindText(errorKey));
        }
    }
}
