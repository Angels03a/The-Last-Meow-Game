using System.Windows;
using TLMG.Client.Communication;
using TLMG.Client.Services;
using TLMG.Client.ViewModels;
using TLMG.Client.Views;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Raíz de composición de las pantallas: crea cada vista con su ViewModel y sus dependencias.
    /// </summary>
    public class ScreenFactory : IScreenFactory
    {
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;
        private readonly ILocalizationService _localizer;
        private readonly ITimerFactory _timers;
        private readonly IAccountsConnection _accountsConnection;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ScreenFactory"/>.
        /// </summary>
        /// <param name="navigation">Servicio de navegación.</param>
        /// <param name="dialogs">Servicio de diálogos.</param>
        /// <param name="localizer">Servicio de localización.</param>
        /// <param name="timers">Fábrica de temporizadores.</param>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        public ScreenFactory(
            INavigationService navigation,
            IDialogService dialogs,
            ILocalizationService localizer,
            ITimerFactory timers,
            IAccountsConnection accountsConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _localizer = localizer;
            _timers = timers;
            _accountsConnection = accountsConnection;
        }

        public ScreenEntry Create(ScreenId screenId, object parameter)
        {
            ScreenEntry screenEntry;

            switch (screenId)
            {
                case ScreenId.Friends:
                    screenEntry = Build(
                        new FriendsScreen(),
                        new FriendsViewModel(_navigation, _localizer,
                        new FriendsService(), _timers));
                    break;
                case ScreenId.Gameplay:
                    screenEntry = Build(
                        new GameplayScreen(),
                        new GameplayViewModel(_navigation, _localizer));
                    break;
                case ScreenId.GameOver:
                    screenEntry = Build(
                        new GameOverScreen(),
                        new GameOverViewModel(_navigation, _localizer));
                    break;
                case ScreenId.ForgotPassword:
                    screenEntry = Build(
                        new ForgotPasswordScreen(),
                        new ForgotPasswordViewModel(_navigation, _localizer, _timers,
                        new PasswordRecoveryService()));
                    break;
                case ScreenId.MatchClosed:
                    screenEntry = Build(
                        new MatchClosedScreen(),
                        new MatchClosedViewModel(_navigation, _localizer));
                    break;
                case ScreenId.CreateAccount:
                    screenEntry = Build(
                        new CreateAccountScreen(),
                        new CreateAccountViewModel(
                            _navigation,
                            _dialogs,
                            _accountsConnection));
                    break;
                case ScreenId.Login:
                    screenEntry = Build(
                        new LoginScreen(),
                        new LoginViewModel(
                            _accountsConnection,
                            _navigation));
                    break;
                default:
                    screenEntry = CreateLegacyScreen(screenId);
                    break;
            }

            return screenEntry;
        }

        private ScreenEntry Build(FrameworkElement view, ViewModelBase viewModel)
        {
            view.DataContext = viewModel;
            return new ScreenEntry(view, viewModel);
        }

        private ScreenEntry CreateLegacyScreen(ScreenId screenId)
        {
            FrameworkElement legacyView = null;

            switch (screenId)
            {
                case ScreenId.NewPassword:
                    legacyView = new NewPasswordScreen();
                    break;
                case ScreenId.MainMenu:
                    legacyView = new MainMenuScreen();
                    break;
                case ScreenId.Options:
                    legacyView = new OptionsScreen();
                    break;
                case ScreenId.Profile:
                    legacyView = new ProfileScreen();
                    break;
                case ScreenId.HowToPlay:
                    legacyView = new HowToPlayScreen();
                    break;
                case ScreenId.Lobby:
                    legacyView = new LobbyScreen();
                    break;
                case ScreenId.RoundResult:
                    legacyView = new RoundResultScreen();
                    break;
                default:
                    break;
            }

            return new ScreenEntry(legacyView, null);
        }
    }
}