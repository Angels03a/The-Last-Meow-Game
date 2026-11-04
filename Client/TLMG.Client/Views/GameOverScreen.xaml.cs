using System.Windows;
using System.Windows.Controls;

namespace TLMG.Client.Views
{
    public partial class GameOverScreen : LocalizableScreen
    {
        private const string WinnerTitleFormatKey = "GameOver_WinnerTitleFormat";
        private const string DefaultFormat = "{0}";
        private const string WinnerName = "NyanProwler";
        private const bool SaveScreenInHistory = false;

        public GameOverScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
            string winnerTitleFormat = TryFindResource(WinnerTitleFormatKey) as string ?? DefaultFormat;
            lblWinnerTitle.Text = string.Format(winnerTitleFormat, WinnerName.ToUpperInvariant());
        }

        private void BtnRematch_Click(object sender, RoutedEventArgs e)
        {
            NavigateToScreen(new GameplayScreen());
        }

        private void BtnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToScreen(new MainMenuScreen());
        }

        private void NavigateToScreen(UserControl targetScreen)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(targetScreen, SaveScreenInHistory);
        }
    }
}