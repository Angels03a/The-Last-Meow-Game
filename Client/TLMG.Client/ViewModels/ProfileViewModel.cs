using System;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Validation;

namespace TLMG.Client.ViewModels
{
    /// <summary>Displays the current player's identity and permits profile changes only for registered players.</summary>
    public class ProfileViewModel : ViewModelBase
    {
        private const string LastPasswordChangeFormatKey = "Profile_LastPasswordChange";
        private const string UnexpectedErrorKey = "Common_UnexpectedError";

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

        public bool IsGuest => _profileConnection.IsGuestSession;

        public bool IsRegisteredPlayer => !IsGuest;

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

            if (IsGuest)
            {
                NicknameError.Hide();
                ShowGuestIdentity();
            }
            else
            {
                LoadProfile();
            }
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

            if (IsGuest)
            {
                ShowGuestIdentity();
            }
        }

        private void ShowGuestIdentity()
        {
            DisplayNickname = DisplayNameHelper.Resolve(
                _profileConnection.PlayerUserId,
                _profileConnection.PlayerNickname);
        }

        private void UpdateLastPasswordChangeLabel()
        {
            string passwordFormat = LanguageManager.FindText(LastPasswordChangeFormatKey);
            LastPasswordChangeLabel = string.Format(passwordFormat, _daysSincePasswordChange);
        }

        private void LoadProfile()
        {
            OperationPlayerProfileResult profileResult = _profileConnection.GetPlayerProfile();
            string errorKey = ResolveProfileErrorKey(profileResult);

            if (errorKey == string.Empty)
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
                ShowProfileError(errorKey);
            }
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteToggleAvatarEditMode()
        {
            if (IsRegisteredPlayer)
            {
                IsEditingAvatar = !IsEditingAvatar;
            }
        }

        private void ExecuteSelectAvatar(string avatarKey)
        {
            if (IsRegisteredPlayer && !string.IsNullOrEmpty(avatarKey))
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
            if (IsRegisteredPlayer)
            {
                EditingNickname = DisplayNickname;
                NicknameError.Hide();
                IsEditingNickname = true;
            }
        }

        private void ExecuteSaveNickname()
        {
            if (IsRegisteredPlayer)
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
        }

        private void ExecuteChangePassword()
        {
            if (IsRegisteredPlayer)
            {
                _navigationManager.NavigateTo(ScreenId.ChangePassword);
            }
        }

        private string SaveProfile(string nickname, string avatarKey)
        {
            OperationResult updateResult = _profileConnection.UpdatePlayerProfile(nickname, avatarKey);
            string errorKey = ResolveProfileErrorKey(updateResult);

            return errorKey;
        }

        private static string ResolveProfileErrorKey(OperationResult result)
        {
            string errorKey = UnexpectedErrorKey;

            if (result != null)
            {
                if (result.IsSuccess && string.IsNullOrEmpty(result.ErrorKey))
                {
                    errorKey = string.Empty;
                }
                else if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorKey)
                    && (Application.Current == null || Application.Current.TryFindResource(result.ErrorKey) is string))
                {
                    errorKey = result.ErrorKey;
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
