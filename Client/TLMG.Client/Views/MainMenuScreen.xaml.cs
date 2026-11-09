using System;
using System.Windows;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla del menú principal de navegación de la aplicación. 
    /// Muestra el nickname del jugador en sesión y delega la navegación al ViewModel.
    /// </summary>
    public partial class MainMenuScreen : LocalizableScreen
    {
        private readonly MainMenuViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainMenuScreen"/>.
        /// </summary>
        public MainMenuScreen()
        {
            InitializeComponent();

            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel;

            _viewModel.LobbyRequested += OnLobbyRequested;
            _viewModel.FriendsRequested += OnFriendsRequested;
            _viewModel.ProfileRequested += OnProfileRequested;
            _viewModel.HowToPlayRequested += OnHowToPlayRequested;
            _viewModel.LeaderboardRequested += OnLeaderboardRequested;
            _viewModel.OptionsRequested += OnOptionsRequested;
            _viewModel.LogOutRequested += OnLogOutRequested;

            Unloaded += OnScreenUnloaded;
        }

        protected override void UpdateDynamicLabels()
        {
            // TODO: Mostrar lo que se decida para el invitado, que no tiene nickname; hoy queda vacío. (BL-089).
            _viewModel.PlayerNickname = ((App)Application.Current).ServerConnection.PlayerNickname ?? string.Empty;
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LobbyRequested -= OnLobbyRequested;
            _viewModel.FriendsRequested -= OnFriendsRequested;
            _viewModel.ProfileRequested -= OnProfileRequested;
            _viewModel.HowToPlayRequested -= OnHowToPlayRequested;
            _viewModel.LeaderboardRequested -= OnLeaderboardRequested;
            _viewModel.OptionsRequested -= OnOptionsRequested;
            _viewModel.LogOutRequested -= OnLogOutRequested;
        }

        private void OnLobbyRequested(object sender, EventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LobbyScreen());
        }

        private void OnFriendsRequested(object sender, EventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new FriendsScreen());
        }

        private void OnProfileRequested(object sender, EventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new ProfileScreen());
        }

        private void OnHowToPlayRequested(object sender, EventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new HowToPlayScreen());
        }

        private void OnLeaderboardRequested(object sender, EventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LeaderboardScreen());
        }

        private void OnOptionsRequested(object sender, EventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new OptionsScreen());
        }

        private void OnLogOutRequested(object sender, EventArgs e)
        {
            ((App)Application.Current).ServerConnection.Disconnect();
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}