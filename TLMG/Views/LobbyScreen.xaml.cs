using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace TLMG.Views
{
    public partial class LobbyScreen : LocalizableScreen
    {
        private const int TotalLobbyPlayersCount = 3;
        private const int CountdownSecondsLimit = 3;
        private const int ToastDurationMilliseconds = 3000;
        private const string FormatReadyFraction = "{0}/{1}";

        private bool _isLocalPlayerReady;
        private bool _isPlayer2Ready;
        private bool _isPlayer3Ready;

        private int _readyPlayersCount;
        private int _currentCountdownSeconds;

        private bool _hasVotedToKick;
        private bool _isCountdownActive;

        private DispatcherTimer _countdownTimer;
        private DispatcherTimer _toastTimer;

        public LobbyScreen()
        {
            InitializeComponent();

            _isLocalPlayerReady = false;
            _isPlayer2Ready = true;
            _isPlayer3Ready = false;
            _hasVotedToKick = false;
            _isCountdownActive = false;

            InitializeTimers();
            UpdateReadyCountAndUI();
        }

        protected override void UpdateDynamicLabels()
        {
            lblReadyFraction.Text = string.Format(FormatReadyFraction, _readyPlayersCount, TotalLobbyPlayersCount);
        }

        private void InitializeTimers()
        {
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;

            _toastTimer = new DispatcherTimer();
            _toastTimer.Interval = TimeSpan.FromMilliseconds(ToastDurationMilliseconds);
            _toastTimer.Tick += ToastTimer_Tick;
        }

        private void BtnLeaveLobby_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                TryFindResource("Lobby_LeaveConfirmMsg") as string,
                TryFindResource("Lobby_LeaveConfirmTitle") as string,
                MessageBoxButton.YesNo);

            if (confirmationResult == MessageBoxResult.Yes)
            {
                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ClearHistory(); 
                applicationMainWindow.ShowScreen(new MainMenuScreen(), false); 
            }
        }

        private void BtnInviteFriends_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new FriendsScreen());
        }

        private void CmbGameMode_SelectionChanged(object eventSender, SelectionChangedEventArgs eventArguments)
        {
            if (IsLoaded)
            {
                _isLocalPlayerReady = false;
                _isPlayer2Ready = false;
                _isPlayer3Ready = false;
                UpdateReadyCountAndUI();
            }
        }

        private void BrdPlayer1_MouseLeftButtonDown(object eventSender, MouseButtonEventArgs eventArguments)
        {
            if (!_isCountdownActive)
            {
                _isLocalPlayerReady = !_isLocalPlayerReady;
                UpdateReadyCountAndUI();
            }
        }

        private void BrdPlayer2_MouseLeftButtonDown(object eventSender, MouseButtonEventArgs eventArguments)
        {
            if (!_isCountdownActive)
            {
                _isPlayer2Ready = !_isPlayer2Ready;
                UpdateReadyCountAndUI();
            }
        }

        private void BrdPlayer3_MouseLeftButtonDown(object eventSender, MouseButtonEventArgs eventArguments)
        {
            if (!_isCountdownActive)
            {
                _isPlayer3Ready = !_isPlayer3Ready;
                UpdateReadyCountAndUI();
            }
        }

        private void BrdPlayer_MouseRightButtonDown(object eventSender, MouseButtonEventArgs eventArguments)
        {
            if (!_isCountdownActive)
            {
                popPlayerOptions.IsOpen = true;
            }
        }

        private void BtnMenuCancel_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            popPlayerOptions.IsOpen = false;
        }

        private void BtnMenuKick_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            popPlayerOptions.IsOpen = false;
            grdKickModal.Visibility = Visibility.Visible;
            grdMainCenter.IsEnabled = false;
        }

        private void BtnCancelKick_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            grdKickModal.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;
        }

        private void BtnConfirmKick_Click(object eventSender, RoutedEventArgs eventArguments)
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

        private void ToastTimer_Tick(object eventSender, EventArgs eventArguments)
        {
            _toastTimer.Stop();
            brdToast.Visibility = Visibility.Collapsed;
        }

        private void UpdateReadyCountAndUI()
        {
            int currentReadyCount = 0;

            if (_isLocalPlayerReady) currentReadyCount++;
            if (_isPlayer2Ready) currentReadyCount++;
            if (_isPlayer3Ready) currentReadyCount++;

            _readyPlayersCount = currentReadyCount;
            UpdateDynamicLabels();

            ApplyPlayerStyle(brdPlayer1, brdStatus1, lblStatus1, _isLocalPlayerReady);
            ApplyPlayerStyle(brdPlayer2, brdStatus2, lblStatus2, _isPlayer2Ready);
            ApplyPlayerStyle(brdPlayer3, brdStatus3, lblStatus3, _isPlayer3Ready);

            bool areAllPlayersReady = _readyPlayersCount == TotalLobbyPlayersCount;

            if (areAllPlayersReady && !_isCountdownActive)
            {
                StartCountdown();
            }
        }

        private void ApplyPlayerStyle(Border cardBorder, Border statusBadge, TextBlock statusLabel, bool isReady)
        {
            if (isReady)
            {
                cardBorder.Style = (Style)FindResource("LobbyCardReadyStyle");
                statusBadge.Style = (Style)FindResource("StatusBadgeReadyStyle");
                statusLabel.Style = (Style)FindResource("StatusTextReadyStyle");
                statusLabel.Text = TryFindResource("Lobby_StatusReady") as string;
            }
            else
            {
                cardBorder.Style = (Style)FindResource("LobbyCardNotReadyStyle");
                statusBadge.Style = (Style)FindResource("StatusBadgeNotReadyStyle");
                statusLabel.Style = (Style)FindResource("StatusTextNotReadyStyle");
                statusLabel.Text = TryFindResource("Lobby_StatusNotReady") as string;
            }
        }

        private void StartCountdown()
        {
            _isCountdownActive = true;
            _currentCountdownSeconds = CountdownSecondsLimit;
            lblCountdownTimer.Text = _currentCountdownSeconds.ToString();

            grdMainCenter.IsEnabled = false;
            btnLeaveLobby.IsEnabled = false;
            btnInviteFriends.IsEnabled = false;
            grdCountdown.Visibility = Visibility.Visible;

            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object eventSender, EventArgs eventArguments)
        {
            _currentCountdownSeconds--;

            if (_currentCountdownSeconds > 0)
            {
                lblCountdownTimer.Text = _currentCountdownSeconds.ToString();
            }
            else
            {
                _countdownTimer.Stop();
                // TODO: Navegar a GameplayScreen cuando se termine el tiempo
                MessageBox.Show("Iniciando partida...");
            }
        }

        private void BtnCancelCountdown_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            _countdownTimer.Stop();
            _isCountdownActive = false;

            grdCountdown.Visibility = Visibility.Collapsed;
            grdMainCenter.IsEnabled = true;
            btnLeaveLobby.IsEnabled = true;
            btnInviteFriends.IsEnabled = true;

            _isLocalPlayerReady = false;
            UpdateReadyCountAndUI();
        }

        private void TxtChatInput_KeyDown(object eventSender, KeyEventArgs eventArguments)
        {
            bool isEnterPressed = eventArguments.Key == Key.Enter;

            if (isEnterPressed)
            {
                ProcessOutgoingChatMessage();
            }
        }

        private void BtnSendChat_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ProcessOutgoingChatMessage();
        }

        private void ProcessOutgoingChatMessage()
        {
            string messageText = txtChatInput.Text.Trim();
            bool isMessageValid = !string.IsNullOrWhiteSpace(messageText);

            if (isMessageValid)
            {
                TextBlock newMessageBlock = new TextBlock();
                newMessageBlock.Margin = new Thickness(0, 0, 0, 8);

                Run usernameRun = new Run("WhiskerKnight: ");
                usernameRun.Foreground = (System.Windows.Media.Brush)FindResource("Brush_Gold");
                usernameRun.FontWeight = FontWeights.Bold;

                Run messageRun = new Run(messageText);
                messageRun.Foreground = (System.Windows.Media.Brush)FindResource("Brush_TextPrimary");

                newMessageBlock.Inlines.Add(usernameRun);
                newMessageBlock.Inlines.Add(messageRun);

                pnlChatMessages.Children.Add(newMessageBlock);

                txtChatInput.Text = string.Empty;
                scrChat.ScrollToEnd();
            }
        }
    }
}