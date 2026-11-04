using System.Windows;

namespace TLMG.Client.Views
{
    public partial class MatchClosedScreen : LocalizableScreen
    {
        private const string InactivityMessageFormatKey = "MatchClosed_InactivityMessageFormat";
        private const string DefaultFormat = "{0}";
        private const int MatchInactivityTimeoutMinutes = 10;
        private const bool SaveScreenInHistory = false;

        public MatchClosedScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
            string inactivityMessageFormat = TryFindResource(InactivityMessageFormatKey) as string ?? DefaultFormat;
            lblInactivityMessage.Text = string.Format(inactivityMessageFormat, MatchInactivityTimeoutMinutes);
        }

        private void BtnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen(), SaveScreenInHistory);
        }
    }
}