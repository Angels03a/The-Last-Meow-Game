using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>Displays match state from the ViewModel and handles the player context menu.</summary>
    public partial class GameplayScreen : LocalizableScreen
    {
        private const string RoundPhaseFormatKey = "Gameplay_RoundPhaseFormat";
        private const string DefaultRoundPhaseFormat = "{0}";
        private const double NoMargin = 0;
        private const double ContextMenuWidthPixels = 190;
        private const double ContextMenuHeightPixels = 200;

        private readonly GameplayViewModel _viewModel;

        /// <summary>Connects the match view to its ViewModel.</summary>
        public GameplayScreen(GameplayViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            DataContext = _viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string roundPhaseText = string.Empty;

            if (_viewModel.HasMatchView)
            {
                string roundPhaseFormat = TryFindResource(RoundPhaseFormatKey) as string ?? DefaultRoundPhaseFormat;
                string phaseName = TryFindResource(_viewModel.PhaseLabelKey) as string ?? string.Empty;
                roundPhaseText = string.Format(roundPhaseFormat, _viewModel.CurrentRoundNumber, phaseName);
            }

            lblRoundPhase.Text = roundPhaseText;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs eventArguments)
        {
            string propertyName = eventArguments.PropertyName;

            if (propertyName == nameof(GameplayViewModel.PhaseLabelKey)
                || propertyName == nameof(GameplayViewModel.CurrentRoundNumber)
                || propertyName == nameof(GameplayViewModel.HasMatchView))
            {
                UpdateDynamicLabels();
            }
        }

        private void PnlOpponent_MouseRightButtonUp(object sender, MouseButtonEventArgs eventArguments)
        {
            FrameworkElement opponentPanel = sender as FrameworkElement;
            string opponentName = opponentPanel?.Tag?.ToString();

            if (!string.IsNullOrWhiteSpace(opponentName))
            {
                _viewModel.SelectedPlayerName = opponentName;
                ShowPlayerContextMenu(eventArguments.GetPosition(grdTable));
            }
        }

        private void GrdTable_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArguments)
        {
            HidePlayerContextMenu();
        }

        private void BtnMenuCancel_Click(object sender, RoutedEventArgs eventArguments)
        {
            HidePlayerContextMenu();
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.KickVoteRequested -= OnKickVoteRequested;
            _viewModel.KickVoteRequested += OnKickVoteRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateDynamicLabels();
            _viewModel.Activate();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.KickVoteRequested -= OnKickVoteRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Deactivate();
        }

        private void OnKickVoteRequested(object sender, EventArgs eventArguments)
        {
            HidePlayerContextMenu();
        }

        private void ShowPlayerContextMenu(Point clickPosition)
        {
            double maximumLeft = Math.Max(NoMargin, grdTable.ActualWidth - ContextMenuWidthPixels);
            double maximumTop = Math.Max(NoMargin, grdTable.ActualHeight - ContextMenuHeightPixels);
            double menuLeft = Math.Max(NoMargin, Math.Min(clickPosition.X, maximumLeft));
            double menuTop = Math.Max(NoMargin, Math.Min(clickPosition.Y, maximumTop));

            brdPlayerContextMenu.Margin = new Thickness(menuLeft, menuTop, NoMargin, NoMargin);
            brdPlayerContextMenu.Visibility = Visibility.Visible;
        }

        private void HidePlayerContextMenu()
        {
            brdPlayerContextMenu.Visibility = Visibility.Collapsed;
        }
    }
}
