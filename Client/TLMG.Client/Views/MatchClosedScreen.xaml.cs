using System.Windows;
using TLMG.Client.Navigation;

namespace TLMG.Client.Views
{
    public partial class MatchClosedScreen : LocalizableScreen
    {
        private const string InactivityMessageFormatKey = "MatchClosed_InactivityMessageFormat";
        private const string DefaultFormat = "{0}";
        private const int MatchInactivityTimeoutMinutes = 10;

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
            ((App)Application.Current).Navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }
    }
}