using System.Windows;
using System.Windows.Documents;
using TLMG.Client.Models;

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
        private const bool SaveScreenInHistory = false;

        private readonly string _targetPlayerName;
        private ModerationMode _currentMode;

        public ModerationScreen(string targetPlayerName, ModerationMode initialMode)
        {
            InitializeComponent();
            _targetPlayerName = targetPlayerName;
            lblTargetPlayerName.Text = targetPlayerName;
            cmbReportReason.SelectedIndex = FirstReasonIndex;
            ApplyMode(initialMode);
        }

        protected override void UpdateDynamicLabels()
        {
            string blockIntroMessage = TryFindResource(BlockIntroKey) as string ?? string.Empty;
            string blockDetail = TryFindResource(BlockDetailKey) as string ?? string.Empty;
            Run introRun = new Run(blockIntroMessage + WordSeparator);
            Run nameRun = new Run(_targetPlayerName);
            Run detailRun = new Run(WordSeparator + blockDetail);

            nameRun.Style = FindStyle(BlockPlayerNameRunStyleKey);
            lblBlockMessage.Inlines.Clear();
            lblBlockMessage.Inlines.Add(introRun);
            lblBlockMessage.Inlines.Add(nameRun);
            lblBlockMessage.Inlines.Add(detailRun);

            UpdateSuccessTexts();
        }

        private void BtnReportTab_Click(object sender, RoutedEventArgs e)
        {
            ApplyMode(ModerationMode.Report);
        }

        private void BtnBlockTab_Click(object sender, RoutedEventArgs e)
        {
            ApplyMode(ModerationMode.Block);
        }

        private void BtnSubmitReport_Click(object sender, RoutedEventArgs e)
        {
            ShowSuccessCard();
        }

        private void BtnConfirmBlock_Click(object sender, RoutedEventArgs e)
        {
            ShowSuccessCard();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGameplay();
        }

        private void BtnReturnToGame_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGameplay();
        }

        private void ApplyMode(ModerationMode mode)
        {
            bool isReportMode = mode == ModerationMode.Report;
            string reportTabStyleKey = isReportMode ? ReportTabActiveStyleKey : TabInactiveStyleKey;
            string blockTabStyleKey = isReportMode ? TabInactiveStyleKey : BlockTabActiveStyleKey;

            _currentMode = mode;
            pnlReportForm.Visibility = isReportMode ? Visibility.Visible : Visibility.Collapsed;
            pnlBlockForm.Visibility = isReportMode ? Visibility.Collapsed : Visibility.Visible;
            btnReportTab.Style = FindStyle(reportTabStyleKey);
            btnBlockTab.Style = FindStyle(blockTabStyleKey);
            UpdateSuccessTexts();
        }

        private void UpdateSuccessTexts()
        {
            bool isReportMode = _currentMode == ModerationMode.Report;
            string titleKey = isReportMode ? ReportSuccessTitleKey : BlockSuccessTitleKey;
            string successMessage;

            if (isReportMode)
            {
                successMessage = TryFindResource(ReportSuccessMessageKey) as string ?? string.Empty;
            }
            else
            {
                string messageFormat = TryFindResource(BlockSuccessMessageFormatKey) as string ?? DefaultFormat;
                successMessage = string.Format(messageFormat, _targetPlayerName);
            }

            lblSuccessTitle.Text = TryFindResource(titleKey) as string ?? string.Empty;
            lblSuccessMessage.Text = successMessage;
        }

        private void ShowSuccessCard()
        {
            scrForm.Visibility = Visibility.Collapsed;
            brdSuccessCard.Visibility = Visibility.Visible;
        }

        private Style FindStyle(string styleKey)
        {
            Style foundStyle = (Style)FindResource(styleKey);
            return foundStyle;
        }

        private void NavigateToGameplay()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new GameplayScreen(), SaveScreenInHistory);
        }
    }
}