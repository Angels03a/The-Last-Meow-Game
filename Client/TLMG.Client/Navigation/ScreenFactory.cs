using System;
using System.Collections.Generic;

using log4net;

using TLMG.Client.Communication;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;
using TLMG.Client.Views;

namespace TLMG.Client.Navigation
{
    public class ScreenFactory : IScreenFactory
    {
        private const string AccountCreatedMessageKey = "CreateAccount_SuccessMessage";

        private static readonly ILog _log = LogManager.GetLogger(typeof(ScreenFactory));

        private static readonly HashSet<ScreenId> _accountScreens = new HashSet<ScreenId>
        {
            ScreenId.CreateAccount,
            ScreenId.Login,
            ScreenId.Verification,
            ScreenId.ForgotPassword,
            ScreenId.Profile,
            ScreenId.NewPassword,
            ScreenId.ChangePassword
        };

        private static readonly HashSet<ScreenId> _matchScreens = new HashSet<ScreenId>
        {
            ScreenId.Lobby,
            ScreenId.Gameplay,
            ScreenId.GameOver,
            ScreenId.RoundResult,
            ScreenId.FugitiveKitten,
            ScreenId.MatchClosed
        };

        private readonly INavigationManager _navigationManager;
        private readonly IDialogManager _dialogManager;
        private readonly IAccountsConnection _accountsConnection;
        private readonly IFriendsConnection _friendsConnection;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly IMatchConnection _matchConnection;
        private readonly ILeaderboardConnection _leaderboardConnection;
        private readonly IProfileConnection _profileConnection;
        private readonly IPasswordConnection _passwordConnection;
        private VerificationNavigationParameter _activeRecoveryParameter;

        public ScreenFactory(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection,
            IFriendsConnection friendsConnection,
            ILobbyConnection lobbyConnection,
            IMatchConnection matchConnection,
            ILeaderboardConnection leaderboardConnection,
            IProfileConnection profileConnection,
            IPasswordConnection passwordConnection)
        {
            _navigationManager = navigation;
            _dialogManager = dialogs;
            _accountsConnection = accountsConnection;
            _friendsConnection = friendsConnection;
            _lobbyConnection = lobbyConnection;
            _matchConnection = matchConnection;
            _leaderboardConnection = leaderboardConnection;
            _profileConnection = profileConnection;
            _passwordConnection = passwordConnection;
        }

        public ScreenEntry Create(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            if (_accountScreens.Contains(screenId))
            {
                screenEntry = CreateAccountScreenEntry(screenId, parameter);
            }
            else if (_matchScreens.Contains(screenId))
            {
                screenEntry = CreateMatchScreenEntry(screenId, parameter);
            }
            else
            {
                screenEntry = CreateGeneralScreenEntry(screenId, parameter);
            }

            return screenEntry;
        }

        private ScreenEntry CreateAccountScreenEntry(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            switch (screenId)
            {
                case ScreenId.CreateAccount:
                    CreateAccountViewModel createAccountViewModel = new CreateAccountViewModel(
                        _navigationManager, _dialogManager, _accountsConnection);
                    screenEntry = new ScreenEntry(new CreateAccountScreen(createAccountViewModel), createAccountViewModel);
                    break;
                case ScreenId.Login:
                    LoginViewModel loginViewModel = new LoginViewModel(
                        _accountsConnection, _navigationManager);
                    screenEntry = new ScreenEntry(new LoginScreen(loginViewModel), loginViewModel);
                    break;
                case ScreenId.Verification:
                    screenEntry = CreateVerificationEntry((VerificationNavigationParameter)parameter);
                    break;
                case ScreenId.ForgotPassword:
                    ForgotPasswordViewModel forgotPasswordViewModel = new ForgotPasswordViewModel(
                        _navigationManager, _passwordConnection);
                    screenEntry = new ScreenEntry(new ForgotPasswordScreen(forgotPasswordViewModel), forgotPasswordViewModel);
                    break;
                case ScreenId.Profile:
                    ProfileViewModel profileViewModel = new ProfileViewModel(_navigationManager, _profileConnection);
                    screenEntry = new ScreenEntry(new ProfileScreen(profileViewModel), profileViewModel);
                    break;
                case ScreenId.NewPassword:
                    NewPasswordNavigationParameter newPasswordParameter = (NewPasswordNavigationParameter)parameter;
                    NewPasswordViewModel newPasswordViewModel = new NewPasswordViewModel(
                        _navigationManager, _passwordConnection, newPasswordParameter);
                    screenEntry = new ScreenEntry(new NewPasswordScreen(newPasswordViewModel), newPasswordViewModel);
                    break;
                case ScreenId.ChangePassword:
                    ChangePasswordViewModel changePasswordViewModel = new ChangePasswordViewModel(
                        _navigationManager, _passwordConnection, _profileConnection);
                    screenEntry = new ScreenEntry(new ChangePasswordScreen(changePasswordViewModel), changePasswordViewModel);
                    break;
                default:
                    screenEntry = new ScreenEntry(null, null);
                    break;
            }

            return screenEntry;
        }

        private ScreenEntry CreateMatchScreenEntry(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            switch (screenId)
            {
                case ScreenId.Lobby:
                    LobbyViewModel lobbyViewModel = new LobbyViewModel(
                        _lobbyConnection, _navigationManager, _dialogManager);
                    screenEntry = new ScreenEntry(new LobbyScreen(lobbyViewModel), lobbyViewModel);
                    break;
                case ScreenId.Gameplay:
                    GameplayViewModel gameplayViewModel = new GameplayViewModel(
                        _navigationManager, _lobbyConnection, _matchConnection, _dialogManager);
                    screenEntry = new ScreenEntry(new GameplayScreen(gameplayViewModel), gameplayViewModel);
                    break;
                case ScreenId.GameOver:
                    GameOverViewModel gameOverViewModel = new GameOverViewModel(
                        _navigationManager, _lobbyConnection, _dialogManager, parameter as MatchView);
                    screenEntry = new ScreenEntry(new GameOverScreen(gameOverViewModel), gameOverViewModel);
                    break;
                case ScreenId.RoundResult:
                    RoundResultViewModel roundResultViewModel = new RoundResultViewModel(_navigationManager);
                    screenEntry = new ScreenEntry(new RoundResultScreen(roundResultViewModel), roundResultViewModel);
                    break;
                case ScreenId.FugitiveKitten:
                    Guid matchId = (Guid)parameter;
                    FugitiveKittenViewModel fugitiveKittenViewModel = new FugitiveKittenViewModel(
                        _navigationManager, _matchConnection, _lobbyConnection, matchId);
                    screenEntry = new ScreenEntry(new FugitiveKittenScreen(fugitiveKittenViewModel), fugitiveKittenViewModel);
                    break;
                case ScreenId.MatchClosed:
                    MatchClosedViewModel matchClosedViewModel = new MatchClosedViewModel(_navigationManager);
                    screenEntry = new ScreenEntry(new MatchClosedScreen(matchClosedViewModel), matchClosedViewModel);
                    break;
                default:
                    screenEntry = new ScreenEntry(null, null);
                    break;
            }

            return screenEntry;
        }

        private ScreenEntry CreateGeneralScreenEntry(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            switch (screenId)
            {
                case ScreenId.MainMenu:
                    MainMenuViewModel mainMenuViewModel = new MainMenuViewModel(
                        _accountsConnection, _navigationManager, _dialogManager);
                    screenEntry = new ScreenEntry(new MainMenuScreen(mainMenuViewModel), mainMenuViewModel);
                    break;
                case ScreenId.Friends:
                    FriendsViewModel friendsViewModel = new FriendsViewModel(_friendsConnection, _navigationManager);
                    screenEntry = new ScreenEntry(new FriendsScreen(friendsViewModel), friendsViewModel);
                    break;
                case ScreenId.Options:
                    OptionsViewModel optionsViewModel = new OptionsViewModel(_navigationManager);
                    screenEntry = new ScreenEntry(new OptionsScreen(optionsViewModel), optionsViewModel);
                    break;
                case ScreenId.HowToPlay:
                    HowToPlayViewModel howToPlayViewModel = new HowToPlayViewModel(_navigationManager);
                    screenEntry = new ScreenEntry(new HowToPlayScreen(howToPlayViewModel), howToPlayViewModel);
                    break;
                case ScreenId.Leaderboard:
                    LeaderboardViewModel leaderboardViewModel = new LeaderboardViewModel(
                        _leaderboardConnection, _navigationManager, _dialogManager);
                    screenEntry = new ScreenEntry(new LeaderboardScreen(leaderboardViewModel), leaderboardViewModel);
                    break;
                case ScreenId.Moderation:
                    ModerationNavigationParameter moderationParameter = (ModerationNavigationParameter)parameter;
                    ModerationViewModel moderationViewModel = new ModerationViewModel(
                        _navigationManager, moderationParameter.PlayerName, moderationParameter.TypeOfModerationMode);
                    screenEntry = new ScreenEntry(new ModerationScreen(moderationViewModel), moderationViewModel);
                    break;
                default:
                    _log.WarnFormat("No screen is registered for destination {0}.", screenId);
                    screenEntry = new ScreenEntry(null, null);
                    break;
            }

            return screenEntry;
        }

        private ScreenEntry CreateVerificationEntry(VerificationNavigationParameter verificationParameter)
        {
            ScreenEntry screenEntry;

            if (verificationParameter.VerificationFlow == VerificationFlow.AccountRegistration)
            {
                VerificationViewModel verificationViewModel = new VerificationViewModel(
                    _accountsConnection, _passwordConnection, verificationParameter);
                VerificationScreen verificationScreen = new VerificationScreen(
                    verificationViewModel, FinishAccountRegistration, ReturnToCreateAccount);
                screenEntry = new ScreenEntry(verificationScreen, verificationViewModel);
            }
            else if (verificationParameter.VerificationFlow == VerificationFlow.PasswordRecovery)
            {
                _activeRecoveryParameter = verificationParameter;
                VerificationViewModel verificationViewModel = new VerificationViewModel(
                    _accountsConnection, _passwordConnection, verificationParameter);
                VerificationScreen verificationScreen = new VerificationScreen(
                    verificationViewModel, ContinueToNewPassword, ReturnToForgotPassword);
                screenEntry = new ScreenEntry(verificationScreen, verificationViewModel);
            }
            else
            {
                _log.WarnFormat("No verification screen is registered for flow {0}.",
                    verificationParameter.VerificationFlow);
                screenEntry = new ScreenEntry(null, null);
            }

            return screenEntry;
        }

        private void FinishAccountRegistration()
        {
            _dialogManager.ShowMessage(AccountCreatedMessageKey);
            _navigationManager.NavigateToWithoutHistory(ScreenId.Login);
            _navigationManager.ClearHistory();
        }

        private void ReturnToCreateAccount()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.CreateAccount);
        }

        private void ContinueToNewPassword()
        {
            NewPasswordNavigationParameter newPasswordParameter = new NewPasswordNavigationParameter(
                _activeRecoveryParameter.TargetEmail, _activeRecoveryParameter.VerifiedCode);
            _navigationManager.NavigateTo(ScreenId.NewPassword, newPasswordParameter);
        }

        private void ReturnToForgotPassword()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.ForgotPassword);
        }
    }
}
