using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TLMG.Client.Models;

namespace TLMG.Client.Views
{
    public partial class GameplayScreen : LocalizableScreen
    {
        private const string RoundPhaseFormatKey = "Gameplay_RoundPhaseFormat";
        private const string PhaseBiddingKey = "Gameplay_PhaseBiddingLabel";
        private const string DefaultRoundPhaseFormat = "{0}";
        private const int CurrentRoundNumber = 3;
        private const int InitialBidAmount = 4;
        private const int MinimumBidAmount = 1;
        private const int MaximumBidAmount = 12;
        private const int BidStep = 1;
        private const double NoMargin = 0;
        private const double ContextMenuWidthPixels = 190;
        private const double ContextMenuHeightPixels = 200;
        private const bool SaveScreenInHistory = false;

        private int _bidAmount;
        private string _selectedPlayerName;

        public GameplayScreen()
        {
            InitializeComponent();
            _bidAmount = InitialBidAmount;
            _selectedPlayerName = string.Empty;
            lblCurrentBidValue.Text = InitialBidAmount.ToString();
            UpdateBidAmountLabel();
        }

        protected override void UpdateDynamicLabels()
        {
            string roundPhaseFormat = TryFindResource(RoundPhaseFormatKey) as string ?? DefaultRoundPhaseFormat;
            string phaseName = TryFindResource(PhaseBiddingKey) as string ?? string.Empty;
            lblRoundPhase.Text = string.Format(roundPhaseFormat, CurrentRoundNumber, phaseName);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Agregar confirmación de salida antes de navegar a la pantalla principal.

            NavigateToScreen(new MainMenuScreen());
        }

        private void BtnDecreaseBid_Click(object sender, RoutedEventArgs e)
        {
            bool canDecrease = _bidAmount > MinimumBidAmount;

            if (canDecrease)
            {
                _bidAmount -= BidStep;
                UpdateBidAmountLabel();
            }
        }

        private void BtnIncreaseBid_Click(object sender, RoutedEventArgs e)
        {
            bool canIncrease = _bidAmount < MaximumBidAmount;

            if (canIncrease)
            {
                _bidAmount += BidStep;
                UpdateBidAmountLabel();
            }
        }

        private void PnlOpponent_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement opponentPanel)
            {
                bool hasOpponentName = opponentPanel.Tag != null;

                if (hasOpponentName)
                {
                    _selectedPlayerName = opponentPanel.Tag.ToString();
                    ShowPlayerContextMenu(_selectedPlayerName, e.GetPosition(grdTable));
                }
            }
        }

        private void GrdTable_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HidePlayerContextMenu();
        }

        // FIXME: Los métodos de navegación a la pantalla de moderación (reportes y bloqueos)
        // son código duplicado con la clase ModerationScreen.
        // Deberían refactorizarse para evitar la duplicación.
        private void BtnMenuReport_Click(object sender, RoutedEventArgs e)
        {
            HidePlayerContextMenu();
            NavigateToModerationScreen(ModerationMode.Report);
        }

        private void BtnMenuKick_Click(object sender, RoutedEventArgs e)
        {
            HidePlayerContextMenu();
            StartKickVote();
        }

        private void BtnMenuBlock_Click(object sender, RoutedEventArgs e)
        {
            HidePlayerContextMenu();
            NavigateToModerationScreen(ModerationMode.Block);
        }

        private void BtnMenuCancel_Click(object sender, RoutedEventArgs e)
        {
            HidePlayerContextMenu();
        }

        private void BtnDemoRevealCards_Click(object sender, RoutedEventArgs e)
        {
            NavigateToScreen(new RoundResultScreen());
        }

        private void BtnDemoGameOver_Click(object sender, RoutedEventArgs e)
        {
            NavigateToScreen(new GameOverScreen());
        }

        private void BtnDemoInactivity_Click(object sender, RoutedEventArgs e)
        {
            NavigateToScreen(new MatchClosedScreen());
        }

        private void UpdateBidAmountLabel()
        {
            lblBidAmount.Text = _bidAmount.ToString();
        }

        private void ShowPlayerContextMenu(string playerName, Point clickPosition)
        {
            double maxLeft = grdTable.ActualWidth - ContextMenuWidthPixels;
            double maxTop = grdTable.ActualHeight - ContextMenuHeightPixels;
            double menuLeft = Math.Min(clickPosition.X, maxLeft);
            double menuTop = Math.Min(clickPosition.Y, maxTop);

            lblContextMenuPlayerName.Text = playerName.ToUpperInvariant();
            brdPlayerContextMenu.Margin = new Thickness(menuLeft, menuTop, NoMargin, NoMargin);
            brdPlayerContextMenu.Visibility = Visibility.Visible;
        }

        private void HidePlayerContextMenu()
        {
            brdPlayerContextMenu.Visibility = Visibility.Collapsed;
        }

        private void NavigateToModerationScreen(ModerationMode moderationMode)
        {
            NavigateToScreen(new ModerationScreen(_selectedPlayerName, moderationMode));
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