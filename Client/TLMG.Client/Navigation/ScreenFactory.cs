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
        private static readonly ILog _log = LogManager.GetLogger(typeof(ScreenFactory));

        private static readonly Dictionary<ScreenId, Func<FrameworkElement>> _legacyScreens = CreateLegacyScreenMap();

        private readonly INavigationService _navigation;
        private readonly IDialogManager _dialogs;
        private readonly IAccountsConnection _accountsConnection;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ScreenFactory"/>.
        /// </summary>
        /// <param name="navigation">Servicio de navegación.</param>
        /// <param name="dialogs">Administrador de diálogos.</param>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        public ScreenFactory(
            INavigationService navigation,
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
                    { ScreenId.Gameplay, () => new GameplayScreen() },
                    { ScreenId.RoundResult, () => new RoundResultScreen() },
                    { ScreenId.GameOver, () => new GameOverScreen() },
                    { ScreenId.MatchClosed, () => new MatchClosedScreen() }
                };

            return legacyScreens;
        }

        private ScreenEntry Build(FrameworkElement view, ViewModelBase viewModel)
        {
            view.DataContext = viewModel;

            return new ScreenEntry(view, viewModel);
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