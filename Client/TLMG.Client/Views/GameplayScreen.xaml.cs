using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TLMG.Client.Models;
using TLMG.Client.ViewModels;
using TLMG.Client.Navigation;

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
        private const bool SaveScreenInHistory = false;

        private readonly GameplayViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="GameplayScreen"/>.
        /// </summary>
        public GameplayScreen()
        {
            InitializeComponent();

            _viewModel = new GameplayViewModel();
            _viewModel.ExitRequested += OnExitRequested;
            _viewModel.ReportRequested += OnReportRequested;
            _viewModel.BlockRequested += OnBlockRequested;
            _viewModel.KickVoteRequested += OnKickVoteRequested;
            _viewModel.DemoRoundResultRequested += OnDemoRoundResultRequested;
            _viewModel.DemoGameOverRequested += OnDemoGameOverRequested;
            _viewModel.DemoInactivityRequested += OnDemoInactivityRequested;
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

        private void OnExitRequested(object sender, EventArgs e)
        {
            // TODO: Agregar confirmación de salida antes de navegar a la pantalla principal.

            ((App)Application.Current).Navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }

        // FIXME: Los métodos de navegación a la pantalla de moderación (reportes y bloqueos)
        // son código duplicado con la clase ModerationScreen.
        // Deberían refactorizarse para evitar la duplicación.
        private void OnReportRequested(object sender, EventArgs e)
        {
            HidePlayerContextMenu();
            NavigateToModerationScreen(ModerationMode.Report);
        }

        private void OnBlockRequested(object sender, EventArgs e)
        {
            HidePlayerContextMenu();
            NavigateToModerationScreen(ModerationMode.Block);
        }

        private void OnKickVoteRequested(object sender, EventArgs e)
        {
            HidePlayerContextMenu();
            StartKickVote();
        }

        private void OnDemoRoundResultRequested(object sender, EventArgs e)
        {
            NavigateToScreen(new RoundResultScreen());
        }

        private void OnDemoGameOverRequested(object sender, EventArgs e)
        {
            NavigateToScreen(new GameOverScreen());
        }

        private void OnDemoInactivityRequested(object sender, EventArgs e)
        {
            NavigateToScreen(new MatchClosedScreen());
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

        private void NavigateToModerationScreen(ModerationMode moderationMode)
        {
            NavigateToScreen(new ModerationScreen(_viewModel.SelectedPlayerName, moderationMode));
        }

        private void StartKickVote()
        {
            // TODO: Iniciar la votación de expulsión del jugador seleccionado.
        }

        private void NavigateToScreen(UserControl targetScreen)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(targetScreen, SaveScreenInHistory);
        }
    }
}