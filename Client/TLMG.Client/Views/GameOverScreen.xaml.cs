using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class GameOverScreen : LocalizableScreen
    {
        private const string WinnerTitleFormatKey = "GameOver_WinnerTitleFormat";
        private const string CollectiveAbandonmentKey = "GameOver_CollectiveAbandonment";
        private const string NoWinnerAvailableKey = "GameOver_NoWinnerAvailable";
        private const string DefaultFormat = "{0}";

        private readonly GameOverViewModel _viewModel;

        /// <summary>Sets up the view with its ViewModel.</summary>
        public GameOverScreen(GameOverViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string winnerTitle;

            if (_viewModel.EndedByCollectiveAbandonment)
            {
                winnerTitle = TryFindResource(CollectiveAbandonmentKey) as string ?? string.Empty;
            }
            else if (string.IsNullOrWhiteSpace(_viewModel.WinnerName))
            {
                winnerTitle = TryFindResource(NoWinnerAvailableKey) as string ?? string.Empty;
            }
            else
            {
                string winnerTitleFormat = TryFindResource(WinnerTitleFormatKey) as string ?? DefaultFormat;
                winnerTitle = string.Format(winnerTitleFormat, _viewModel.WinnerName.ToUpperInvariant());
            }

            lblWinnerTitle.Text = winnerTitle;
        }
    }
}
