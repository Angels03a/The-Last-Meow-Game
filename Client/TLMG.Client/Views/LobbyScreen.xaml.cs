using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

using TLMG.Client.Models;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class LobbyScreen : LocalizableScreen
    {
        private readonly LobbyViewModel _viewModel;

        public LobbyScreen(LobbyViewModel lobbyViewModel)
        {
            InitializeComponent();
            _viewModel = lobbyViewModel;
            DataContext = lobbyViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs eventArguments)
        {
            _viewModel.ChatMessages.CollectionChanged -= OnChatMessagesChanged;
            _viewModel.ChatMessages.CollectionChanged += OnChatMessagesChanged;
            _viewModel.Activate();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs eventArguments)
        {
            popPlayerOptions.IsOpen = false;
            popPlayerOptions.DataContext = null;
            _viewModel.ChatMessages.CollectionChanged -= OnChatMessagesChanged;
            _viewModel.Deactivate();
        }

        private void OnChatMessagesChanged(object sender, NotifyCollectionChangedEventArgs eventArguments)
        {
            scrChat.ScrollToEnd();
        }

        private void OnPlayerCardRightButtonUp(object sender, MouseButtonEventArgs eventArguments)
        {
            FrameworkElement playerCard = sender as FrameworkElement;
            LobbyPlayerEntry player = playerCard?.DataContext as LobbyPlayerEntry;

            if (player != null && _viewModel.ModerationNavigation.CanOpen(
                player.RoomId, player.UserId, ModerationMode.VoteToKick))
            {
                popPlayerOptions.DataContext = player;
                popPlayerOptions.PlacementTarget = playerCard;
                popPlayerOptions.IsOpen = true;
                eventArguments.Handled = true;
            }
        }

        private void OnModerationOptionClicked(object sender, RoutedEventArgs eventArguments)
        {
            FrameworkElement option = sender as FrameworkElement;
            LobbyPlayerEntry player = popPlayerOptions.DataContext as LobbyPlayerEntry;
            popPlayerOptions.IsOpen = false;
            popPlayerOptions.DataContext = null;

            if (player != null && option?.Tag is ModerationMode mode)
            {
                _viewModel.ModerationNavigation.Open(player.RoomId, player.UserId, mode);
            }
        }

        private void OnCancelPlayerMenuClicked(object sender, RoutedEventArgs eventArguments)
        {
            popPlayerOptions.IsOpen = false;
            popPlayerOptions.DataContext = null;
        }
    }
}