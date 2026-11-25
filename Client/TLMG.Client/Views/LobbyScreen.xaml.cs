using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Displays the lobby before a match. The view filters chat input, scrolls chat and keeps the removal controls
    /// hidden (BL-082, BL-161). <see cref="LobbyViewModel"/> manages the room, readiness, cards per player, countdown
    /// and chat.
    /// </summary>
    public partial class LobbyScreen : LocalizableScreen
    {
        private const int ToastDurationMilliseconds = 3000;
        private const string MenuKickVotedKey = "Lobby_MenuKickVoted";

        private readonly LobbyViewModel _viewModel;
        private readonly DispatcherTimer _toastTimer;

        private bool _hasVotedToKick;

        /// <summary>
        /// Creates a <see cref="LobbyScreen"/> with its ViewModel.
        /// </summary>
        /// <param name="lobbyViewModel">ViewModel created and supplied by the screen factory.</param>
        public LobbyScreen(LobbyViewModel lobbyViewModel)
        {
            InitializeComponent();
            _viewModel = lobbyViewModel;
            DataContext = lobbyViewModel;

            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ToastDurationMilliseconds)
            };
            _toastTimer.Tick += OnToastTimerTick;

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
            _viewModel.ChatMessages.CollectionChanged -= OnChatMessagesChanged;
            _viewModel.Deactivate();
            _toastTimer.Stop();
        }

        private void OnChatMessagesChanged(object sender, NotifyCollectionChangedEventArgs eventArguments)
        {
            scrChat.ScrollToEnd();
        }

        private void BrdPlayer_MouseRightButtonDown(object sender, MouseButtonEventArgs eventArguments)
        {
            if (!_viewModel.IsCountdownVisible)
            {
                popPlayerOptions.IsOpen = true;
            }
        }

        private void BtnMenuCancel_Click(object sender, RoutedEventArgs eventArguments)
        {
            popPlayerOptions.IsOpen = false;
        }

        private void BtnMenuKick_Click(object sender, RoutedEventArgs eventArguments)
        {
            popPlayerOptions.IsOpen = false;
            grdKickModal.Visibility = Visibility.Visible;
            grdMainCenter.IsEnabled = false;
        }

        private void BtnCancelKick_Click(object sender, RoutedEventArgs eventArguments)
        {
            grdKickModal.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;
        }

        private void BtnConfirmKick_Click(object sender, RoutedEventArgs eventArguments)
        {
            grdKickModal.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;

            btnMenuKick.Content = TryFindResource(MenuKickVotedKey) as string;
            btnMenuKick.IsEnabled = false;
            brdToast.Visibility = Visibility.Visible;
            _toastTimer.Start();
        }

        private void OnToastTimerTick(object sender, EventArgs eventArguments)
        {
            _toastTimer.Stop();
            brdToast.Visibility = Visibility.Collapsed;
        }
    }
}
