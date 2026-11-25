using System;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly IAccountsConnection _accountsConnection;
        private readonly INavigationManager _navigationManager;
        private readonly IDialogManager _dialogs;

        public MainMenuViewModel(
            IAccountsConnection accountsConnection,
            INavigationManager navigationManager,
            IDialogManager dialogs)
        {
            _accountsConnection = accountsConnection;
            _navigationManager = navigationManager;
            _dialogs = dialogs;

            IsGuest = _accountsConnection.IsGuestSession;
            IsRegisteredPlayer = !IsGuest;

            NavigateToLobbyCommand = new RelayCommand(ExecuteNavigateToLobby);
            NavigateToFriendsCommand = new RelayCommand(ExecuteNavigateToFriends);
            NavigateToProfileCommand = new RelayCommand(ExecuteNavigateToProfile);
            NavigateToHowToPlayCommand = new RelayCommand(ExecuteNavigateToHowToPlay);
            NavigateToLeaderboardCommand = new RelayCommand(ExecuteNavigateToLeaderboard);
            NavigateToOptionsCommand = new RelayCommand(ExecuteNavigateToOptions);
            LogOutCommand = new RelayCommand(ExecuteLogOut);
        }

        public ICommand NavigateToLobbyCommand { get; }

        public ICommand NavigateToFriendsCommand { get; }

        public ICommand NavigateToProfileCommand { get; }

        public ICommand NavigateToHowToPlayCommand { get; }

        public ICommand NavigateToLeaderboardCommand { get; }

        public ICommand NavigateToOptionsCommand { get; }

        public ICommand LogOutCommand { get; }

        public string PlayerNickname => DisplayNameHelper.Resolve(
            _accountsConnection.PlayerUserId,
            _accountsConnection.PlayerNickname ?? string.Empty);

        public bool IsGuest { get; }

        public bool IsRegisteredPlayer { get; }

        public void Activate()
        {
            Deactivate();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            OnPropertyChanged(nameof(PlayerNickname));
        }

        public void Deactivate()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        protected override void OnDisposed()
        {
            Deactivate();
            base.OnDisposed();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArguments)
        {
            OnPropertyChanged(nameof(PlayerNickname));
        }

        private void ExecuteNavigateToLobby()
        {
            _navigationManager.NavigateTo(ScreenId.Lobby);
        }

        private void ExecuteNavigateToFriends()
        {
            _navigationManager.NavigateTo(ScreenId.Friends);
        }

        private void ExecuteNavigateToProfile()
        {
            _navigationManager.NavigateTo(ScreenId.Profile);
        }

        private void ExecuteNavigateToHowToPlay()
        {
            _navigationManager.NavigateTo(ScreenId.HowToPlay);
        }

        private void ExecuteNavigateToLeaderboard()
        {
            _navigationManager.NavigateTo(ScreenId.Leaderboard);
        }

        private void ExecuteNavigateToOptions()
        {
            _navigationManager.NavigateTo(ScreenId.Options);
        }

        private void ExecuteLogOut()
        {
            LogOutResult logOutResult = _accountsConnection.LogOut();

            if (logOutResult.IsSuccess)
            {
                _navigationManager.NavigateToWithoutHistory(ScreenId.Login);
                _navigationManager.ClearHistory();
            }
            else
            {
                _dialogs.ShowError(logOutResult.ErrorKey);
            }
        }
    }
}