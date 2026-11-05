using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using log4net;

using TLMG.Client.Communication;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Views;

namespace TLMG.Client
{
    /// <summary>
    /// Ventana principal que contiene las pantallas y administra la navegación entre ellas.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConnectionLostMessageKey = "Common_ConnectionLostError";
        private const string RoomInvitationTitleKey = "Common_RoomInvitationTitle";
        private const string RoomInvitationMessageFormatKey = "Common_RoomInvitationMessageFormat";
        private const string ErrorTitleKey = "Common_ErrorTitle";
        private const int ZeroHistorySize = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

        private readonly HashSet<Guid> _enteredMatchIds = new HashSet<Guid>();
        private readonly Stack<UserControl> _navigationHistory;
        private UserControl _currentScreen;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainWindow"/> y prepara la navegación inicial.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ServerConnection.ConnectionLost += OnServerConnectionLost;
            ServerConnection.RoomInvitationReceived += OnRoomInvitationReceived;
            ServerConnection.AcceptInvitationResultReceived += OnAcceptInvitationResultReceived;
            ServerConnection.EnterLobbyResultReceived += OnEnterLobbyResultReceived;
            ServerConnection.LobbyRoomChanged += OnAuthorizedRoomChanged;
            Closed += OnMainWindowClosed;
            _navigationHistory = new Stack<UserControl>();
            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += (eventSender, eventArguments) => ShowScreen(new LoginScreen(), false);
            ShowScreen(loadingScreen, false);
        }

        private static ServerConnectionManager ServerConnection => ((App)Application.Current).ServerConnection;

        /// <summary>
        /// Muestra una nueva pantalla en el contenedor principal de la ventana, permitiendo
        /// registrarla en el historial de navegación.
        /// </summary>
        /// <param name="newScreen">Instancia del control de usuario que representa la pantalla a mostrar.</param>
        /// <param name="saveInHistory">
        /// Indica si la pantalla actual debe guardarse en la pila de historial para permitir retroceder.
        /// </param>
        public void ShowScreen(UserControl newScreen, bool saveInHistory = true)
        {
            if (saveInHistory && _currentScreen != null)
            {
                _navigationHistory.Push(_currentScreen);
            }

            _currentScreen = newScreen;
            grdMainContainer.Children.Clear();
            grdMainContainer.Children.Add(_currentScreen);
        }

        /// <summary>
        /// Navega a la pantalla anterior registrada en el historial o muestra el menú principal
        /// si el historial está vacío.
        /// </summary>
        public void GoBack()
        {
            if (_navigationHistory.Count > ZeroHistorySize)
            {
                _currentScreen = _navigationHistory.Pop();
                grdMainContainer.Children.Clear();
                grdMainContainer.Children.Add(_currentScreen);
            }
            else
            {
                ShowScreen(new MainMenuScreen(), false);
            }
        }

        /// <summary>
        /// Limpia todo el historial de pantallas navegadas.
        /// </summary>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
        }

        private void OnMainWindowClosed(object sender, EventArgs eventArguments)
        {
            ServerConnection.LobbyRoomChanged -= OnAuthorizedRoomChanged;
            ServerConnection.ConnectionLost -= OnServerConnectionLost;
            ServerConnection.RoomInvitationReceived -= OnRoomInvitationReceived;
            ServerConnection.AcceptInvitationResultReceived -= OnAcceptInvitationResultReceived;
            ServerConnection.EnterLobbyResultReceived -= OnEnterLobbyResultReceived;
        }

        private void OnServerConnectionLost(object sender, EventArgs eventArguments)
        {
            ClearHistory();
            ShowScreen(new LoginScreen(), false);
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
                    ShowScreen(new LobbyScreen(), false);
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
            try
            {
                Type gameplayType = typeof(MainWindow).Assembly.GetType("TLMG.Client.Views.GameplayScreen");

                if (gameplayType == null)
                {
                    ShowServerError("Lobby_GameplayUnavailableError");
                }
                else
                {
                    UserControl gameplayScreen = (UserControl)Activator.CreateInstance(gameplayType);
                    ClearHistory();
                    ShowScreen(gameplayScreen, false);
                }
            }
            catch (Exception exception)
            {
                _log.Error("No se pudo abrir la pantalla de partida autorizada.", exception);
                ShowServerError("Common_UnexpectedError");
            }
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