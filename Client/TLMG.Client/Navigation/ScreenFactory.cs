using System;
using System.Collections.Generic;
using System.Windows;

using log4net;

using TLMG.Client.Communication;
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
            ILeaderboardConnection leaderboardConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;
            _friendsConnection = friendsConnection;
            _lobbyConnection = lobbyConnection;
            _leaderboardConnection = leaderboardConnection;
        }

        /// <inheritdoc />
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
                    screenEntry = BuildNewPasswordEntry();
                    break;
                case ScreenId.HowToPlay:
                    screenEntry = BuildHowToPlayEntry();
                    break;
                case ScreenId.Leaderboard:
                    screenEntry = BuildLeaderboardEntry();
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
                    { ScreenId.FugitiveKitten, () => new FugitiveKittenScreen() },
                    { ScreenId.Gameplay, () => new GameplayScreen() },
                    { ScreenId.RoundResult, () => new RoundResultScreen() },
                    { ScreenId.GameOver, () => new GameOverScreen() },
                    { ScreenId.MatchClosed, () => new MatchClosedScreen() }
                };

            return legacyScreens;
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

        private ScreenEntry BuildNewPasswordEntry()
        {
            NewPasswordViewModel newPasswordViewModel = new NewPasswordViewModel(_navigation);
            NewPasswordScreen newPasswordScreen = new NewPasswordScreen(newPasswordViewModel);

            return new ScreenEntry(newPasswordScreen, newPasswordViewModel);
        }

        private ScreenEntry BuildProfileEntry()
        {
            ProfileViewModel profileViewModel = new ProfileViewModel(_navigation);
            ProfileScreen profileScreen = new ProfileScreen(profileViewModel);

            return new ScreenEntry(profileScreen, profileViewModel);
        }

        private ScreenEntry BuildOptionsEntry()
        {
            OptionsViewModel optionsViewModel = new OptionsViewModel(_navigation);
            OptionsScreen optionsScreen = new OptionsScreen(optionsViewModel);

            return new ScreenEntry(optionsScreen, optionsViewModel);
        }

        private ScreenEntry BuildForgotPasswordEntry()
        {
            ForgotPasswordViewModel forgotPasswordViewModel = new ForgotPasswordViewModel(_navigation);
            ForgotPasswordScreen forgotPasswordScreen = new ForgotPasswordScreen(forgotPasswordViewModel);

            return new ScreenEntry(forgotPasswordScreen, forgotPasswordViewModel);
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

            if (verificationParameter == null)
            {
                _log.Warn("Se pidió la verificación sin indicar el flujo; no se cambió de pantalla.");
            }
            else if (verificationParameter.VerificationFlow == VerificationFlow.AccountRegistration)
            {
                verificationScreen = new VerificationScreen(FinishAccountRegistration, ReturnToCreateAccount);
            }
            else if (verificationParameter.VerificationFlow == VerificationFlow.PasswordRecovery)
            {
                verificationScreen = new VerificationScreen(ContinueToNewPassword, ReturnToForgotPassword);
            }
            else
            {
                _log.WarnFormat(
                    "Se pidió la verificación con el flujo {0}, que no tiene pantalla; no se cambió de pantalla.",
                    verificationParameter.VerificationFlow);
            }

            return new ScreenEntry(verificationScreen, null);
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
            _navigation.NavigateTo(ScreenId.NewPassword);
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