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
        private const string ReportTabActiveStyleKey = "ModerationTabReportActiveButtonStyle";
        private const string BlockTabActiveStyleKey = "ModerationTabBlockActiveButtonStyle";
        private const string TabInactiveStyleKey = "ModerationTabInactiveButtonStyle";
        private const string BlockPlayerNameRunStyleKey = "ModerationBlockPlayerNameRunStyle";
        private const string DefaultFormat = "{0}";
        private const string WordSeparator = " ";
        private const int FirstReasonIndex = 0;

        private readonly ModerationViewModel _viewModel;

        public ModerationScreen(ModerationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            lblTargetPlayerName.Text = viewModel.TargetPlayerName;
            cmbReportReason.SelectedIndex = FirstReasonIndex;
            ApplyMode(viewModel.Mode);
        }

        protected override void UpdateDynamicLabels()
        {
            string blockIntroMessage = TryFindResource(BlockIntroKey) as string ?? string.Empty;
            string blockDetail = TryFindResource(BlockDetailKey) as string ?? string.Empty;
            Run introRun = new Run(blockIntroMessage + WordSeparator);
            Run nameRun = new Run(_viewModel.TargetPlayerName);
            Run detailRun = new Run(WordSeparator + blockDetail);

            nameRun.Style = FindStyle(BlockPlayerNameRunStyleKey);
            lblBlockMessage.Inlines.Clear();
            lblBlockMessage.Inlines.Add(introRun);
            lblBlockMessage.Inlines.Add(nameRun);
            lblBlockMessage.Inlines.Add(detailRun);

            UpdateSuccessTexts();
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            RefreshVisualState();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            ApplyMode(_viewModel.Mode);
            scrForm.Visibility = _viewModel.IsCompleted ? Visibility.Collapsed : Visibility.Visible;
            brdSuccessCard.Visibility = _viewModel.IsCompleted ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyMode(ModerationMode mode)
        {
            bool isReportMode = mode == ModerationMode.Report;
            string reportTabStyleKey = isReportMode ? ReportTabActiveStyleKey : TabInactiveStyleKey;
            string blockTabStyleKey = isReportMode ? TabInactiveStyleKey : BlockTabActiveStyleKey;

            pnlReportForm.Visibility = isReportMode ? Visibility.Visible : Visibility.Collapsed;
            pnlBlockForm.Visibility = isReportMode ? Visibility.Collapsed : Visibility.Visible;
            btnReportTab.Style = FindStyle(reportTabStyleKey);
            btnBlockTab.Style = FindStyle(blockTabStyleKey);
            UpdateSuccessTexts();
        }

        private void UpdateSuccessTexts()
        {
            bool isReportMode = _viewModel.Mode == ModerationMode.Report;
            string titleKey = isReportMode ? ReportSuccessTitleKey : BlockSuccessTitleKey;
            string successMessage;

            if (isReportMode)
            {
                successMessage = TryFindResource(ReportSuccessMessageKey) as string ?? string.Empty;
            }
            else
            {
                string messageFormat = TryFindResource(BlockSuccessMessageFormatKey) as string ?? DefaultFormat;
                successMessage = string.Format(messageFormat, _viewModel.TargetPlayerName);
            }

            lblSuccessTitle.Text = TryFindResource(titleKey) as string ?? string.Empty;
            lblSuccessMessage.Text = successMessage;
        }

        private Style FindStyle(string styleKey)
        {
            Style foundStyle = (Style)FindResource(styleKey);
            return foundStyle;
        }
    }
}