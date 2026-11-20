using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Displays the podium and leaderboard. <see cref="LeaderboardViewModel"/> handles the server query, podium, text
    /// and navigation.
    /// </summary>
    public partial class LeaderboardScreen : LocalizableScreen
    {
        private readonly LeaderboardViewModel _viewModel;

        /// <summary>
        /// Creates a <see cref="LeaderboardScreen"/> with its ViewModel.
        /// </summary>
        /// <param name="leaderboardViewModel">ViewModel created and supplied by the screen factory.</param>
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
