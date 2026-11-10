using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de amigos: muestra la lista real que conserva el ServerConnectionManager, con su presencia, e invita
    /// a un amigo en línea a la sala del jugador.
    /// </summary>
    public partial class FriendsScreen : LocalizableScreen
    {
        private const int TabIndexFriends = 0;
        private const int TabIndexPending = 1;
        private const int TabIndexBlocked = 2;
        private const string CountPending = "2";
        private const string CountBlocked = "1";

        private const string ActiveTabButtonStyleKey = "TabNavActiveButtonStyle";
        private const string InactiveTabButtonStyleKey = "TabNavInactiveButtonStyle";
        private const string ActiveBadgeStyleKey = "BadgeActiveStyle";
        private const string InactiveBadgeStyleKey = "BadgeInactiveStyle";
        private const string ActiveBadgeTextStyleKey = "BadgeTextActiveStyle";
        private const string InactiveBadgeTextStyleKey = "BadgeTextInactiveStyle";

        private const string EmptyFriendsMessageKey = "Friends_EmptyFriends";
        private const string GuestWithoutFriendsMessageKey = "Friends_GuestWithoutFriendsMessage";
        private const string InvitationSentMessageKey = "Friends_InvitationSentMessage";
        private const bool IsErrorMessage = true;

        private readonly FriendsViewModel _viewModel;
        private int _currentTabIndex;
        private TabViews[] _tabViews;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="FriendsScreen"/>.
        /// </summary>
        public FriendsScreen()
        {
            InitializeComponent();

            _viewModel = new FriendsViewModel();
            DataContext = _viewModel;
            _viewModel.BackRequested += OnBackRequested;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;

            _currentTabIndex = TabIndexFriends;
            InitializeTabViews();
            InitializeBadges();
            SwitchTab(_currentTabIndex);
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

        private void InitializeTabViews()
        {
            _tabViews = new[]
            {
                new TabViews(pnlFriendsContent, btnTabFriends, brdBadgeFriends),
                new TabViews(pnlPendingContent, btnTabPending, brdBadgePending),
                new TabViews(pnlBlockedContent, btnTabBlocked, brdBadgeBlocked)
            };
        }

        private void InitializeBadges()
        {
            lblBadgePending.Text = CountPending;
            lblBadgeBlocked.Text = CountBlocked;
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.GoBack();
        }

        private void BtnTab_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;

            if (clickedButton == btnTabFriends)
            {
                SwitchTab(TabIndexFriends);
            }
            else if (clickedButton == btnTabPending)
            {
                SwitchTab(TabIndexPending);
            }
            else if (clickedButton == btnTabBlocked)
            {
                SwitchTab(TabIndexBlocked);
            }
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
                currentTabViews.BadgeBorder.Style 
                    = (Style)FindResource(isTargetTab ? ActiveBadgeStyleKey : InactiveBadgeStyleKey);
            }

            _currentTabIndex = targetTabIndex;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            ServerConnection.FriendListReceived -= OnFriendListReceived;
            ServerConnection.FriendListReceived += OnFriendListReceived;

            ServerConnection.FriendPresenceChanged -= OnFriendPresenceChanged;
            ServerConnection.FriendPresenceChanged += OnFriendPresenceChanged;

            ServerConnection.InviteFriendResultReceived -= OnInviteFriendResultReceived;
            ServerConnection.InviteFriendResultReceived += OnInviteFriendResultReceived;

            ShowOrRequestFriendList();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.BackRequested -= OnBackRequested;

            ServerConnection.FriendListReceived -= OnFriendListReceived;
            ServerConnection.FriendPresenceChanged -= OnFriendPresenceChanged;
            ServerConnection.InviteFriendResultReceived -= OnInviteFriendResultReceived;
        }

        private void ShowOrRequestFriendList()
        {
            UpdateViewModelFriends();

            bool isGuest = !ServerConnection.HasRegisteredSession;
            bool missingList = ServerConnection.FriendList == null;

            if (!isGuest && missingList)
            {
                ServerConnection.RequestFriendList();
            }
        }

        private void OnFriendListReceived(object sender, ServerEventArgs<OperationSubscribeToFriendsResult> 
            serverEventArguments)
        {
            if (serverEventArguments.Payload.IsSuccess)
            {
                UpdateViewModelFriends();
            }
            else
            {
                ShowStatusMessage(
                    lblFriendsStatusMessage,
                    ErrorMessageHelper.FindErrorMessage(this, serverEventArguments.Payload.ErrorKey),
                    IsErrorMessage);
            }
        }

        private void OnFriendPresenceChanged(object sender, ServerEventArgs<Friend> serverEventArguments)
        {
            UpdateViewModelFriends();
        }

        private void OnInviteFriendResultReceived(object sender, ServerEventArgs<OperationInviteFriendToRoomResult> 
            serverEventArguments)
        {
            if (serverEventArguments.Payload.IsSuccess)
            {
                ShowStatusMessage(
                    lblFriendsStatusMessage,
                    TryFindResource(InvitationSentMessageKey) as string,
                    !IsErrorMessage);
            }
            else
            {
                ShowStatusMessage(
                    lblFriendsStatusMessage,
                    ErrorMessageHelper.FindErrorMessage(this, serverEventArguments.Payload.ErrorKey),
                    IsErrorMessage);
            }
        }

        private void BtnInviteFriend_Click(object sender, RoutedEventArgs e)
        {
            FriendEntry invitedFriend = (FriendEntry)((Button)sender).DataContext;
            ServerConnection.InviteFriendToRoom(invitedFriend.UserId);
        }

        private void UpdateViewModelFriends()
        {
            List<FriendEntry> friendEntries = new List<FriendEntry>();
            bool hasRoom = ServerConnection.CurrentRoom != null;

            if (ServerConnection.FriendList != null)
            {
                foreach (Friend friend in ServerConnection.FriendList)
                {
                    friendEntries.Add(new FriendEntry
                    {
                        UserId = friend.UserId,
                        Nickname = friend.Nickname,
                        IsOnline = friend.IsOnline,
                        CanBeInvited = friend.IsOnline && hasRoom
                    });
                }
            }

            bool isGuest = !ServerConnection.HasRegisteredSession;

            _viewModel.UpdateFriendList(
                friendEntries,
                isGuest,
                GuestWithoutFriendsMessageKey,
                EmptyFriendsMessageKey);
        }

        /// <summary>
        /// Agrupa los elementos de la interfaz asociados a una pestaña de navegación.
        /// </summary>
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