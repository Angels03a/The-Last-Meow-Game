using System.Windows;
using System.Windows.Controls;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla para la gestión de amigos, solicitudes pendientes y usuarios bloqueados.
    /// </summary>
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

        private int _currentTabIndex;
        private TabViews[] _tabViews;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="FriendsScreen"/>.
        /// </summary>
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
                new TabViews(pnlFriendsContent, btnTabFriends, brdBadgeFriends, lblBadgeFriends),
                new TabViews(pnlPendingContent, btnTabPending, brdBadgePending, lblBadgePending),
                new TabViews(pnlBlockedContent, btnTabBlocked, brdBadgeBlocked, lblBadgeBlocked)
            };
        }

        private void InitializeBadges()
        {
            lblBadgeFriends.Text = CountFriends;
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