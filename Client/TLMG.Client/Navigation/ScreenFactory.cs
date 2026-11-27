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
        private readonly IModerationConnection _moderationConnection;
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
            IPasswordConnection passwordConnection,
            IModerationConnection moderationConnection)
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
            _moderationConnection = moderationConnection;
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

            if (screenId == ScreenId.CreateAccount)
            {
                CreateAccountViewModel createAccountViewModel = new CreateAccountViewModel(
                    _navigationManager, _dialogManager, _accountsConnection);
                screenEntry = new ScreenEntry(new CreateAccountScreen(createAccountViewModel), createAccountViewModel);
            }
            else if (screenId == ScreenId.Login)
            {
                LoginViewModel loginViewModel = new LoginViewModel(
                    _accountsConnection, _navigationManager, _dialogManager);
                screenEntry = new ScreenEntry(new LoginScreen(loginViewModel), loginViewModel);
            }
            else if (screenId == ScreenId.Verification)
            {
                screenEntry = CreateVerificationEntry(parameter as VerificationNavigationParameter);
            }
            else if (screenId == ScreenId.ForgotPassword)
            {
                ForgotPasswordViewModel forgotPasswordViewModel = new ForgotPasswordViewModel(
                    _navigationManager, _passwordConnection);
                screenEntry = new ScreenEntry(new ForgotPasswordScreen(forgotPasswordViewModel), forgotPasswordViewModel);
            }
            else if (screenId == ScreenId.Profile)
            {
                ProfileViewModel profileViewModel = new ProfileViewModel(_navigationManager, _profileConnection);
                screenEntry = new ScreenEntry(new ProfileScreen(profileViewModel), profileViewModel);
            }
            else if (screenId == ScreenId.NewPassword)
            {
                NewPasswordNavigationParameter newPasswordParameter = parameter as NewPasswordNavigationParameter;
                screenEntry = new ScreenEntry(null, null);
                if (newPasswordParameter != null)
                {
                    NewPasswordViewModel newPasswordViewModel = new NewPasswordViewModel(
                        _navigationManager, _passwordConnection, newPasswordParameter);
                    screenEntry = new ScreenEntry(new NewPasswordScreen(newPasswordViewModel), newPasswordViewModel);
                }
                else
                {
                    _log.Warn("Missing password recovery navigation parameter.");
                }
            }
            else if (screenId == ScreenId.ChangePassword)
            {
                ChangePasswordViewModel changePasswordViewModel = new ChangePasswordViewModel(
                    _navigationManager, _passwordConnection, _profileConnection);
                screenEntry = new ScreenEntry(new ChangePasswordScreen(changePasswordViewModel), changePasswordViewModel);
            }
            else
            {
                screenEntry = new ScreenEntry(null, null);
            }

            return screenEntry;
        }

        private ScreenEntry CreateMatchScreenEntry(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            if (screenId == ScreenId.Lobby)
            {
                LobbyViewModel lobbyViewModel = new LobbyViewModel(
                    _lobbyConnection, _navigationManager, _dialogManager);
                screenEntry = new ScreenEntry(new LobbyScreen(lobbyViewModel), lobbyViewModel);
            }
            else if (screenId == ScreenId.Gameplay)
            {
                GameplayViewModel gameplayViewModel = new GameplayViewModel(
                    _navigationManager, _lobbyConnection, _matchConnection, _dialogManager);
                screenEntry = new ScreenEntry(new GameplayScreen(gameplayViewModel), gameplayViewModel);
            }
            else if (screenId == ScreenId.GameOver)
            {
                GameOverViewModel gameOverViewModel = new GameOverViewModel(
                    _navigationManager, _lobbyConnection, _dialogManager, parameter as MatchView);
                screenEntry = new ScreenEntry(new GameOverScreen(gameOverViewModel), gameOverViewModel);
            }
            else if (screenId == ScreenId.RoundResult)
            {
                RoundResultViewModel roundResultViewModel = new RoundResultViewModel(_navigationManager);
                screenEntry = new ScreenEntry(new RoundResultScreen(roundResultViewModel), roundResultViewModel);
            }
            else if (screenId == ScreenId.FugitiveKitten)
            {
                screenEntry = new ScreenEntry(null, null);
                if (parameter is Guid matchId)
                {
                    FugitiveKittenViewModel fugitiveKittenViewModel = new FugitiveKittenViewModel(
                        _navigationManager, _matchConnection, _lobbyConnection, matchId);
                    screenEntry = new ScreenEntry(new FugitiveKittenScreen(fugitiveKittenViewModel), fugitiveKittenViewModel);
                }
                else
                {
                    _log.Warn("Missing fugitive kitten match identifier.");
                }
            }
            else if (screenId == ScreenId.MatchClosed)
            {
                MatchClosedViewModel matchClosedViewModel = new MatchClosedViewModel(_navigationManager);
                screenEntry = new ScreenEntry(new MatchClosedScreen(matchClosedViewModel), matchClosedViewModel);
            }
            else
            {
                screenEntry = new ScreenEntry(null, null);
            }

            return screenEntry;
        }

        private ScreenEntry CreateGeneralScreenEntry(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            if (screenId == ScreenId.MainMenu)
            {
                MainMenuViewModel mainMenuViewModel = new MainMenuViewModel(
                    _accountsConnection, _navigationManager, _dialogManager);
                screenEntry = new ScreenEntry(new MainMenuScreen(mainMenuViewModel), mainMenuViewModel);
            }
            else if (screenId == ScreenId.Friends)
            {
                BlockedPlayersViewModel blockedPlayersViewModel = new BlockedPlayersViewModel(
                    _friendsConnection, _moderationConnection, _dialogManager);
                FriendsViewModel friendsViewModel = new FriendsViewModel(
                    _friendsConnection, _navigationManager, blockedPlayersViewModel);
                screenEntry = new ScreenEntry(new FriendsScreen(friendsViewModel), friendsViewModel);
            }
            else if (screenId == ScreenId.Options)
            {
                OptionsViewModel optionsViewModel = new OptionsViewModel(_navigationManager);
                screenEntry = new ScreenEntry(new OptionsScreen(optionsViewModel), optionsViewModel);
            }
            else if (screenId == ScreenId.HowToPlay)
            {
                HowToPlayViewModel howToPlayViewModel = new HowToPlayViewModel(_navigationManager);
                screenEntry = new ScreenEntry(new HowToPlayScreen(howToPlayViewModel), howToPlayViewModel);
            }
            else if (screenId == ScreenId.Leaderboard)
            {
                LeaderboardViewModel leaderboardViewModel = new LeaderboardViewModel(
                    _leaderboardConnection, _navigationManager, _dialogManager);
                screenEntry = new ScreenEntry(new LeaderboardScreen(leaderboardViewModel), leaderboardViewModel);
            }
            else if (screenId == ScreenId.Moderation)
            {
                ModerationNavigationParameter moderationParameter = parameter as ModerationNavigationParameter;
                screenEntry = new ScreenEntry(null, null);
                if (moderationParameter != null)
                {
                    ModerationViewModel moderationViewModel = new ModerationViewModel(
                        _navigationManager, _moderationConnection, _dialogManager, moderationParameter, _lobbyConnection);
                    screenEntry = new ScreenEntry(new ModerationScreen(moderationViewModel), moderationViewModel);
                }
                else
                {
                    _log.Warn("Missing moderation navigation parameter.");
                }
            }
            else
            {
                _log.WarnFormat("No screen is registered for destination {0}.", screenId);
                screenEntry = new ScreenEntry(null, null);
            }

            return screenEntry;
        }

        private ScreenEntry CreateVerificationEntry(VerificationNavigationParameter verificationParameter)
        {
            ScreenEntry screenEntry;

            if (verificationParameter == null)
            {
                _log.Warn("Missing verification navigation parameter.");
                screenEntry = new ScreenEntry(null, null);
            }
            else if (verificationParameter.VerificationFlow == VerificationFlow.AccountRegistration)
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