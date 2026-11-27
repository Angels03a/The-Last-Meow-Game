using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using log4net;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;
using TLMG.Client.Views;

namespace TLMG.Client
{
    public partial class MainWindow : Window
    {
        private const string ConnectionLostMessageKey = "Common_ConnectionLostError";
        private const string RoomInvitationTitleKey = "Common_RoomInvitationTitle";
        private const string RoomInvitationMessageFormatKey = "Common_RoomInvitationMessageFormat";
        private const string ErrorTitleKey = "Common_ErrorTitle";
        private const string PlayerExpelledMessageKey = "Moderation_PlayerExpelledMessage";
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private const string InvitationNameFormat = "{0}";
        private const int NoRoomId = 0;
        private const int NoInvitationId = 0;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

        private readonly HashSet<Guid> _enteredMatchIds = new HashSet<Guid>();
        private Guid? _displayedMatchId;
        private bool _isInvitationDialogOpen;

        public MainWindow(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            DataContext = mainWindowViewModel;
            ServerConnection.ConnectionLost += OnServerConnectionLost;
            ServerConnection.RoomInvitationReceived += OnRoomInvitationReceived;
            ServerConnection.AcceptInvitationResultReceived += OnAcceptInvitationResultReceived;
            ServerConnection.EnterLobbyResultReceived += OnEnterLobbyResultReceived;
            ServerConnection.LobbyRoomChanged += OnAuthorizedRoomChanged;
            ServerConnection.MatchRecoveryReady += OnMatchRecoveryReady;
            ServerConnection.MatchContextEnded += OnMatchContextEnded;
            ServerConnection.Moderation.PlayerExpelled += OnPlayerExpelled;
            ServerConnection.ChatErrorReceived += OnChatErrorReceived;
            Closed += OnMainWindowClosed;
            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += OnLoadingCompleted;
            ShowScreen(loadingScreen, false);
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

        private static NavigationManager Navigation => ((App)Application.Current).Navigation;

        public void ShowScreen(UserControl newScreen, bool saveInHistory = true)
        {
            Navigation.ShowExistingScreen(newScreen, saveInHistory);
        }

        public void GoBack()
        {
            Navigation.GoBack();
        }

        public void ClearHistory()
        {
            Navigation.ClearHistory();
        }

        private void OnMainWindowClosed(object sender, EventArgs eventArguments)
        {
            ServerConnection.ChatErrorReceived -= OnChatErrorReceived;
            ServerConnection.LobbyRoomChanged -= OnAuthorizedRoomChanged;
            ServerConnection.MatchRecoveryReady -= OnMatchRecoveryReady;
            ServerConnection.MatchContextEnded -= OnMatchContextEnded;
            ServerConnection.Moderation.PlayerExpelled -= OnPlayerExpelled;
            ServerConnection.ConnectionLost -= OnServerConnectionLost;
            ServerConnection.RoomInvitationReceived -= OnRoomInvitationReceived;
            ServerConnection.AcceptInvitationResultReceived -= OnAcceptInvitationResultReceived;
            ServerConnection.EnterLobbyResultReceived -= OnEnterLobbyResultReceived;
        }

        private void OnLoadingCompleted(object sender, EventArgs eventArguments)
        {
            LoadingScreen loadingScreen = (LoadingScreen)sender;
            loadingScreen.LoadingCompleted -= OnLoadingCompleted;
            if (ReferenceEquals(Navigation.CurrentView, loadingScreen))
            {
                Navigation.NavigateToWithoutHistory(ScreenId.Login);
            }
        }

        private void OnServerConnectionLost(object sender, EventArgs eventArguments)
        {
            _enteredMatchIds.Clear();
            _displayedMatchId = null;
            Navigation.NavigateToWithoutHistory(ScreenId.Login);
            Navigation.ClearHistory();
            MessageBox.Show(TryFindResource(ConnectionLostMessageKey) as string);
        }

        private void OnRoomInvitationReceived(object sender, ServerEventArgs<RoomInvitation> serverEventArguments)
        {
            RoomInvitation roomInvitation = serverEventArguments?.Payload;

            if (roomInvitation == null || roomInvitation.InvitationId <= NoInvitationId)
            {
                _log.Warn("Invalid room invitation received.");
            }
            else if (!_isInvitationDialogOpen && ServerConnection.CanReceiveRoomInvitation)
            {
                ShowRoomInvitation(roomInvitation);
            }
        }

        private void ShowRoomInvitation(RoomInvitation roomInvitation)
        {
            string sessionToken = ServerConnection.SessionToken;
            _isInvitationDialogOpen = true;

            try
            {
                string invitationFormat = TryFindResource(RoomInvitationMessageFormatKey) as string
                    ?? InvitationNameFormat;
                MessageBoxResult invitationAnswer = MessageBox.Show(this,
                    string.Format(invitationFormat, roomInvitation.InviterNickname),
                    TryFindResource(RoomInvitationTitleKey) as string,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (invitationAnswer == MessageBoxResult.Yes && ServerConnection.SessionToken == sessionToken)
                {
                    if (!ServerConnection.AcceptRoomInvitation(roomInvitation.InvitationId))
                    {
                        ShowServerError(UnexpectedErrorKey);
                    }
                }
            }
            finally
            {
                _isInvitationDialogOpen = false;
            }
        }

        private void OnAcceptInvitationResultReceived(
            object sender,
            ServerEventArgs<OperationAcceptRoomInvitationResult> serverEventArguments)
        {
            OperationAcceptRoomInvitationResult result = ServerConnectionManager.NormalizeOperationResult(
                serverEventArguments?.Payload);

            if (result.IsSuccess && result.RoomId > NoRoomId && ServerConnection.HasSession)
            {
                if (!ServerConnection.EnterLobby(result.RoomId))
                {
                    ShowServerError(UnexpectedErrorKey);
                }
            }
            else if (ServerConnection.HasSession)
            {
                if (result.IsSuccess)
                {
                    _log.Warn("Successful invitation response did not identify a room.");
                    ShowServerError(UnexpectedErrorKey);
                }
                else
                {
                    ShowServerError(result.ErrorKey);
                }
            }
        }

        private void OnEnterLobbyResultReceived(
            object sender,
            ServerEventArgs<OperationLobbyRoomResult> serverEventArguments)
        {
            OperationLobbyRoomResult result = ServerConnectionManager.NormalizeOperationResult(
                serverEventArguments?.Payload);
            LobbyRoom currentRoom = ServerConnection.CurrentRoom;

            if (ServerConnection.HasSession)
            {
                if (result.IsSuccess && result.Room != null && currentRoom != null
                    && result.Room.RoomId == currentRoom.RoomId)
                {
                    if (!currentRoom.InGame)
                    {
                        ClearHistory();
                        Navigation.NavigateToWithoutHistory(ScreenId.Lobby);
                    }
                }
                else if (!result.IsSuccess)
                {
                    ShowServerError(result.ErrorKey);
                }
                else
                {
                    _log.Warn("Room entry confirmation did not match the current room.");
                    ShowServerError(UnexpectedErrorKey);
                }
            }
        }

        private void OnAuthorizedRoomChanged(object sender, ServerEventArgs<LobbyRoom> eventArguments)
        {
            LobbyRoom room = eventArguments?.Payload;
            bool hasMatch = room != null && room.MatchId.HasValue;
            bool isAuthorized = hasMatch && room.InGame;
            bool isKnownMode = hasMatch &&
                (room.CardsPerPlayerCount == MatchModeSettings.FourCardsPerPlayer
                || room.CardsPerPlayerCount == MatchModeSettings.SixCardsPerPlayer
                || room.CardsPerPlayerCount == MatchModeSettings.EightCardsPerPlayer);

            if (!ServerConnection.IsMatchRecoveryPending && !ServerConnection.IsMatchRecoveryExhausted
                && isAuthorized && isKnownMode && _enteredMatchIds.Add(room.MatchId.Value))
            {
                NavigateToAuthorizedMatch(room);
            }
        }

        private void NavigateToAuthorizedMatch(LobbyRoom room)
        {
            _displayedMatchId = room.MatchId;
            Navigation.ClearHistory();

            if (room.CardsPerPlayerCount == MatchModeSettings.EightCardsPerPlayer)
            {
                Navigation.NavigateToWithoutHistory(ScreenId.FugitiveKitten, room.MatchId.Value);
            }
            else if (room.CardsPerPlayerCount == MatchModeSettings.FourCardsPerPlayer
                || room.CardsPerPlayerCount == MatchModeSettings.SixCardsPerPlayer)
            {
                Navigation.NavigateToWithoutHistory(ScreenId.Gameplay);
            }
        }

        private void OnMatchContextEnded(object sender, EventArgs args)
        {
            _displayedMatchId = null;
            Navigation.ClearHistory();
            Navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }

        private void OnPlayerExpelled(object sender, ServerEventArgs<int> eventArguments)
        {
            if (eventArguments != null && eventArguments.Payload > NoRoomId && ServerConnection.HasSession)
            {
                OnMatchContextEnded(sender, eventArguments);
                MessageBox.Show(TryFindResource(PlayerExpelledMessageKey) as string);
            }
        }

        private void OnMatchRecoveryReady(object sender, ServerEventArgs<MatchView> args)
        {
            MatchView view = args?.Payload;
            bool canRestoreMatch = view != null && ServerConnection.CurrentRoom?.MatchId == view.MatchId
                && ServerConnection.IsMatchSubscribed;

            if (canRestoreMatch)
            {
                bool sameMatch = _displayedMatchId == view.MatchId;
                _enteredMatchIds.Add(view.MatchId);
                _displayedMatchId = view.MatchId;
                Navigation.ClearHistory();

                if (view.Phase == MatchRoundPhase.MatchFinished)
                {
                    if (!sameMatch || !(Navigation.CurrentView is GameOverScreen))
                    {
                        Navigation.NavigateToWithoutHistory(ScreenId.GameOver, view);
                    }
                }
                else
                {
                    NavigateToRecoveredMatch(view, sameMatch);
                }
            }
        }

        private void NavigateToRecoveredMatch(MatchView view, bool sameMatch)
        {
            bool isSpectator = false;
            foreach (MatchPlayerView player in view.Players ?? Array.Empty<MatchPlayerView>())
            {
                if (player != null && player.UserId == ServerConnection.PlayerUserId && player.IsEliminated)
                {
                    isSpectator = true;
                }
            }

            if (!isSpectator && view.FugitiveKitten?.Result == FugitiveKittenResult.InProgress)
            {
                if (!sameMatch || !(Navigation.CurrentView is FugitiveKittenScreen))
                {
                    Navigation.NavigateToWithoutHistory(ScreenId.FugitiveKitten, view.MatchId);
                }
            }
            else if (!sameMatch || !(Navigation.CurrentView is GameplayScreen))
            {
                Navigation.NavigateToWithoutHistory(ScreenId.Gameplay);
            }
        }

        private void OnChatErrorReceived(object sender, ServerEventArgs<string> eventArguments)
        {
            ShowServerError(eventArguments?.Payload);
        }

        private void ShowServerError(string errorKey)
        {
            if (string.IsNullOrWhiteSpace(errorKey))
            {
                _log.Warn("Missing error key in server notification.");
                errorKey = UnexpectedErrorKey;
            }

            MessageBox.Show(
                ErrorMessageHelper.FindErrorMessage(this, errorKey),
                TryFindResource(ErrorTitleKey) as string,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}