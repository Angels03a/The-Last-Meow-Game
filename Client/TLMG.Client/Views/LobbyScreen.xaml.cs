using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de sala previa a la partida (lobby): crea la sala del jugador registrado, o muestra la que ya tiene,
    /// con los jugadores que entrega el servidor. La sala vive en el ServerConnectionManager, no en la pantalla.
    /// </summary>
    public partial class LobbyScreen : LocalizableScreen
    {
        private const int ToastDurationMilliseconds = 3000;

        // Máximo de jugadores por sala (Reglas de The Last Meow, §1.1.1); solo se muestra, el servidor lo aplica.
        private const int MaximumPlayersPerRoom = 6;
        private const int EmptyPlayerCount = 0;
        private const string RoomInformationFormatKey = "Lobby_RoomInformationFormat";
        private const string GuestCannotCreateRoomErrorKey = "Lobby_GuestCannotCreateRoomError";
        private const string ErrorTitleKey = "Lobby_ErrorTitle";


        private bool _isLocalPlayerReady;
        private bool _hasVotedToKick;
        private bool _isCountdownActive;

        private DispatcherTimer _toastTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LobbyScreen"/>.
        /// </summary>
        public LobbyScreen()
        {
            InitializeComponent();
            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;

            _hasVotedToKick = false;
            _isCountdownActive = false;

            InitializeTimers();
            txtChatInput.MaxLength = ChatInputHelper.MaximumMessageCharacters;
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            LobbyRoom currentRoom = ServerConnection.CurrentRoom;
            string roomInformation = string.Empty;

            if (currentRoom != null)
            {
                string roomInformationFormat = TryFindResource(RoomInformationFormatKey) as string ?? string.Empty;
                roomInformation = string.Format(
                    roomInformationFormat,
                    currentRoom.RoomId,
                    CountPlayers(currentRoom),
                    MaximumPlayersPerRoom);
            }

            lblRoomInformation.Text = roomInformation;
            ShowCountdown();
        }

        // Loaded se repite cuando la pantalla vuelve del historial (por ejemplo, desde FriendsScreen): por eso la
        // suscripción va aquí y no en el constructor, y la sala se pide solo si el jugador todavía no tiene una.
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
            pnlChatMessages.Children.Clear();
            _toastTimer?.Stop();
        }

        private void InitializeTimers()
        {
            _toastTimer = new DispatcherTimer();
            _toastTimer.Interval = TimeSpan.FromMilliseconds(ToastDurationMilliseconds);
            _toastTimer.Tick += ToastTimer_Tick;
        }

        private void BtnLeaveLobby_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                TryFindResource("Lobby_LeaveConfirmMessage") as string,
                TryFindResource("Lobby_LeaveConfirmTitle") as string,
                MessageBoxButton.YesNo);

            if (confirmationResult == MessageBoxResult.Yes)
            {
                ServerConnection.LeaveLobby();
                var applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ClearHistory();
                applicationMainWindow.ShowScreen(new MainMenuScreen(), false);
            }
        }

        private void BtnInviteFriends_Click(object sender, RoutedEventArgs e)
        {
            var applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new FriendsScreen());
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
            btnMenuKick.Content = TryFindResource("Lobby_MenuKickVoted") as string;
            btnMenuKick.IsEnabled = false;

            ShowToastNotification();
        }

        private void ShowToastNotification()
        {
            brdToast.Visibility = Visibility.Visible;
            _toastTimer.Start();
        }

        private void ToastTimer_Tick(object sender, EventArgs e)
        {
            _toastTimer.Stop();
            brdToast.Visibility = Visibility.Collapsed;
        }

        private void BtnToggleReady_Click(object sender, RoutedEventArgs eventArguments)
        {
            ServerConnection.ChangePlayerReadiness(!_isLocalPlayerReady);
        }

        private void BtnCancelCountdown_Click(object sender, RoutedEventArgs eventArguments)
        {
            ServerConnection.ChangePlayerReadiness(false);
        }

        private void OnCountdownChanged(object sender, EventArgs eventArguments)
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

            string countdownFormat = TryFindResource("Lobby_CountdownSecondsFormat") as string ?? string.Empty;
            if (countdown.SecondsRemaining > 0)
            {
                lblCountdownTimer.FontSize = 120;
                lblCountdownTimer.Text = string.Format(countdownFormat, countdown.SecondsRemaining);
            }
            else
            {
                lblCountdownTimer.FontSize = 32;
                lblCountdownTimer.Text = TryFindResource("Lobby_CountdownWaitingLabel") as string ?? string.Empty;
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

        private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            bool isEnterPressed = e.Key == Key.Enter;

            if (isEnterPressed)
            {
                e.Handled = true;
                AppendOutgoingChatMessage();
            }
        }

        private void BtnSendChat_Click(object sender, RoutedEventArgs e)
        {
            AppendOutgoingChatMessage();
        }

        private void AppendOutgoingChatMessage()
        {
            string messageText = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(messageText))
            {
                ShowLobbyError("Lobby_ChatEmptyError");
            }
            else if (ChatInputHelper.CanEnterText(messageText))
            {
                if (ServerConnection.SendChatMessage(messageText))
                {
                    txtChatInput.Clear();
                }
            }
        }

        private void OnChatMessageReceived(object sender, ServerEventArgs<ChatMessage> eventArguments)
        {
            ChatMessage chatMessage = eventArguments.Payload;
            TextBlock messageBlock = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Run senderRun = new Run(chatMessage.SenderNickname);
            senderRun.FontWeight = FontWeights.Bold;
            string senderBrush = "Brush_TextPrimary";
            if (chatMessage.SenderUserId == ServerConnection.PlayerUserId)
            {
                senderBrush = "Brush_Gold";
            }
            senderRun.SetResourceReference(TextElement.ForegroundProperty, senderBrush);
            messageBlock.Inlines.Add(senderRun);
            messageBlock.Inlines.Add(new LineBreak());
            Run messageRun = new Run(chatMessage.Text);
            messageRun.SetResourceReference(TextElement.ForegroundProperty, "Brush_TextPrimary");
            messageBlock.Inlines.Add(messageRun);
            pnlChatMessages.Children.Add(messageBlock);
            scrChat.ScrollToEnd();
        }

        private void OnChatAvailabilityChanged(object sender, EventArgs eventArguments)
        {
            ShowChatAvailability();
        }

        private void ShowChatAvailability()
        {
            txtChatInput.IsEnabled = ServerConnection.IsChatSubscribed;
            btnSendChat.IsEnabled = ServerConnection.IsChatSubscribed;
            lblGuestChatNotice.Visibility = Visibility.Collapsed;
            if (!ServerConnection.HasRegisteredSession)
            {
                lblGuestChatNotice.Visibility = Visibility.Visible;
            }
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
                // Si el envío falla, el manager dispara ConnectionLost y MainWindow regresa al inicio de sesión.
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

        // Muestra solo los jugadores que trajo el servidor; sin sala, la lista queda vacía.
        private void ShowCurrentRoom()
        {
            LobbyRoom currentRoom = ServerConnection.CurrentRoom;
            List<LobbyPlayerEntry> playerEntries = new List<LobbyPlayerEntry>();

            if (currentRoom != null && currentRoom.Players != null)
            {
                foreach (LobbyPlayer roomPlayer in currentRoom.Players)
                {
                    playerEntries.Add(new LobbyPlayerEntry
                    {
                        Nickname = roomPlayer.Nickname,
                        IsLocalPlayer = roomPlayer.UserId == ServerConnection.PlayerUserId,
                        IsReady = roomPlayer.IsReady
                    });
                }
            }

            lstLobbyPlayers.ItemsSource = playerEntries;
            ShowReadinessButton(playerEntries, currentRoom);
            UpdateDynamicLabels();
        }

        private void ShowReadinessButton(List<LobbyPlayerEntry> playerEntries, LobbyRoom room)
        {
            bool hasLocalPlayer = false;
            _isLocalPlayerReady = false;

            foreach (LobbyPlayerEntry playerEntry in playerEntries)
            {
                if (playerEntry.IsLocalPlayer)
                {
                    hasLocalPlayer = true;
                    _isLocalPlayerReady = playerEntry.IsReady;
                    break;
                }
            }

            string buttonKey = "Lobby_MarkReadyButton";

            if (_isLocalPlayerReady)
            {
                buttonKey = "Lobby_MarkNotReadyButton";
            }

            bool isWaitingRoom = room != null && !room.InGame;
            btnToggleReady.IsEnabled = hasLocalPlayer && isWaitingRoom;
            btnToggleReady.SetResourceReference(ContentControl.ContentProperty, buttonKey);
        }

        private static int CountPlayers(LobbyRoom lobbyRoom)
        {
            int playersCount = EmptyPlayerCount;

            if (lobbyRoom.Players != null)
            {
                playersCount = lobbyRoom.Players.Length;
            }

            return playersCount;
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