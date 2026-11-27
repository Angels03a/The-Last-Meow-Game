using System.Windows;
using System.Windows.Threading;

using log4net;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.ViewModels;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace TLMG.Client
{
    public partial class App : Application
    {
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));
        private IDialogManager _dialogs;

        public App()
        {
            LanguageManager.InitializeLanguage();
            ServerConnection = new ServerConnectionManager(Dispatcher);
        }

        internal ServerConnectionManager ServerConnection { get; }
        internal NavigationManager Navigation { get; private set; }

        protected override void OnStartup(StartupEventArgs startupEventArguments)
        {
            base.OnStartup(startupEventArguments);

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
            NavigationManager navigationManager = new NavigationManager(mainWindowViewModel);
            _dialogs = new DialogManager();
            navigationManager.ScreenFactory = new ScreenFactory(
                navigationManager,
                _dialogs,
                ServerConnection,
                ServerConnection,
                ServerConnection,
                ServerConnection,
                ServerConnection,
                ServerConnection,
                ServerConnection,
                ServerConnection.Moderation);
            Navigation = navigationManager;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            MainWindow shellWindow = new MainWindow(mainWindowViewModel);
            shellWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs eventArguments)
        {
            _log.Error("Unexpected user interface failure.", eventArguments.Exception);
            ServerConnection.Disconnect();
            Navigation.ClearHistory();
            Navigation.NavigateToWithoutHistory(ScreenId.Login);
            _dialogs.ShowError(UnexpectedErrorKey);
            eventArguments.Handled = true;
        }

        protected override void OnExit(ExitEventArgs exitEventArguments)
        {
            DispatcherUnhandledException -= OnDispatcherUnhandledException;
            ServerConnection.Disconnect();
            base.OnExit(exitEventArguments);
        }
    }
}