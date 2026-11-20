using System.Windows;
using System.Windows.Controls;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class FriendsScreen : LocalizableScreen
    {
        private const int TabIndexFriends = 0;
        private const int TabIndexPending = 1;

        private const string ActiveTabButtonStyleKey = "TabNavActiveButtonStyle";
        private const string InactiveTabButtonStyleKey = "TabNavInactiveButtonStyle";
        private const string ActiveBadgeStyleKey = "BadgeActiveStyle";
        private const string InactiveBadgeStyleKey = "BadgeInactiveStyle";

        private readonly FriendsViewModel _viewModel;
        private TabViews[] _tabViews;

        public FriendsScreen(FriendsViewModel friendsViewModel)
        {
            InitializeComponent();

            _viewModel = friendsViewModel;
            DataContext = friendsViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;

            InitializeTabViews();
            SwitchTab(TabIndexFriends);
        }

        private void InitializeTabViews()
        {
            _tabViews = new[]
            {
                new TabViews(pnlFriendsContent, btnTabFriends, brdBadgeFriends),
                new TabViews(pnlPendingContent, btnTabPending, brdBadgePending)
            };
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Activate();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Deactivate();
        }

        private void BtnTab_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;

            if (clickedButton == btnTabPending)
            {
                SwitchTab(TabIndexPending);
            }
            else
            {
                SwitchTab(TabIndexFriends);
            }
        }

        private void SwitchTab(int targetTabIndex)
        {
            for (int tabIndex = 0; tabIndex < _tabViews.Length; tabIndex++)
            {
                bool isTargetTab = tabIndex == targetTabIndex;
                TabViews currentTabViews = _tabViews[tabIndex];

                currentTabViews.Content.Visibility = isTargetTab ? Visibility.Visible : Visibility.Collapsed;
                currentTabViews.TabButton.Style =
                    (Style)FindResource(isTargetTab ? ActiveTabButtonStyleKey : InactiveTabButtonStyleKey);
                currentTabViews.BadgeBorder.Style =
                    (Style)FindResource(isTargetTab ? ActiveBadgeStyleKey : InactiveBadgeStyleKey);
            }
        }

        private sealed class TabViews
        {
            public TabViews(UIElement content, Button tabButton, Border badgeBorder)
            {
                Content = content;
                TabButton = tabButton;
                BadgeBorder = badgeBorder;
            }

            public UIElement Content { get; }

            public Button TabButton { get; }

            public Border BadgeBorder { get; }
        }
    }
}