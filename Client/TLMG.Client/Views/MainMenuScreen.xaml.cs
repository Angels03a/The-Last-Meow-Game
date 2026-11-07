using System.Windows;
using System.Windows.Input;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla del menú principal de navegación de la aplicación. Muestra el nickname del jugador en
    /// sesión; las victorias, los puntos, el puesto y la racha quedan ocultos hasta que el servidor los entregue
    /// (BL-103).
    /// </summary>
    public partial class MainMenuScreen : LocalizableScreen
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainMenuScreen"/>.
        /// </summary>
        public MainMenuScreen()
        {
            InitializeComponent();
            Focusable = true;
            Loaded += OnMainMenuScreenLoaded;
            KeyDown += OnMainMenuScreenKeyDown;
        }

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            // TODO: Mostrar lo que se decida para el invitado, que no tiene nickname; hoy queda vacío. (BL-089).
            lblPlayerNickname.Text = ((App)Application.Current).ServerConnection.PlayerNickname ?? string.Empty;
        }

        private void BtnLobby_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LobbyScreen());
        }

        private void BtnFriends_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new FriendsScreen());
        }
        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new ProfileScreen());
        }

        private void BtnHowToPlay_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new HowToPlayScreen());
        }

        private void BtnLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LeaderboardScreen());
        }

        private void BtnOptions_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new OptionsScreen());
        }

        private void OnMainMenuScreenLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        // TODO: Retirar el acceso temporal con F8 a FugitiveKittenScreen. (BL-122).
        // TODO: Abrir FugitiveKittenScreen al iniciar una partida de 8 cartas, no desde el menú. (BL-118).
        private void OnMainMenuScreenKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
            {
                ((MainWindow)Application.Current.MainWindow).ShowScreen(new FugitiveKittenScreen());
            }
        }

        private void BtnLogOut_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ServerConnection.Disconnect();
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}