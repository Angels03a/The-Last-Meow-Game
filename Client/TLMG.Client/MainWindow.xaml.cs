using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using TLMG.Client.Views;

namespace TLMG.Client
{
    /// <summary>
    /// Ventana principal que contiene las pantallas y administra la navegación entre ellas.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConnectionLostMessageKey = "Common_ConnectionLostError";

        private readonly Stack<UserControl> _navigationHistory;
        private UserControl _currentScreen;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainWindow"/> y prepara la navegación inicial.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ((App)Application.Current).ServerConnection.ConnectionLost += OnServerConnectionLost;
            Closed += OnMainWindowClosed;
            _navigationHistory = new Stack<UserControl>();
            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += (eventSender, eventArguments) => ShowScreen(new LoginScreen(), false);
            ShowScreen(loadingScreen, false);
        }

        /// <summary>
        /// Muestra una nueva pantalla en el contenedor principal de la ventana, permitiendo
        /// registrarla en el historial de navegación.
        /// </summary>
        /// <param name="newScreen">Instancia del control de usuario que representa la pantalla a mostrar.</param>
        /// <param name="saveInHistory">
        /// Indica si la pantalla actual debe guardarse en la pila de historial para permitir retroceder.
        /// </param>
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
        /// Navega a la pantalla anterior registrada en el historial o muestra el menú principal
        /// si el historial está vacío.
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
        /// Limpia todo el historial de pantallas navegadas.
        /// </summary>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
        }

        private void OnMainWindowClosed(object sender, EventArgs eventArguments)
        {
            ((App)Application.Current).ServerConnection.ConnectionLost -= OnServerConnectionLost;
        }

        private void OnServerConnectionLost(object sender, EventArgs eventArguments)
        {
            ClearHistory();
            ShowScreen(new LoginScreen(), false);
            MessageBox.Show(TryFindResource(ConnectionLostMessageKey) as string);
        }
    }
}