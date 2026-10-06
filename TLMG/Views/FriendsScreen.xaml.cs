using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    public partial class FriendsScreen : LocalizableScreen
    {
        private const int TabIndexFriends = 0;
        private const int TabIndexPending = 1;
        private const int TabIndexBlocked = 2;
        private const string CountFriends = "5";
        private const string CountPending = "2";
        private const string CountBlocked = "1";
        private const string ActiveTabButtonStyleKey = "TabNavActiveButtonStyle";
        private const string InactiveTabButtonStyleKey = "TabNavInactiveButtonStyle";
        private const string ActiveBadgeStyleKey = "BadgeActiveStyle";
        private const string InactiveBadgeStyleKey = "BadgeInactiveStyle";
        private const string ActiveBadgeTextStyleKey = "BadgeTextActiveStyle";
        private const string InactiveBadgeTextStyleKey = "BadgeTextInactiveStyle";

        private sealed class TabViews
        {
            public TabViews(UIElement content, Button tabButton, Border badgeBorder, TextBlock badgeLabel)
            {
                Content = content;
                TabButton = tabButton;
                BadgeBorder = badgeBorder;
                BadgeLabel = badgeLabel;
            }

            public UIElement Content { get; }
            public Button TabButton { get; }
            public Border BadgeBorder { get; }
            public TextBlock BadgeLabel { get; }
        }

        private int _currentTabIndex;
        private TabViews[] _tabViews;

        public FriendsScreen()
        {
            InitializeComponent();
            _currentTabIndex = TabIndexFriends;
            InitializeTabViews();
            InitializeBadges();
            SwitchTab(_currentTabIndex);
        }

        private void InitializeTabViews()
        {
            _tabViews = new[]
            {
                new TabViews(grdFriendsContent, btnTabFriends, brdBadgeFriends, lblBadgeFriends),
                new TabViews(grdPendingContent, btnTabPending, brdBadgePending, lblBadgePending),
                new TabViews(grdBlockedContent, btnTabBlocked, brdBadgeBlocked, lblBadgeBlocked)
            };
        }

        private void InitializeBadges()
        {
            lblBadgeFriends.Text = CountFriends;
            lblBadgePending.Text = CountPending;
            lblBadgeBlocked.Text = CountBlocked;
        }

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.GoBack();
        }

        private void BtnTabFriends_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            SwitchTab(TabIndexFriends);
        }

        private void BtnTabPending_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            SwitchTab(TabIndexPending);
        }

        private void BtnTabBlocked_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            SwitchTab(TabIndexBlocked);
        }

        private void SwitchTab(int targetTabIndex)
        {
            for (int tabIndex = 0; tabIndex < _tabViews.Length; tabIndex++)
            {
                bool isTargetTab = tabIndex == targetTabIndex;
                TabViews currentTabViews = _tabViews[tabIndex];

                currentTabViews.Content.Visibility = 
                    isTargetTab ? Visibility.Visible : Visibility.Collapsed;
                currentTabViews.TabButton.Style = 
                    (Style)FindResource(isTargetTab ? ActiveTabButtonStyleKey : InactiveTabButtonStyleKey);
                currentTabViews.BadgeBorder.Style = 
                    (Style)FindResource(isTargetTab ? ActiveBadgeStyleKey : InactiveBadgeStyleKey);
                currentTabViews.BadgeLabel.Style = 
                    (Style)FindResource(isTargetTab ? ActiveBadgeTextStyleKey : InactiveBadgeTextStyleKey);
            }

            _currentTabIndex = targetTabIndex;
        }
    }
}