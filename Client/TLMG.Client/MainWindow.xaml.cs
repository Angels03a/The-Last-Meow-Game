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
    /// <summary>
    /// Ventana principal que muestra la pantalla actual y atiende los avisos globales del servidor.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConnectionLostMessageKey = "Common_ConnectionLostError";
        private const string RoomInvitationTitleKey = "Common_RoomInvitationTitle";
        private const string RoomInvitationMessageFormatKey = "Common_RoomInvitationMessageFormat";
        private const string ErrorTitleKey = "Common_ErrorTitle";

        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

        private readonly HashSet<Guid> _enteredMatchIds = new HashSet<Guid>();

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainWindow"/> y muestra la pantalla de carga.
        /// </summary>
        /// <param name="mainWindowViewModel">ViewModel que expone la pantalla que se está mostrando.</param>
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

        /// <summary>
        /// Muestra una pantalla que aún no tiene ViewModel. Puente temporal hacia NavigationManager, que lleva el
        /// único historial de pantallas.
        /// </summary>
        /// <param name="newScreen">Instancia del control de usuario que representa la pantalla a mostrar.</param>
        /// <param name="saveInHistory">
        /// Indica si la pantalla actual debe guardarse en el historial para permitir retroceder.
        /// </param>
        public void ShowScreen(UserControl newScreen, bool saveInHistory = true)
        {
            Navigation.ShowExistingScreen(newScreen, saveInHistory);
        }

        /// <summary>
        /// Regresa a la pantalla anterior del historial o muestra el menú principal si el historial está vacío.
        /// Puente temporal hacia NavigationManager.
        /// </summary>
        public void GoBack()
        {
            Navigation.GoBack();
        }

        /// <summary>
        /// Limpia el historial de pantallas. Puente temporal hacia NavigationManager.
        /// </summary>
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
            ServerEventArgs<OperationEnterLobbyResult> serverEventArguments)
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

            if (isAuthorized && _enteredMatchIds.Add(room.MatchId.Value))
            {
                NavigateToAuthorizedMatch();
            }
        }

        // BL-046: punto único de integración; GameplayScreen debe estar incluida y compilada en el proyecto.
        private void NavigateToAuthorizedMatch()
        {
            Navigation.ClearHistory();
            Navigation.NavigateToWithoutHistory(ScreenId.Gameplay);
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