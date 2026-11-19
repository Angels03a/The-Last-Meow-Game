using System;
using System.Collections.Generic;
using System.Windows;

using log4net;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.ViewModels;
using TLMG.Client.Views;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Raíz de composición de las pantallas: crea cada vista con su ViewModel y sus dependencias.
    /// </summary>
    public class ScreenFactory : IScreenFactory
    {
        private const string AccountCreatedMessageKey = "CreateAccount_SuccessMessage";

        private static readonly ILog _log = LogManager.GetLogger(typeof(ScreenFactory));

        private static readonly Dictionary<ScreenId, Func<FrameworkElement>> _legacyScreens = CreateLegacyScreenMap();

        private readonly INavigationManager _navigation;
        private readonly IDialogManager _dialogs;
        private readonly IAccountsConnection _accountsConnection;
        private readonly IFriendsConnection _friendsConnection;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly ILeaderboardConnection _leaderboardConnection;
        private readonly IProfileConnection _profileConnection;
        private readonly IPasswordConnection _passwordConnection;
        private VerificationNavigationParameter _activeRecoveryParameter;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ScreenFactory"/>.
        /// </summary>
        /// <param name="navigation">Administrador de navegación.</param>
        /// <param name="dialogs">Administrador de diálogos.</param>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        /// <param name="friendsConnection">Área de amigos de la conexión con el servidor.</param>
        /// <param name="lobbyConnection">Área de sala de la conexión con el servidor.</param>
        /// <param name="leaderboardConnection">Área de la tabla de posiciones de la conexión con el servidor.</param>
        public ScreenFactory(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection,
            IFriendsConnection friendsConnection,
            ILobbyConnection lobbyConnection,
            ILeaderboardConnection leaderboardConnection,
            IProfileConnection profileConnection,
            IPasswordConnection passwordConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;
            _friendsConnection = friendsConnection;
            _lobbyConnection = lobbyConnection;
            _leaderboardConnection = leaderboardConnection;
            _profileConnection = profileConnection;
            _passwordConnection = passwordConnection;
        }

        public ScreenEntry Create(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            switch (screenId)
            {
                case ScreenId.CreateAccount:
                    screenEntry = BuildCreateAccountEntry();
                    break;
                case ScreenId.Login:
                    screenEntry = BuildLoginEntry();
                    break;
                case ScreenId.Verification:
                    screenEntry = BuildVerificationEntry(parameter);
                    break;
                case ScreenId.Friends:
                    screenEntry = BuildFriendsEntry();
                    break;
                case ScreenId.Lobby:
                    screenEntry = BuildLobbyEntry();
                    break;
                case ScreenId.MainMenu:
                    screenEntry = BuildMainMenuEntry();
                    break;
                case ScreenId.ForgotPassword:
                    screenEntry = BuildForgotPasswordEntry();
                    break;
                case ScreenId.Options:
                    screenEntry = BuildOptionsEntry();
                    break;
                case ScreenId.Profile:
                    screenEntry = BuildProfileEntry();
                    break;
                case ScreenId.NewPassword:
                    screenEntry = BuildNewPasswordEntry(parameter);
                    break;
                case ScreenId.ChangePassword:
                    screenEntry = BuildChangePasswordEntry();
                    break;
                case ScreenId.HowToPlay:
                    screenEntry = BuildHowToPlayEntry();
                    break;
                case ScreenId.Leaderboard:
                    screenEntry = BuildLeaderboardEntry();
                    break;
                case ScreenId.Gameplay:
                    GameplayViewModel gameplayViewModel = new GameplayViewModel(_navigation, _lobbyConnection);
                    screenEntry = new ScreenEntry(new GameplayScreen(gameplayViewModel), gameplayViewModel);
                    break;
                case ScreenId.Moderation:
                    Tuple<string, ModerationMode> moderation = parameter as Tuple<string, ModerationMode>;
                    if (moderation == null)
                    {
                        _log.Warn("Se solicitó moderación sin parámetros; se conserva la pantalla actual.");
                        return null;
                    }
                    ModerationViewModel moderationViewModel = new ModerationViewModel(
                        _navigation,
                        moderation.Item1,
                        moderation.Item2);
                    screenEntry = new ScreenEntry(new ModerationScreen(moderationViewModel), moderationViewModel);
                    break;
                case ScreenId.GameOver:
                    GameOverViewModel gameOverViewModel = new GameOverViewModel(_navigation);
                    screenEntry = new ScreenEntry(new GameOverScreen(gameOverViewModel), gameOverViewModel);
                    break;
                case ScreenId.RoundResult:
                    RoundResultViewModel roundResultViewModel = new RoundResultViewModel(_navigation);
                    screenEntry = new ScreenEntry(new RoundResultScreen(roundResultViewModel), roundResultViewModel);
                    break;
                case ScreenId.MatchClosed:
                    MatchClosedViewModel matchClosedViewModel = new MatchClosedViewModel(_navigation);
                    screenEntry = new ScreenEntry(new MatchClosedScreen(matchClosedViewModel), matchClosedViewModel);
                    break;
                default:
                    screenEntry = CreateLegacyScreen(screenId);
                    break;
            }

            return screenEntry;
        }

        private static Dictionary<ScreenId, Func<FrameworkElement>> CreateLegacyScreenMap()
        {
            Dictionary<ScreenId, Func<FrameworkElement>> legacyScreens =
                new Dictionary<ScreenId, Func<FrameworkElement>>
                {
                    { ScreenId.FugitiveKitten, () => new FugitiveKittenScreen() }
                };

            return legacyScreens;
        }

        private ScreenEntry BuildForgotPasswordEntry()
        {
            ForgotPasswordViewModel forgotPasswordViewModel =
                new ForgotPasswordViewModel(_navigation, _passwordConnection);
            ForgotPasswordScreen forgotPasswordScreen = new ForgotPasswordScreen(forgotPasswordViewModel);

            return new ScreenEntry(forgotPasswordScreen, forgotPasswordViewModel);
        }

        private ScreenEntry BuildCreateAccountEntry()
        {
            CreateAccountViewModel createAccountViewModel = new CreateAccountViewModel(
                _navigation,
                _dialogs,
                _accountsConnection);
            CreateAccountScreen createAccountScreen = new CreateAccountScreen(createAccountViewModel);

            return new ScreenEntry(createAccountScreen, createAccountViewModel);
        }

        private ScreenEntry BuildLoginEntry()
        {
            LoginViewModel loginViewModel = new LoginViewModel(_accountsConnection, _navigation);
            LoginScreen loginScreen = new LoginScreen(loginViewModel);

            return new ScreenEntry(loginScreen, loginViewModel);
        }

        private ScreenEntry BuildFriendsEntry()
        {
            FriendsViewModel friendsViewModel = new FriendsViewModel(_friendsConnection, _navigation);
            FriendsScreen friendsScreen = new FriendsScreen(friendsViewModel);

            return new ScreenEntry(friendsScreen, friendsViewModel);
        }

        private ScreenEntry BuildLeaderboardEntry()
        {
            LeaderboardViewModel leaderboardViewModel = new LeaderboardViewModel(
                _leaderboardConnection,
                _navigation,
                _dialogs);
            LeaderboardScreen leaderboardScreen = new LeaderboardScreen(leaderboardViewModel);

            return new ScreenEntry(leaderboardScreen, leaderboardViewModel);
        }

        private ScreenEntry BuildHowToPlayEntry()
        {
            HowToPlayViewModel howToPlayViewModel = new HowToPlayViewModel(_navigation);
            HowToPlayScreen howToPlayScreen = new HowToPlayScreen(howToPlayViewModel);

            return new ScreenEntry(howToPlayScreen, howToPlayViewModel);
        }

        private ScreenEntry BuildNewPasswordEntry(object parameter)
        {
            NewPasswordNavigationParameter newPasswordParameter = parameter as NewPasswordNavigationParameter;
            NewPasswordScreen newPasswordScreen = null;
            NewPasswordViewModel newPasswordViewModel = null;

            if (newPasswordParameter == null)
            {
                _log.Warn("Se pidió la nueva contraseña sin correo ni código; no se cambió de pantalla.");
            }
            else
            {
                newPasswordViewModel = new NewPasswordViewModel(_navigation, _passwordConnection, newPasswordParameter);
                newPasswordScreen = new NewPasswordScreen(newPasswordViewModel);
            }

            return new ScreenEntry(newPasswordScreen, newPasswordViewModel);
        }

        private ScreenEntry BuildChangePasswordEntry()
        {
            ChangePasswordViewModel changePasswordViewModel = new ChangePasswordViewModel(
                _navigation,
                _passwordConnection,
                _profileConnection);
            ChangePasswordScreen changePasswordScreen = new ChangePasswordScreen(changePasswordViewModel);

            return new ScreenEntry(changePasswordScreen, changePasswordViewModel);
        }

        private ScreenEntry BuildProfileEntry()
        {
            ProfileViewModel profileViewModel = new ProfileViewModel(_navigation, _profileConnection);
            ProfileScreen profileScreen = new ProfileScreen(profileViewModel);

            return new ScreenEntry(profileScreen, profileViewModel);
        }

        private ScreenEntry BuildOptionsEntry()
        {
            OptionsViewModel optionsViewModel = new OptionsViewModel(_navigation);
            OptionsScreen optionsScreen = new OptionsScreen(optionsViewModel);

            return new ScreenEntry(optionsScreen, optionsViewModel);
        }

        private ScreenEntry BuildMainMenuEntry()
        {
            MainMenuViewModel mainMenuViewModel = new MainMenuViewModel(_accountsConnection, _navigation);
            MainMenuScreen mainMenuScreen = new MainMenuScreen(mainMenuViewModel);

            return new ScreenEntry(mainMenuScreen, mainMenuViewModel);
        }

        private ScreenEntry BuildLobbyEntry()
        {
            LobbyViewModel lobbyViewModel = new LobbyViewModel(_lobbyConnection, _navigation, _dialogs);
            LobbyScreen lobbyScreen = new LobbyScreen(lobbyViewModel);

            return new ScreenEntry(lobbyScreen, lobbyViewModel);
        }

        private ScreenEntry BuildVerificationEntry(object parameter)
        {
            VerificationNavigationParameter verificationParameter = parameter as VerificationNavigationParameter;
            VerificationScreen verificationScreen = null;
            VerificationViewModel verificationViewModel = null;

            if (verificationParameter == null)
            {
                _log.Warn("Se pidió la verificación sin indicar el flujo; no se cambió de pantalla.");
            }
            else if (verificationParameter.VerificationFlow == VerificationFlow.AccountRegistration)
            {
                verificationViewModel = new VerificationViewModel(
                    _accountsConnection,
                    _passwordConnection,
                    verificationParameter);
                verificationScreen = new VerificationScreen(
                    verificationViewModel,
                    FinishAccountRegistration,
                    ReturnToCreateAccount);
            }
            else if (verificationParameter.VerificationFlow == VerificationFlow.PasswordRecovery)
            {
                _activeRecoveryParameter = verificationParameter;
                verificationViewModel = new VerificationViewModel(
                    _accountsConnection,
                    _passwordConnection,
                    verificationParameter);
                verificationScreen = new VerificationScreen(
                    verificationViewModel,
                    ContinueToNewPassword,
                    ReturnToForgotPassword);
            }
            else
            {
                _log.WarnFormat(
                    "Se pidió la verificación con el flujo {0}, que no tiene pantalla; no se cambió de pantalla.",
                    verificationParameter.VerificationFlow);
            }

            return new ScreenEntry(verificationScreen, verificationViewModel);
        }

        private void FinishAccountRegistration()
        {
            _dialogs.ShowMessage(AccountCreatedMessageKey);
            _navigation.NavigateToWithoutHistory(ScreenId.Login);
            _navigation.ClearHistory();
        }

        private void ReturnToCreateAccount()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.CreateAccount);
        }

        private void ContinueToNewPassword()
        {
            NewPasswordNavigationParameter newPasswordParameter = new NewPasswordNavigationParameter(
                _activeRecoveryParameter.TargetEmail,
                _activeRecoveryParameter.VerifiedCode);

            _navigation.NavigateTo(ScreenId.NewPassword, newPasswordParameter);
        }

        private void ReturnToForgotPassword()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.ForgotPassword);
        }

        private ScreenEntry CreateLegacyScreen(ScreenId screenId)
        {
            FrameworkElement legacyView = null;
            Func<FrameworkElement> createLegacyView;

            if (_legacyScreens.TryGetValue(screenId, out createLegacyView))
            {
                legacyView = createLegacyView();
            }
            else
            {
                _log.WarnFormat(
                    "No hay una pantalla registrada para el destino {0}; no se cambió de pantalla.",
                    screenId);
            }

            return new ScreenEntry(legacyView, null);
        }
    }
}