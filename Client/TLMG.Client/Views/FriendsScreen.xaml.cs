using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de amigos: muestra la lista real que conserva el ServerConnectionManager, con su presencia, e invita
    /// a un amigo en línea a la sala del jugador. Las solicitudes y los bloqueos aún no tienen servidor.
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
        private const int EmptyFriendCount = 0;

        private int _currentTabIndex;
        private TabViews[] _tabViews;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="FriendsScreen"/>.
        /// </summary>
        public FriendsScreen()
        {
            InitializeComponent();
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
            _currentTabIndex = TabIndexFriends;
            InitializeTabViews();
            InitializeBadges();
            SwitchTab(_currentTabIndex);
        }

        private void InitializeTabViews()
        {
            _tabViews = new[]
            {
                new TabViews(pnlFriendsContent, btnTabFriends, brdBadgeFriends, lblBadgeFriends),
                new TabViews(pnlPendingContent, btnTabPending, brdBadgePending, lblBadgePending),
                new TabViews(pnlBlockedContent, btnTabBlocked, brdBadgeBlocked, lblBadgeBlocked)
            };
        }

        private void InitializeBadges()
        {
            lblBadgePending.Text = CountPending;
            lblBadgeBlocked.Text = CountBlocked;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
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
                currentTabViews.BadgeBorder.Style =
                    (Style)FindResource(isTargetTab ? ActiveBadgeStyleKey : InactiveBadgeStyleKey);
                currentTabViews.BadgeLabel.Style =
                    (Style)FindResource(isTargetTab ? ActiveBadgeTextStyleKey : InactiveBadgeTextStyleKey);
            }

            _currentTabIndex = targetTabIndex;
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

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
            ServerConnection.FriendListReceived -= OnFriendListReceived;
            ServerConnection.FriendPresenceChanged -= OnFriendPresenceChanged;
            ServerConnection.InviteFriendResultReceived -= OnInviteFriendResultReceived;
        }

        // Un invitado no tiene canal de amigos: se le avisa sin llamar al servidor. Si la lista aún no llegó (por
        // ejemplo, falló la suscripción del login), se pide de nuevo y llega por FriendListReceived.
        private void ShowOrRequestFriendList()
        {
            ShowFriendList();

            if (!ServerConnection.HasFriendsChannel)
            {
                lblEmptyFriends.SetResourceReference(TextBlock.TextProperty, GuestWithoutFriendsMessageKey);
                lblEmptyFriends.Visibility = Visibility.Visible;
            }
            else if (ServerConnection.FriendList == null)
            {
                ServerConnection.RequestFriendList();
            }
        }

        private void OnFriendListReceived(
            object sender,
            ServerEventArgs<OperationSubscribeToFriendsResult> serverEventArguments)
        {
            if (serverEventArguments.Payload.IsSuccess)
            {
                ShowFriendList();
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
            ShowFriendList();
        }

        private void OnInviteFriendResultReceived(
            object sender,
            ServerEventArgs<OperationInviteFriendToRoomResult> serverEventArguments)
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

            // Si el envío falla, el manager dispara ConnectionLost y MainWindow regresa al inicio de sesión.
            ServerConnection.InviteFriendToRoom(invitedFriend.UserId);
        }

        // Invitar se habilita solo con el amigo en línea y una sala propia; el servidor vuelve a validar todo.
        private void ShowFriendList()
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

            lstFriends.ItemsSource = friendEntries;
            lblBadgeFriends.Text = friendEntries.Count.ToString();
            lblEmptyFriends.SetResourceReference(TextBlock.TextProperty, EmptyFriendsMessageKey);
            bool hasNoFriends = friendEntries.Count == EmptyFriendCount;
            lblEmptyFriends.Visibility = hasNoFriends ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Agrupa los elementos de la interfaz asociados a una pestaña de navegación.
        /// </summary>
        private sealed class TabViews
        {
            /// <summary>
            /// Inicializa una nueva instancia de la clase <see cref="TabViews"/>.
            /// </summary>
            /// <param name="content">Elemento de interfaz que contiene la vista de la pestaña.</param>
            /// <param name="tabButton">Botón del encabezado de la pestaña.</param>
            /// <param name="badgeBorder">Borde contenedor del indicador numérico.</param>
            /// <param name="badgeLabel">Etiqueta de texto con el contador numérico.</param>
            public TabViews(UIElement content, Button tabButton, Border badgeBorder, TextBlock badgeLabel)
            {
                Content = content;
                TabButton = tabButton;
                BadgeBorder = badgeBorder;
                BadgeLabel = badgeLabel;
            }

            /// <summary>
            /// Obtiene el elemento de contenido asociado a la pestaña.
            /// </summary>
            public UIElement Content { get; }

            /// <summary>
            /// Obtiene el botón de navegación de la pestaña.
            /// </summary>
            public Button TabButton { get; }

            /// <summary>
            /// Obtiene el borde contenedor del indicador numérico de la pestaña.
            /// </summary>
            public Border BadgeBorder { get; }

            /// <summary>
            /// Obtiene la etiqueta de texto del contador numérico de la pestaña.
            /// </summary>
            public TextBlock BadgeLabel { get; }
        }
    }
}