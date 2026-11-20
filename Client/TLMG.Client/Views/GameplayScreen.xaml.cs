using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using TLMG.Client.Communication;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class GameplayScreen : LocalizableScreen
    {
        private const string RoundPhaseFormatKey = "Gameplay_RoundPhaseFormat";
        private const string DefaultRoundPhaseFormat = "{0}";
        private const double NoMargin = 0;
        private const double ContextMenuWidthPixels = 190;
        private const double ContextMenuHeightPixels = 200;

        private readonly GameplayViewModel _viewModel;

        public GameplayScreen(GameplayViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            txtChatInput.MaxLength = ChatInputHelper.MaximumMessageCharacters;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            DataContext = _viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string roundPhaseFormat = TryFindResource(RoundPhaseFormatKey) as string ?? DefaultRoundPhaseFormat;
            string phaseName = TryFindResource(_viewModel.PhaseLabelKey) as string ?? string.Empty;
            lblRoundPhase.Text = string.Empty;

            if (_viewModel.HasMatchView)
            {
                lblRoundPhase.Text = string.Format(roundPhaseFormat, _viewModel.CurrentRoundNumber, phaseName);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs eventArguments)
        {
            if (eventArguments.PropertyName == nameof(GameplayViewModel.PhaseLabelKey))
            {
                UpdateDynamicLabels();
            }
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
            _viewModel.Activate();

            _viewModel.KickVoteRequested -= OnKickVoteRequested;
            _viewModel.KickVoteRequested += OnKickVoteRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateDynamicLabels();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Deactivate();

            _viewModel.KickVoteRequested -= OnKickVoteRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnKickVoteRequested(object sender, EventArgs e)
        {
            HidePlayerContextMenu();
            // TODO (BL-161): Start the kick vote for the selected player.
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

        private bool CanInsertChatText(string insertedText)
        {
            string proposedText = txtChatInput.Text.Remove(txtChatInput.SelectionStart, txtChatInput.SelectionLength);
            proposedText = proposedText.Insert(txtChatInput.SelectionStart, insertedText);
            bool canInsert = ChatInputHelper.CanEnterText(proposedText);

            return canInsert;
        }

        private void OnChatPreviewTextInput(object sender, TextCompositionEventArgs eventArguments)
        {
            eventArguments.Handled = !CanInsertChatText(eventArguments.Text);
        }

        private void OnChatPasting(object sender, DataObjectPastingEventArgs eventArguments)
        {
            string pastedText = eventArguments.DataObject.GetData(DataFormats.UnicodeText) as string;
            bool canPaste = pastedText != null && CanInsertChatText(pastedText);

            if (!canPaste)
            {
                eventArguments.CancelCommand();
            }
        }
    }
}