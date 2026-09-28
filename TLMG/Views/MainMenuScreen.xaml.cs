using System.Windows;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla del menú principal de navegación de la aplicación.
    /// </summary>
    public partial class MainMenuScreen : LocalizableScreen
    {
        private const int DefaultWinStreakCount = 3;
        private const int DefaultPointsToNextRank = 120;
        private const string WinStreakFormatResourceKey = "MainMenu_WinStreakValue";
        private const string NextRankFormatResourceKey = "MainMenu_NextRankValue";

        public MainMenuScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
            string winStreakFormat = TryFindResource(WinStreakFormatResourceKey) as string ?? "{0}";
            lblWinStreakValue.Text = string.Format(winStreakFormat, DefaultWinStreakCount);

            string nextRankFormat = TryFindResource(NextRankFormatResourceKey) as string ?? "{0}";
            lblNextRankValue.Text = string.Format(nextRankFormat, DefaultPointsToNextRank);
        }

        private void BtnNav_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            // TODO: Implementar botón de navegación cuando se construyan las pantallas correspondientes.
        }

        private void BtnHowToPlay_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new HowToPlayScreen());
        }

        private void BtnLeaderboard_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LeaderboardScreen());
        }

        private void BtnOptions_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new OptionsScreen());
        }

        private void BtnLogOut_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}