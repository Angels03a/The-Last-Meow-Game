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

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ScreenFactory"/>.
        /// </summary>
        /// <param name="navigation">Administrador de navegación.</param>
        /// <param name="dialogs">Administrador de diálogos.</param>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        /// <param name="friendsConnection">Área de amigos de la conexión con el servidor.</param>
        /// <param name="lobbyConnection">Área de sala de la conexión con el servidor.</param>
        public ScreenFactory(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection,
            IFriendsConnection friendsConnection,
            ILobbyConnection lobbyConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;
            _friendsConnection = friendsConnection;
            _lobbyConnection = lobbyConnection;
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
                    { ScreenId.NewPassword, () => new NewPasswordScreen() },
                    { ScreenId.ForgotPassword, () => new ForgotPasswordScreen() },
                    { ScreenId.MainMenu, () => new MainMenuScreen() },
                    { ScreenId.Options, () => new OptionsScreen() },
                    { ScreenId.Profile, () => new ProfileScreen() },
                    { ScreenId.HowToPlay, () => new HowToPlayScreen() },
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

        private ScreenEntry BuildLobbyEntry()
        {
            LobbyViewModel lobbyViewModel = new LobbyViewModel(_lobbyConnection, _navigation, _dialogs);
            LobbyScreen lobbyScreen = new LobbyScreen(lobbyViewModel);

            return new ScreenEntry(lobbyScreen, lobbyViewModel);
        }

        private ScreenEntry BuildVerificationEntry(object parameter)
        {
            VerificationNavigationParameter verificationParameter = parameter as VerificationNavigationParameter;
            bool isAccountRegistration = verificationParameter != null
                && verificationParameter.VerificationFlow == VerificationFlow.AccountRegistration;
            VerificationScreen verificationScreen = null;

            if (isAccountRegistration)
            {
                verificationScreen = new VerificationScreen(FinishAccountRegistration, ReturnToCreateAccount);
            }
            else
            {
                _log.Warn("Se pidió la verificación sin el flujo de registro de cuenta; no se cambió de pantalla.");
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