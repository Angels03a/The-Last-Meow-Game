using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TLMG.Client.Models;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de partida. Muestra el estado del ViewModel y resuelve el menú contextual y la navegación.
    /// </summary>
    public partial class GameplayScreen : LocalizableScreen
    {
        private const string RoundPhaseFormatKey = "Gameplay_RoundPhaseFormat";
        private const string DefaultRoundPhaseFormat = "{0}";
        private const double NoMargin = 0;
        private const double ContextMenuWidthPixels = 190;
        private const double ContextMenuHeightPixels = 200;

        private readonly GameplayViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="GameplayScreen"/>.
        /// </summary>
        public GameplayScreen(GameplayViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            DataContext = _viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string roundPhaseFormat = TryFindResource(RoundPhaseFormatKey) as string ?? DefaultRoundPhaseFormat;
            string phaseName = TryFindResource(_viewModel.PhaseLabelKey) as string ?? string.Empty;
            lblRoundPhase.Text = string.Format(roundPhaseFormat, _viewModel.CurrentRoundNumber, phaseName);
        }

        private void PnlOpponent_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement opponentPanel)
            {
                bool hasOpponentName = opponentPanel.Tag != null;

                if (hasOpponentName)
                {
                    _viewModel.SelectedPlayerName = opponentPanel.Tag.ToString();
                    ShowPlayerContextMenu(e.GetPosition(grdTable));
                }
            }
        }

        private void GrdTable_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HidePlayerContextMenu();
        }

        private void BtnMenuCancel_Click(object sender, RoutedEventArgs e)
        {
            HidePlayerContextMenu();
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.KickVoteRequested -= OnKickVoteRequested;
            _viewModel.KickVoteRequested += OnKickVoteRequested;
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.KickVoteRequested -= OnKickVoteRequested;
        }

        private void OnKickVoteRequested(object sender, EventArgs e)
        {
            HidePlayerContextMenu();
            // TODO (BL-161): Iniciar la votación de expulsión del jugador seleccionado.
        }

        private void ShowPlayerContextMenu(Point clickPosition)
        {
            double maximunLeft = grdTable.ActualWidth - ContextMenuWidthPixels;
            double maximunTop = grdTable.ActualHeight - ContextMenuHeightPixels;
            double menuLeft = Math.Min(clickPosition.X, maximunLeft);
            double menuTop = Math.Min(clickPosition.Y, maximunTop);

            brdPlayerContextMenu.Margin = new Thickness(menuLeft, menuTop, NoMargin, NoMargin);
            brdPlayerContextMenu.Visibility = Visibility.Visible;
        }

        private void HidePlayerContextMenu()
        {
            brdPlayerContextMenu.Visibility = Visibility.Collapsed;
        }

    }
}
