using System.Windows;
using System.Windows.Controls;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class FriendsScreen : LocalizableScreen
    {
        private const string ActiveTabButtonStyleKey = "TabNavActiveButtonStyle";
        private const string InactiveTabButtonStyleKey = "TabNavInactiveButtonStyle";
        private const string ActiveBadgeStyleKey = "BadgeActiveStyle";
        private const string InactiveBadgeStyleKey = "BadgeInactiveStyle";

        private readonly FriendsViewModel _viewModel;
        private Button _selectedTab;

        public FriendsScreen(FriendsViewModel friendsViewModel)
        {
            InitializeComponent();
            _viewModel = friendsViewModel;
            DataContext = friendsViewModel;
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            SwitchTab(btnTabFriends);
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.Activate();
            SwitchTab(btnTabFriends);
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.Deactivate();
        }

        private void BtnTab_Click(object sender, RoutedEventArgs eventArguments)
        {
            Button clickedButton = sender as Button;

            if (clickedButton == btnTabFriends || clickedButton == btnTabPending
                || (clickedButton == btnTabBlocked && _viewModel.IsRegisteredPlayer))
            {
                SwitchTab(clickedButton);
            }
        }

        private void SwitchTab(Button selectedTab)
        {
            _selectedTab = selectedTab;
            ApplyTabState(pnlFriendsContent, btnTabFriends, brdBadgeFriends);
            ApplyTabState(pnlPendingContent, btnTabPending, brdBadgePending);
            ApplyTabState(pnlBlockedContent, btnTabBlocked, brdBadgeBlocked);

            if (selectedTab == btnTabBlocked)
            {
                _viewModel.BlockedPlayers.RefreshCommand.Execute(null);
            }
        }

        private void ApplyTabState(UIElement content, Button tabButton, Border badgeBorder)
        {
            Visibility contentVisibility = Visibility.Collapsed;
            string tabStyleKey = InactiveTabButtonStyleKey;
            string badgeStyleKey = InactiveBadgeStyleKey;

            if (tabButton == _selectedTab)
            {
                contentVisibility = Visibility.Visible;
                tabStyleKey = ActiveTabButtonStyleKey;
                badgeStyleKey = ActiveBadgeStyleKey;
            }

            content.Visibility = contentVisibility;
            tabButton.Style = (Style)FindResource(tabStyleKey);
            badgeBorder.Style = (Style)FindResource(badgeStyleKey);
        }
    }
}