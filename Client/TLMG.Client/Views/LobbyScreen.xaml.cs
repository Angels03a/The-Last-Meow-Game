using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de sala previa a la partida (lobby): crea la sala del jugador registrado, o muestra la que ya tiene,
    /// con los jugadores que entrega el servidor. La sala vive en el ServerConnectionManager, no en la pantalla.
    /// </summary>
    public partial class LobbyScreen : LocalizableScreen
    {
        private const int ToastDurationMilliseconds = 3000;
        private const int NoSecondsRemaining = 0;
        private const double CountdownLargeFontSize = 120;
        private const double CountdownSmallFontSize = 32;
        private const bool SaveInHistory = false;

        private const string RoomInformationFormatKey = "Lobby_RoomInformationFormat";
        private const string GuestCannotCreateRoomErrorKey = "Lobby_GuestCannotCreateRoomError";
        private const string ErrorTitleKey = "Lobby_ErrorTitle";
        private const string LeaveConfirmMessageKey = "Lobby_LeaveConfirmMessage";
        private const string LeaveConfirmTitleKey = "Lobby_LeaveConfirmTitle";
        private const string CountdownSecondsFormatKey = "Lobby_CountdownSecondsFormat";
        private const string CountdownWaitingLabelKey = "Lobby_CountdownWaitingLabel";
        private const string MenuKickVotedKey = "Lobby_MenuKickVoted";

        private readonly LobbyViewModel _viewModel;
        private readonly DispatcherTimer _toastTimer;

        private bool _hasVotedToKick;
        private bool _isCountdownActive;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LobbyScreen"/>.
        /// </summary>
        public LobbyScreen()
        {
            InitializeComponent();

            _viewModel = new LobbyViewModel();
            _viewModel.LeaveRequested += OnLeaveRequested;
            _viewModel.InviteFriendsRequested += OnInviteFriendsRequested;
            _viewModel.ReadinessChangeRequested += OnReadinessChangeRequested;
            _viewModel.CancelCountdownRequested += OnCancelCountdownRequested;
            _viewModel.ChatSendRequested += OnChatSendRequested;
            _viewModel.ErrorRequested += OnErrorRequested;
            _viewModel.ChatMessages.CollectionChanged += OnChatMessagesChanged;
            DataContext = _viewModel;

            _hasVotedToKick = false;
            _isCountdownActive = false;

            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ToastDurationMilliseconds)
            };
            _toastTimer.Tick += OnToastTimerTick;

            txtChatInput.MaxLength = ChatInputHelper.MaximumMessageCharacters;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            string roomInformation = string.Empty;

            if (_viewModel.HasRoom)
            {
                string roomInformationFormat = TryFindResource(RoomInformationFormatKey) as string ?? string.Empty;
                roomInformation = string.Format(
                    roomInformationFormat,
                    _viewModel.RoomId,
                    _viewModel.PlayerCount,
                    _viewModel.MaximumPlayers);
            }

            lblRoomInformation.Text = roomInformation;
            ShowCountdown();
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            ServerConnection.CreateRoomResultReceived -= OnCreateRoomResultReceived;
            ServerConnection.CreateRoomResultReceived += OnCreateRoomResultReceived;
            ServerConnection.LobbyRoomChanged -= OnLobbyRoomChanged;
            ServerConnection.LobbyRoomChanged += OnLobbyRoomChanged;
            ServerConnection.ReadinessResultReceived -= OnReadinessResultReceived;
            ServerConnection.ReadinessResultReceived += OnReadinessResultReceived;
            ServerConnection.MatchStartResultReceived -= OnMatchStartResultReceived;
            ServerConnection.MatchStartResultReceived += OnMatchStartResultReceived;
            ServerConnection.Countdown.Changed -= OnCountdownChanged;
            ServerConnection.Countdown.Changed += OnCountdownChanged;
            ServerConnection.ChatMessageReceived -= OnChatMessageReceived;
            ServerConnection.ChatMessageReceived += OnChatMessageReceived;
            ServerConnection.ChatAvailabilityChanged -= OnChatAvailabilityChanged;
            ServerConnection.ChatAvailabilityChanged += OnChatAvailabilityChanged;
            ShowChatAvailability();
            ShowOrCreateRoom();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            ServerConnection.CreateRoomResultReceived -= OnCreateRoomResultReceived;
            ServerConnection.LobbyRoomChanged -= OnLobbyRoomChanged;
            ServerConnection.ReadinessResultReceived -= OnReadinessResultReceived;
            ServerConnection.MatchStartResultReceived -= OnMatchStartResultReceived;
            ServerConnection.Countdown.Changed -= OnCountdownChanged;
            ServerConnection.ChatMessageReceived -= OnChatMessageReceived;
            ServerConnection.ChatAvailabilityChanged -= OnChatAvailabilityChanged;
            _viewModel.ClearChatMessages();
            _toastTimer.Stop();
        }

        private void OnLeaveRequested(object sender, EventArgs e)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                TryFindResource(LeaveConfirmMessageKey) as string,
                TryFindResource(LeaveConfirmTitleKey) as string,
                MessageBoxButton.YesNo);

            bool hasConfirmedLeave = confirmationResult == MessageBoxResult.Yes;

            if (hasConfirmedLeave)
            {
                ServerConnection.LeaveLobby();
                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ClearHistory();
                applicationMainWindow.ShowScreen(new MainMenuScreen(), SaveInHistory);
            }
        }

        private void OnInviteFriendsRequested(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new FriendsScreen());
        }

        private void OnReadinessChangeRequested(object sender, EventArgs e)
        {
            ServerConnection.ChangePlayerReadiness(!_viewModel.IsLocalPlayerReady);
        }

        private void OnCancelCountdownRequested(object sender, EventArgs e)
        {
            ServerConnection.ChangePlayerReadiness(false);
        }

        private void OnChatSendRequested(object sender, EventArgs e)
        {
            bool wasMessageSent = ServerConnection.SendChatMessage(_viewModel.PendingChatText);

            if (wasMessageSent)
            {
                _viewModel.ClearChatInput();
            }
        }

        private void OnErrorRequested(object sender, EventArgs e)
        {
            ShowLobbyError(_viewModel.LastErrorKey);
        }

        private void OnChatMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            scrChat.ScrollToEnd();
        }

        private void BrdPlayer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isCountdownActive)
            {
                popPlayerOptions.IsOpen = true;
            }
        }

        private void BtnMenuCancel_Click(object sender, RoutedEventArgs e)
        {
            popPlayerOptions.IsOpen = false;
        }

        private void BtnMenuKick_Click(object sender, RoutedEventArgs e)
        {
            popPlayerOptions.IsOpen = false;
            grdKickModal.Visibility = Visibility.Visible;
            grdMainCenter.IsEnabled = false;
        }

        private void BtnCancelKick_Click(object sender, RoutedEventArgs e)
        {
            grdKickModal.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;
        }

        private void BtnConfirmKick_Click(object sender, RoutedEventArgs e)
        {
            grdKickModal.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;

            _hasVotedToKick = true;
            btnMenuKick.Content = TryFindResource(MenuKickVotedKey) as string;
            btnMenuKick.IsEnabled = false;

            ShowToastNotification();
        }

        private void ShowToastNotification()
        {
            brdToast.Visibility = Visibility.Visible;
            _toastTimer.Start();
        }

        private void OnToastTimerTick(object sender, EventArgs e)
        {
            _toastTimer.Stop();
            brdToast.Visibility = Visibility.Collapsed;
        }

        private void OnCountdownChanged(object sender, EventArgs e)
        {
            ShowCountdown();
        }

        private void ShowCountdown()
        {
            LobbyCountdown countdown = ServerConnection.Countdown;
            _isCountdownActive = countdown.IsVisible;
            grdCountdown.Visibility = Visibility.Collapsed;

            if (countdown.IsVisible)
            {
                grdCountdown.Visibility = Visibility.Visible;
            }

            string countdownFormat = TryFindResource(CountdownSecondsFormatKey) as string ?? string.Empty;

            if (countdown.SecondsRemaining > NoSecondsRemaining)
            {
                lblCountdownTimer.FontSize = CountdownLargeFontSize;
                lblCountdownTimer.Text = string.Format(countdownFormat, countdown.SecondsRemaining);
            }
            else
            {
                lblCountdownTimer.FontSize = CountdownSmallFontSize;
                lblCountdownTimer.Text = TryFindResource(CountdownWaitingLabelKey) as string ?? string.Empty;
            }
        }

        private void OnReadinessResultReceived(
            object sender,
            ServerEventArgs<OperationChangePlayerReadinessResult> eventArguments)
        {
            if (!eventArguments.Payload.IsSuccess)
            {
                ShowLobbyError(eventArguments.Payload.ErrorKey);
            }
        }

        private void OnMatchStartResultReceived(
            object sender,
            ServerEventArgs<OperationRequestMatchStartResult> eventArguments)
        {
            if (!eventArguments.Payload.IsSuccess)
            {
                ShowLobbyError(eventArguments.Payload.ErrorKey);
            }
        }

        private void OnChatMessageReceived(object sender, ServerEventArgs<ChatMessage> eventArguments)
        {
            ChatMessage chatMessage = eventArguments.Payload;

            ChatMessageEntry chatMessageEntry = new ChatMessageEntry
            {
                SenderNickname = chatMessage.SenderNickname,
                Text = chatMessage.Text,
                IsLocalSender = chatMessage.SenderUserId == ServerConnection.PlayerUserId
            };

            _viewModel.AddChatMessage(chatMessageEntry);
        }

        private void OnChatAvailabilityChanged(object sender, EventArgs e)
        {
            ShowChatAvailability();
        }

        private void ShowChatAvailability()
        {
            _viewModel.ApplyChatAvailability(ServerConnection.IsChatSubscribed, ServerConnection.HasRegisteredSession);
        }

        private bool CanInsertChatText(string insertedText)
        {
            string proposedText = txtChatInput.Text.Remove(txtChatInput.SelectionStart, txtChatInput.SelectionLength);
            proposedText = proposedText.Insert(txtChatInput.SelectionStart, insertedText);
            bool canInsert = ChatInputHelper.CanEnterText(proposedText);

            return canInsert;
        }

        private void OnChatPreviewTextInput(object sender, TextCompositionEventArgs eventArguments)
        {
            eventArguments.Handled = !CanInsertChatText(eventArguments.Text);
        }

        private void OnChatPasting(object sender, DataObjectPastingEventArgs eventArguments)
        {
            string pastedText = eventArguments.DataObject.GetData(DataFormats.UnicodeText) as string;
            bool canPaste = pastedText != null && CanInsertChatText(pastedText);

            if (!canPaste)
            {
                eventArguments.CancelCommand();
            }
        }

        private void ShowOrCreateRoom()
        {
            ShowCurrentRoom();
            bool needsRoom = ServerConnection.CurrentRoom == null && !ServerConnection.IsRoomCreationPending;

            if (needsRoom && !ServerConnection.HasRegisteredSession)
            {
                ShowLobbyError(GuestCannotCreateRoomErrorKey);
            }
            else if (needsRoom)
            {
                ServerConnection.CreateRoom();
            }
        }

        private void OnCreateRoomResultReceived(
            object sender,
            ServerEventArgs<OperationCreateRoomResult> serverEventArguments)
        {
            if (serverEventArguments.Payload.IsSuccess)
            {
                ShowCurrentRoom();
            }
            else
            {
                ShowLobbyError(serverEventArguments.Payload.ErrorKey);
            }
        }

        private void OnLobbyRoomChanged(object sender, ServerEventArgs<LobbyRoom> serverEventArguments)
        {
            ShowCurrentRoom();
        }

        private void ShowCurrentRoom()
        {
            LobbyRoom currentRoom = ServerConnection.CurrentRoom;

            if (currentRoom != null)
            {
                List<LobbyPlayerEntry> playerEntries = BuildPlayerEntries(currentRoom);
                _viewModel.ShowRoom(currentRoom.RoomId, !currentRoom.InGame, playerEntries);
            }
            else
            {
                _viewModel.ClearRoom();
            }

            UpdateDynamicLabels();
        }

        private List<LobbyPlayerEntry> BuildPlayerEntries(LobbyRoom lobbyRoom)
        {
            List<LobbyPlayerEntry> playerEntries = new List<LobbyPlayerEntry>();

            if (lobbyRoom.Players != null)
            {
                foreach (LobbyPlayer roomPlayer in lobbyRoom.Players)
                {
                    playerEntries.Add(new LobbyPlayerEntry
                    {
                        Nickname = roomPlayer.Nickname,
                        IsLocalPlayer = roomPlayer.UserId == ServerConnection.PlayerUserId,
                        IsReady = roomPlayer.IsReady
                    });
                }
            }

            return playerEntries;
        }

        private void ShowLobbyError(string errorKey)
        {
            MessageBox.Show(
                ErrorMessageHelper.FindErrorMessage(this, errorKey),
                TryFindResource(ErrorTitleKey) as string,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}