using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;

using TLMG.Client.Models;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class ModerationScreen : LocalizableScreen
    {
        private const string BlockIntroKey = "Moderation_BlockIntroLabel";
        private const string BlockDetailKey = "Moderation_BlockDetailMessage";
        private const string ReportSuccessTitleKey = "Moderation_ReportSuccessTitle";
        private const string ReportSuccessMessageKey = "Moderation_ReportSuccessMessage";
        private const string BlockSuccessTitleKey = "Moderation_BlockSuccessTitle";
        private const string BlockSuccessMessageFormatKey = "Moderation_BlockSuccessMessageFormat";
        private const string VoteSuccessTitleKey = "Moderation_VoteAcceptedTitle";
        private const string VoteSuccessMessageKey = "Moderation_VoteAcceptedMessage";
        private const string ReportTabActiveStyleKey = "ModerationTabReportActiveButtonStyle";
        private const string BlockTabActiveStyleKey = "ModerationTabBlockActiveButtonStyle";
        private const string TabInactiveStyleKey = "ModerationTabInactiveButtonStyle";
        private const string BlockPlayerNameRunStyleKey = "ModerationBlockPlayerNameRunStyle";
        private const string DefaultFormat = "{0}";
        private const string WordSeparator = " ";
        private readonly ModerationViewModel _viewModel;

        public ModerationScreen(ModerationViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            RefreshVisualState();
        }

        protected override void UpdateDynamicLabels()
        {
            string blockIntro = TryFindResource(BlockIntroKey) as string ?? string.Empty;
            string blockDetail = TryFindResource(BlockDetailKey) as string ?? string.Empty;
            Run introRun = new Run(blockIntro + WordSeparator);
            Run nameRun = new Run(_viewModel.TargetPlayerName);
            Run detailRun = new Run(WordSeparator + blockDetail);
            nameRun.Style = (Style)FindResource(BlockPlayerNameRunStyleKey);
            lblBlockMessage.Inlines.Clear();
            lblBlockMessage.Inlines.Add(introRun);
            lblBlockMessage.Inlines.Add(nameRun);
            lblBlockMessage.Inlines.Add(detailRun);
            UpdateSuccessTexts();
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            RefreshVisualState();
            UpdateDynamicLabels();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs eventArguments)
        {
            if (eventArguments.PropertyName == nameof(ModerationViewModel.Mode)
                || eventArguments.PropertyName == nameof(ModerationViewModel.IsCompleted))
            {
                RefreshVisualState();
            }
        }

        private void RefreshVisualState()
        {
            ApplyMode(_viewModel.Mode);
            scrForm.Visibility = Visibility.Visible;
            brdSuccessCard.Visibility = Visibility.Collapsed;
            if (_viewModel.IsCompleted)
            {
                scrForm.Visibility = Visibility.Collapsed;
                brdSuccessCard.Visibility = Visibility.Visible;
            }
        }

        private void ApplyMode(ModerationMode mode)
        {
            pnlReportForm.Visibility = Visibility.Collapsed;
            pnlBlockForm.Visibility = Visibility.Collapsed;
            pnlVoteForm.Visibility = Visibility.Collapsed;
            grdModerationTabs.Visibility = Visibility.Visible;
            btnReportTab.Style = (Style)FindResource(TabInactiveStyleKey);
            btnBlockTab.Style = (Style)FindResource(TabInactiveStyleKey);

            if (mode == ModerationMode.Report)
            {
                pnlReportForm.Visibility = Visibility.Visible;
                btnReportTab.Style = (Style)FindResource(ReportTabActiveStyleKey);
            }
            else if (mode == ModerationMode.Block)
            {
                pnlBlockForm.Visibility = Visibility.Visible;
                btnBlockTab.Style = (Style)FindResource(BlockTabActiveStyleKey);
            }
            else if (mode == ModerationMode.VoteToKick)
            {
                pnlVoteForm.Visibility = Visibility.Visible;
                grdModerationTabs.Visibility = Visibility.Collapsed;
            }
            UpdateSuccessTexts();
        }

        private void UpdateSuccessTexts()
        {
            string titleKey = ReportSuccessTitleKey;
            string successMessage = TryFindResource(ReportSuccessMessageKey) as string ?? string.Empty;

            if (_viewModel.Mode == ModerationMode.Block)
            {
                titleKey = BlockSuccessTitleKey;
                string format = TryFindResource(BlockSuccessMessageFormatKey) as string ?? DefaultFormat;
                successMessage = string.Format(format, _viewModel.TargetPlayerName);
            }
            else if (_viewModel.Mode == ModerationMode.VoteToKick)
            {
                titleKey = VoteSuccessTitleKey;
                successMessage = TryFindResource(VoteSuccessMessageKey) as string ?? string.Empty;
            }
            lblSuccessTitle.Text = TryFindResource(titleKey) as string ?? string.Empty;
            lblSuccessMessage.Text = successMessage;
        }
    }
}