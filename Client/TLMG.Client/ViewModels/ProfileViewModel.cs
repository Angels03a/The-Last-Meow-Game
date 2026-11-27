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
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private const string ProfileDataModelErrorKey = "Profile_DataModelError";
        private const int InitialStatisticValue = 0;
        private const int MinimumRank = 1;

        private readonly INavigationManager _navigationManager;
        private readonly IProfileConnection _profileConnection;

        private bool _hasLoadedProfile;
        private bool _isEditingAvatar;
        private bool _isEditingNickname;
        private string _currentAvatarKey = AvatarCatalog.DefaultAvatarKey;
        private string _currentAvatar = AvatarCatalog.FindEmoji(AvatarCatalog.DefaultAvatarKey);
        private string _displayNickname;
        private string _email;
        private string _editingNickname;
        private string _lastPasswordChangeLabel;
        private int? _daysSincePasswordChange;
        private int? _totalWins;
        private int? _totalPoints;
        private int? _personalRank;
        private decimal? _winRate;

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

        public int? TotalWins
        {
            get { return _totalWins; }
            private set { SetProperty(ref _totalWins, value); }
        }

        public int? TotalPoints
        {
            get { return _totalPoints; }
            private set { SetProperty(ref _totalPoints, value); }
        }

        public int? PersonalRank
        {
            get { return _personalRank; }
            private set { SetProperty(ref _personalRank, value); }
        }

        public decimal? WinRate
        {
            get { return _winRate; }
            private set { SetProperty(ref _winRate, value); }
        }

        public void Activate()
        {
            Deactivate();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            NicknameError.Hide();
            _hasLoadedProfile = false;
            IsEditingAvatar = false;
            IsEditingNickname = false;
            TotalWins = null;
            TotalPoints = null;
            PersonalRank = null;
            WinRate = null;
            _daysSincePasswordChange = null;
            UpdateLastPasswordChangeLabel();
            DisplayNickname = string.Empty;
            Email = string.Empty;
            EditingNickname = string.Empty;
            ApplyAvatar(AvatarCatalog.DefaultAvatarKey);

            if (IsGuest)
            {
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
                _profileConnection.PlayerUserId, _profileConnection.PlayerNickname);
        }

        private void UpdateLastPasswordChangeLabel()
        {
            LastPasswordChangeLabel = string.Empty;

            if (_daysSincePasswordChange.HasValue)
            {
                string passwordFormat = LanguageManager.FindText(LastPasswordChangeFormatKey);
                LastPasswordChangeLabel = string.Format(passwordFormat, _daysSincePasswordChange.Value);
            }
        }

        private void LoadProfile()
        {
            OperationPlayerProfileResult profileResult = _profileConnection.GetPlayerProfile();

            if (profileResult == null || !profileResult.IsSuccess)
            {
                ShowProfileError(profileResult?.ErrorKey);
            }
            else if (!HasValidStatistics(profileResult))
            {
                ShowProfileError(ProfileDataModelErrorKey);
            }
            else
            {
                _hasLoadedProfile = true;
                NicknameError.Hide();
                DisplayNickname = profileResult.Nickname;
                Email = profileResult.Email;
                ApplyAvatar(profileResult.AvatarKey);
                _daysSincePasswordChange = Math.Max(InitialStatisticValue,
                    (DateTime.UtcNow - profileResult.PasswordChangedAtUtc).Days);
                UpdateLastPasswordChangeLabel();
                TotalWins = profileResult.TotalWins;
                TotalPoints = profileResult.TotalPoints;
                PersonalRank = profileResult.PersonalRank;
                WinRate = InitialStatisticValue;

                if (profileResult.TotalGamesPlayed.Value > InitialStatisticValue)
                {
                    WinRate = (decimal)profileResult.TotalWins.Value / profileResult.TotalGamesPlayed.Value;
                }
            }
        }

        private static bool HasValidStatistics(OperationPlayerProfileResult result)
        {
            bool hasValidStatistics = result.TotalWins.HasValue && result.TotalPoints.HasValue
                && result.TotalGamesPlayed.HasValue && result.PersonalRank.HasValue
                && result.TotalWins.Value >= InitialStatisticValue && result.TotalPoints.Value >= InitialStatisticValue
                && result.TotalGamesPlayed.Value >= result.TotalWins.Value && result.PersonalRank.Value >= MinimumRank;

            return hasValidStatistics;
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteToggleAvatarEditMode()
        {
            if (IsRegisteredPlayer && _hasLoadedProfile)
            {
                IsEditingAvatar = !IsEditingAvatar;
            }
        }

        private void ExecuteSelectAvatar(string avatarKey)
        {
            if (IsRegisteredPlayer && _hasLoadedProfile && !string.IsNullOrEmpty(avatarKey))
            {
                OperationResult updateResult = SaveProfile(DisplayNickname, avatarKey);

                if (updateResult.IsSuccess)
                {
                    NicknameError.Hide();
                    ApplyAvatar(avatarKey);
                    IsEditingAvatar = false;
                }
                else
                {
                    ShowProfileError(updateResult.ErrorKey);
                }
            }
        }

        private void ExecuteBeginNicknameEdit()
        {
            if (IsRegisteredPlayer && _hasLoadedProfile)
            {
                EditingNickname = DisplayNickname;
                NicknameError.Hide();
                IsEditingNickname = true;
            }
        }

        private void ExecuteSaveNickname()
        {
            if (IsRegisteredPlayer && _hasLoadedProfile)
            {
                string validationErrorKey = InputValidator.GetNicknameValidationErrorKey(EditingNickname);

                if (validationErrorKey == null)
                {
                    OperationResult updateResult = SaveProfile(EditingNickname, _currentAvatarKey);

                    if (updateResult.IsSuccess)
                    {
                        NicknameError.Hide();
                        DisplayNickname = EditingNickname;
                        IsEditingNickname = false;
                    }
                    else
                    {
                        ShowProfileError(updateResult.ErrorKey);
                    }
                }
                else
                {
                    ShowProfileError(validationErrorKey);
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

        private OperationResult SaveProfile(string nickname, string avatarKey)
        {
            OperationResult updateResult = _profileConnection.UpdatePlayerProfile(nickname, avatarKey);

            if (updateResult == null)
            {
                updateResult = new OperationResult { ErrorKey = UnexpectedErrorKey };
            }

            return updateResult;
        }

        private void ApplyAvatar(string avatarKey)
        {
            _currentAvatarKey = avatarKey;
            CurrentAvatar = AvatarCatalog.FindEmoji(avatarKey);
        }

        private void ShowProfileError(string errorKey)
        {
            if (string.IsNullOrWhiteSpace(errorKey))
            {
                errorKey = UnexpectedErrorKey;
            }

            NicknameError.ShowError(LanguageManager.FindText(errorKey));
        }
    }
}