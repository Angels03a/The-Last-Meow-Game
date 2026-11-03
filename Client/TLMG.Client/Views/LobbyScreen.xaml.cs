using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

using TLMG.Client.Communication;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de sala previa a la partida (lobby): crea la sala del jugador registrado, o muestra la que ya tiene,
    /// con los jugadores que entrega el servidor. La sala vive en el ServerConnectionManager, no en la pantalla.
    /// </summary>
    public partial class LobbyScreen : LocalizableScreen
    {
        private const int LobbyStartCountdownSeconds = 3;
        private const int CountdownSecondsLimitForStart = 0;
        private const int CountdownTickIntervalSeconds = 1;
        private const int ToastDurationMilliseconds = 3000;

        // Máximo de jugadores por sala (Reglas de The Last Meow, §1.1.1); solo se muestra, el servidor lo aplica.
        private const int MaximumPlayersPerRoom = 6;
        private const int EmptyPlayerCount = 0;
        private const string RoomInformationFormatKey = "Lobby_RoomInformationFormat";
        private const string GuestCannotCreateRoomErrorKey = "Lobby_GuestCannotCreateRoomError";
        private const string ErrorTitleKey = "Lobby_ErrorTitle";

        private int _currentCountdownSeconds;

        private bool _hasVotedToKick;
        private bool _isCountdownActive;

        private DispatcherTimer _countdownTimer;
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
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            LobbyRoom currentRoom = ServerConnection.CurrentRoom;
            string roomInformation = string.Empty;

            if (currentRoom != null)
            {
                string roomInformationFormat = TryFindResource(RoomInformationFormatKey) as string ?? "{0} {1}/{2}";
                roomInformation = string.Format(
                    roomInformationFormat,
                    currentRoom.RoomId,
                    CountPlayers(currentRoom),
                    MaximumPlayersPerRoom);
            }

            lblRoomInformation.Text = roomInformation;
        }

        // Loaded se repite cuando la pantalla vuelve del historial (por ejemplo, desde FriendsScreen): por eso la
        // suscripción va aquí y no en el constructor, y la sala se pide solo si el jugador todavía no tiene una.
        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            ServerConnection.CreateRoomResultReceived -= OnCreateRoomResultReceived;
            ServerConnection.CreateRoomResultReceived += OnCreateRoomResultReceived;
            ServerConnection.LobbyRoomChanged -= OnLobbyRoomChanged;
            ServerConnection.LobbyRoomChanged += OnLobbyRoomChanged;
            ShowOrCreateRoom();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            ServerConnection.CreateRoomResultReceived -= OnCreateRoomResultReceived;
            ServerConnection.LobbyRoomChanged -= OnLobbyRoomChanged;
            _countdownTimer?.Stop();
            _toastTimer?.Stop();
        }

        private void InitializeTimers()
        {
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(CountdownTickIntervalSeconds);
            _countdownTimer.Tick += CountdownTimer_Tick;

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

        private void StartCountdown()
        {
            _isCountdownActive = true;
            _currentCountdownSeconds = LobbyStartCountdownSeconds;
            lblCountdownTimer.Text = _currentCountdownSeconds.ToString();

            grdMainCenter.IsEnabled = false;
            btnLeaveLobby.IsEnabled = false;
            btnInviteFriends.IsEnabled = false;
            grdCountdown.Visibility = Visibility.Visible;

            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _currentCountdownSeconds--;

            // FIXME: Decidir si es variable local o constante, ya que solo se usa aquí. (BL-043).
            if (_currentCountdownSeconds > CountdownSecondsLimitForStart)
            {
                lblCountdownTimer.Text = _currentCountdownSeconds.ToString();
            }
            else
            {
                _countdownTimer.Stop();
                // TODO: Navegar a GameplayScreen al terminar la cuenta regresiva, cuando exista. (BL-046).
                MessageBox.Show(TryFindResource("Lobby_StartGameMessage") as string);
            }
        }

        private void BtnCancelCountdown_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer.Stop();
            _isCountdownActive = false;

            grdCountdown.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;
            btnLeaveLobby.IsEnabled = true;
            btnInviteFriends.IsEnabled = true;
        }

        private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            bool isEnterPressed = e.Key == Key.Enter;

            if (isEnterPressed)
            {
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
            bool isMessageValid = !string.IsNullOrWhiteSpace(messageText);

            if (isMessageValid)
            {
                var newMessageBlock = new TextBlock();
                newMessageBlock.Margin = new Thickness(0, 0, 0, 8);

                var usernameRun = new Run("WhiskerKnight: ");
                usernameRun.Foreground = (System.Windows.Media.Brush)FindResource("Brush_Gold");
                usernameRun.FontWeight = FontWeights.Bold;

                var messageRun = new Run(messageText);
                messageRun.Foreground = (System.Windows.Media.Brush)FindResource("Brush_TextPrimary");

                newMessageBlock.Inlines.Add(usernameRun);
                newMessageBlock.Inlines.Add(messageRun);

                pnlChatMessages.Children.Add(newMessageBlock);

                txtChatInput.Text = string.Empty;
                scrChat.ScrollToEnd();
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
            LobbyPlayer[] roomPlayers = new LobbyPlayer[EmptyPlayerCount];

            if (currentRoom != null && currentRoom.Players != null)
            {
                roomPlayers = currentRoom.Players;
            }

            lstLobbyPlayers.ItemsSource = roomPlayers;
            UpdateDynamicLabels();
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