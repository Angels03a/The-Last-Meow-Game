using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using log4net;
using TLMG.Client.Communication;
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

        public MainWindow(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            DataContext = mainWindowViewModel;
            ServerConnection.ConnectionLost += OnServerConnectionLost;
            ServerConnection.RoomInvitationReceived += OnRoomInvitationReceived;
            ServerConnection.AcceptInvitationResultReceived += OnAcceptInvitationResultReceived;
            ServerConnection.EnterLobbyResultReceived += OnEnterLobbyResultReceived;
            ServerConnection.LobbyRoomChanged += OnAuthorizedRoomChanged;
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
            ServerConnection.ConnectionLost -= OnServerConnectionLost;
            ServerConnection.RoomInvitationReceived -= OnRoomInvitationReceived;
            ServerConnection.AcceptInvitationResultReceived -= OnAcceptInvitationResultReceived;
            ServerConnection.EnterLobbyResultReceived -= OnEnterLobbyResultReceived;
        }

        private void OnLoadingCompleted(object sender, EventArgs eventArguments)
        {
            LoadingScreen loadingScreen = (LoadingScreen)sender;
            loadingScreen.LoadingCompleted -= OnLoadingCompleted;
            Navigation.NavigateToWithoutHistory(ScreenId.Login);
        }

        private void OnServerConnectionLost(object sender, EventArgs eventArguments)
        {
            Navigation.NavigateToWithoutHistory(ScreenId.Login);
            Navigation.ClearHistory();
            MessageBox.Show(TryFindResource(ConnectionLostMessageKey) as string);
        }

        private void OnRoomInvitationReceived(object sender, ServerEventArgs<RoomInvitation> serverEventArguments)
        {
            RoomInvitation roomInvitation = serverEventArguments.Payload;
            string invitationFormat = TryFindResource(RoomInvitationMessageFormatKey) as string ?? "{0}";

            MessageBoxResult invitationAnswer = MessageBox.Show(
                string.Format(invitationFormat, roomInvitation.InviterNickname),
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
            bool isKnownMode = hasMatch && (room.CardsPerPlayerCount == 4 || room.CardsPerPlayerCount == 6
                || room.CardsPerPlayerCount == 8);

            if (isAuthorized && isKnownMode && _enteredMatchIds.Add(room.MatchId.Value))
            {
                NavigateToAuthorizedMatch(room);
            }
        }

        private void NavigateToAuthorizedMatch(LobbyRoom room)
        {
            Navigation.ClearHistory();
            if (room.CardsPerPlayerCount == 8)
            {
                Navigation.NavigateToWithoutHistory(ScreenId.FugitiveKitten, room.MatchId.Value);
            }
            else if (room.CardsPerPlayerCount == 4 || room.CardsPerPlayerCount == 6)
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