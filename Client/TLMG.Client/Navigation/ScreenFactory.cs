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

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ScreenFactory"/>.
        /// </summary>
        /// <param name="navigation">Administrador de navegación.</param>
        /// <param name="dialogs">Administrador de diálogos.</param>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        public ScreenFactory(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;
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
                case ScreenId.Gameplay:
                    GameplayViewModel gameplayViewModel = new GameplayViewModel(_navigation);
                    screenEntry = new ScreenEntry(new GameplayScreen(gameplayViewModel), gameplayViewModel);
                    break;
                case ScreenId.Moderation:
                    Tuple<string, ModerationMode> moderation = parameter as Tuple<string, ModerationMode>;
                    if (moderation == null)
                    {
                        _log.Warn("Se solicitó moderación sin parámetros; se conserva la pantalla actual.");
                        return null;
                    }
                    ModerationViewModel moderationViewModel = new ModerationViewModel(_navigation, moderation.Item1, moderation.Item2);
                    screenEntry = new ScreenEntry(new ModerationScreen(moderationViewModel), moderationViewModel);
                    break;
                case ScreenId.GameOver:
                    GameOverViewModel gameoverViewModel = new GameOverViewModel(_navigation);
                    screenEntry = new ScreenEntry(new GameOverScreen(gameoverViewModel), gameoverViewModel);
                    break;
                case ScreenId.RoundResult:
                    RoundResultViewModel roundresultViewModel = new RoundResultViewModel(_navigation);
                    screenEntry = new ScreenEntry(new RoundResultScreen(roundresultViewModel), roundresultViewModel);
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
                    { ScreenId.Friends, () => new FriendsScreen() },
                    { ScreenId.HowToPlay, () => new HowToPlayScreen() },
                    { ScreenId.Lobby, () => new LobbyScreen() },
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