using System.Windows;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.ViewModels;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace TLMG.Client
{
    /// <summary>
    /// Punto de entrada y lógica de inicialización general de la aplicación.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Inicializa una nueva instancia de la aplicación y configura el idioma inicial.
        /// </summary>
        public App()
        {
            LanguageManager.InitializeLanguage();
            ServerConnection = new ServerConnectionManager(Dispatcher);
        }

        /// <summary>
        /// Obtiene la conexión con el servidor, única para toda la aplicación. Las pantallas la alcanzan con
        /// ((App)Application.Current).ServerConnection.
        /// </summary>
        internal ServerConnectionManager ServerConnection { get; }

        /// <summary>
        /// Obtiene la navegación entre pantallas, única para toda la aplicación. Las pantallas aún no migradas la
        /// alcanzan con ((App)Application.Current).Navigation.
        /// </summary>
        internal NavigationManager Navigation { get; private set; }

        /// <summary>
        /// Compone la navegación, los diálogos y la fábrica de pantallas, y muestra la ventana principal.
        /// </summary>
        /// <param name="startupEventArguments">Datos del inicio de la aplicación.</param>
        protected override void OnStartup(StartupEventArgs startupEventArguments)
        {
            base.OnStartup(startupEventArguments);

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
            NavigationManager navigationManager = new NavigationManager(mainWindowViewModel);
            navigationManager.ScreenFactory = new ScreenFactory(
                navigationManager,
                new DialogManager(),
                ServerConnection,
                ServerConnection,
                ServerConnection,
                ServerConnection);
            Navigation = navigationManager;

            MainWindow shellWindow = new MainWindow(mainWindowViewModel);
            shellWindow.Show();
        }

        /// <summary>
        /// Cierra los canales con el servidor al salir de la aplicación, lo que termina la sesión del jugador.
        /// </summary>
        /// <param name="exitEventArguments">Datos de la salida de la aplicación.</param>
        protected override void OnExit(ExitEventArgs exitEventArguments)
        {
            ServerConnection.Disconnect();
            base.OnExit(exitEventArguments);
        }
    }
}