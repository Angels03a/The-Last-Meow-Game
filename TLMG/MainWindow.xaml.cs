using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TLMG.Views;

namespace TLMG
{
    /// <summary>
    /// Ventana principal que gestiona la navegación entre pantallas mediante su contenedor central.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Stack<UserControl> _navigationHistory;
        private UserControl _currentScreen;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainWindow"/> y carga la pantalla inicial de carga.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _navigationHistory = new Stack<UserControl>();

            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += (eventSender, eventArguments) => ShowScreen(new LoginScreen(), false);
            ShowScreen(loadingScreen, false);
        }

        /// <summary>
        /// Muestra el control de usuario especificado dentro del contenedor principal de la ventana y lo agrega al historial si es necesario.
        /// </summary>
        /// <param name="newScreen">Control de usuario correspondiente a la pantalla a desplegar.</param>
        /// <param name="saveInHistory">Indica si la pantalla actual debe guardarse en el historial antes de cambiar.</param>
        public void ShowScreen(UserControl newScreen, bool saveInHistory = true)
        {
            if (saveInHistory && _currentScreen != null)
            {
                _navigationHistory.Push(_currentScreen);
            }

            _currentScreen = newScreen;
            grdMainContainer.Children.Clear();
            grdMainContainer.Children.Add(_currentScreen);
        }

        /// <summary>
        /// Regresa a la pantalla anterior almacenada en el historial de navegación.
        /// </summary>
        public void GoBack()
        {
            if (_navigationHistory.Count > 0)
            {
                _currentScreen = _navigationHistory.Pop();
                grdMainContainer.Children.Clear();
                grdMainContainer.Children.Add(_currentScreen);
            }
            else
            {
                ShowScreen(new MainMenuScreen(), false);
            }
        }

        /// <summary>
        /// Limpia el historial de navegación de la ventana principal.
        /// </summary>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
        }
    }
}