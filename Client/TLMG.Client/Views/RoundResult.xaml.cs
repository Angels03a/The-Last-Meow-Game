using System.Windows;
using System.Windows.Controls;
using TLMG.Client.Navigation;

namespace TLMG.Client.Views
{
    public partial class RoundResultScreen : LocalizableScreen
    {
        private const string WinnerTitleFormatKey = "RoundResult_WinnerTitleFormat";
        private const string BidCompletedFormatKey = "RoundResult_BidCompletedFormat";
        private const string EliminatedTitleFormatKey = "RoundResult_EliminatedTitleFormat";
        private const string ScoresTitleFormatKey = "RoundResult_ScoresTitleFormat";
        private const string DefaultFormat = "{0}";
        private const string WinnerName = "WhiskerKnight";
        private const string EliminatedPlayerName = "GraveyardPaws";
        private const int CompletedBidCardCount = 4;
        private const int TargetWinsCount = 2;
        private const bool SaveScreenInHistory = false;

        public RoundResultScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
            string winnerTitleFormat = TryFindResource(WinnerTitleFormatKey) as string ?? DefaultFormat;
            string bidCompletedFormat = TryFindResource(BidCompletedFormatKey) as string ?? DefaultFormat;
            string eliminatedTitleFormat = TryFindResource(EliminatedTitleFormatKey) as string ?? DefaultFormat;
            string scoresTitleFormat = TryFindResource(ScoresTitleFormatKey) as string ?? DefaultFormat;

            lblWinnerTitle.Text = string.Format(winnerTitleFormat, WinnerName.ToUpperInvariant());
            lblBidCompleted.Text = string.Format(bidCompletedFormat, CompletedBidCardCount);
            lblEliminatedTitle.Text = string.Format(eliminatedTitleFormat, EliminatedPlayerName);
            lblScoresTitle.Text = string.Format(scoresTitleFormat, TargetWinsCount);
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            NavigateToScreen(new GameplayScreen());
        }

        private void BtnBackToMenu_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }

        private void NavigateToScreen(UserControl targetScreen)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(targetScreen, SaveScreenInHistory);
        }
    }
}