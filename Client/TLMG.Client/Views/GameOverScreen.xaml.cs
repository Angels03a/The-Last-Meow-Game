using System.Windows;
using TLMG.Client.ViewModels;
using System.Windows.Controls;

namespace TLMG.Client.Views
{
    public partial class GameOverScreen : LocalizableScreen
    {
        private const string WinnerTitleFormatKey = "GameOver_WinnerTitleFormat";
        private const string DefaultFormat = "{0}";
        private readonly GameOverViewModel _viewModel;

        /// <summary>Inicializa la vista con su ViewModel.</summary>
        public GameOverScreen(GameOverViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string winnerTitleFormat = TryFindResource(WinnerTitleFormatKey) as string ?? DefaultFormat;
            lblWinnerTitle.Text = string.IsNullOrWhiteSpace(_viewModel.WinnerName)
                ? TryFindResource("GameOver_NoWinnerAvailable") as string ?? string.Empty
                : string.Format(winnerTitleFormat, _viewModel.WinnerName.ToUpperInvariant());
        }

    }
}
