using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

        private readonly HashSet<Guid> _enteredMatchIds = new HashSet<Guid>();
        private Guid? _displayedMatchId;

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
            RoomInvitation roomInvitation = serverEventArguments.Payload;
            string invitationFormat = TryFindResource(RoomInvitationMessageFormatKey) as string ?? "{0}";
            string inviterName = DisplayNameHelper.Resolve(roomInvitation.InviterUserId, roomInvitation.InviterNickname);

            MessageBoxResult invitationAnswer = MessageBox.Show(
                string.Format(invitationFormat, inviterName),
                TryFindResource(RoomInvitationTitleKey) as string,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (invitationAnswer == MessageBoxResult.Yes)
            {
                ServerConnection.AcceptRoomInvitation(roomInvitation.InvitationId);
            }
        }

        private void OnAcceptInvitationResultReceived(
            object sender,
            ServerEventArgs<OperationAcceptRoomInvitationResult> serverEventArguments)
        {
            if (serverEventArguments.Payload.IsSuccess)
            {
                ServerConnection.EnterLobby(serverEventArguments.Payload.RoomId);
            }
            else
            {
                ShowServerError(serverEventArguments.Payload.ErrorKey);
            }
        }

        private void OnEnterLobbyResultReceived(
            object sender,
            ServerEventArgs<OperationLobbyRoomResult> serverEventArguments)
        {
            bool hasEnteredRoom = serverEventArguments.Payload.IsSuccess && ServerConnection.CurrentRoom != null;

            if (hasEnteredRoom)
            {
                if (!ServerConnection.CurrentRoom.InGame)
                {
                    ClearHistory();
                    Navigation.NavigateToWithoutHistory(ScreenId.Lobby);
                }
            }
            else if (!serverEventArguments.Payload.IsSuccess)
            {
                ShowServerError(serverEventArguments.Payload.ErrorKey);
            }
        }

        private void OnAuthorizedRoomChanged(object sender, ServerEventArgs<LobbyRoom> eventArguments)
        {
            LobbyRoom room = eventArguments.Payload;
            bool hasMatch = room != null && room.MatchId.HasValue;
            bool isAuthorized = hasMatch && room.InGame;
            bool isKnownMode = hasMatch && (room.CardsPerPlayerCount == MatchModeSettings.FourCardsPerPlayer
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
                Navigation.NavigateToWithoutHistory(
                    ScreenId.FugitiveKitten, room.MatchId.Value);
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

        private void OnMatchRecoveryReady(object sender, ServerEventArgs<MatchView> args)
        {
            MatchView view = args.Payload;
            if (view == null || ServerConnection.CurrentRoom?.MatchId != view.MatchId
                || !ServerConnection.IsMatchSubscribed)
            {
                return;
            }

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
                return;
            }

            bool isSpectator = (view.Players ?? Array.Empty<MatchPlayerView>()).Any(
                player => player.UserId == ServerConnection.PlayerUserId && player.IsEliminated);
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
            ShowServerError(eventArguments.Payload);
        }

        private void ShowServerError(string errorKey)
        {
            MessageBox.Show(
                ErrorMessageHelper.FindErrorMessage(this, errorKey),
                TryFindResource(ErrorTitleKey) as string,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}