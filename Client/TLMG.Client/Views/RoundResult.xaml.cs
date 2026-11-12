using System.Windows;
using TLMG.Client.ViewModels;
using System.Windows.Controls;

namespace TLMG.Client.Views
{
    public partial class RoundResultScreen : LocalizableScreen
    {
        private const string WinnerTitleFormatKey = "RoundResult_WinnerTitleFormat";
        private const string BidCompletedFormatKey = "RoundResult_BidCompletedFormat";
        private const string EliminatedTitleFormatKey = "RoundResult_EliminatedTitleFormat";
        private const string ScoresTitleFormatKey = "RoundResult_ScoresTitleFormat";
        private const string DefaultFormat = "{0}";
        private readonly RoundResultViewModel _viewModel;

        /// <summary>Inicializa la vista con su ViewModel.</summary>
        public RoundResultScreen(RoundResultViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string winnerTitleFormat = TryFindResource(WinnerTitleFormatKey) as string ?? DefaultFormat;
            string bidCompletedFormat = TryFindResource(BidCompletedFormatKey) as string ?? DefaultFormat;
            string eliminatedTitleFormat = TryFindResource(EliminatedTitleFormatKey) as string ?? DefaultFormat;
            string scoresTitleFormat = TryFindResource(ScoresTitleFormatKey) as string ?? DefaultFormat;

            lblWinnerTitle.Text = string.Format(winnerTitleFormat, _viewModel.WinnerName.ToUpperInvariant());
            lblBidCompleted.Text = string.Format(bidCompletedFormat, _viewModel.CompletedBidCardCount);
            lblEliminatedTitle.Text = string.Format(eliminatedTitleFormat, _viewModel.EliminatedPlayerName);
            lblScoresTitle.Text = string.Format(scoresTitleFormat, _viewModel.TargetWinsCount);
        }

    }
}
