using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista del podio y la tabla de posiciones de los jugadores (leaderboard). No tiene lógica propia: la consulta
    /// al servidor, el podio, los textos y la navegación viven en <see cref="LeaderboardViewModel"/>.
    /// </summary>
    public partial class LeaderboardScreen : LocalizableScreen
    {
        private readonly LeaderboardViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="leaderboardViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public LeaderboardScreen(LeaderboardViewModel leaderboardViewModel)
        {
            InitializeComponent();

            _viewModel = leaderboardViewModel;
            DataContext = leaderboardViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Activate();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Deactivate();
        }
    }
}
