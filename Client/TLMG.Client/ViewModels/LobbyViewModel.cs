using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Models;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado, lista de jugadores, chat y comandos de la pantalla de sala (lobby).
    /// </summary>
    public class LobbyViewModel : ViewModelBase
    {
        private const int MaximumPlayersPerRoom = 6;
        private const int NoRoomId = 0;
        private const string ChatEmptyErrorKey = "Lobby_ChatEmptyError";

        private readonly ObservableCollection<LobbyPlayerEntry> _players;
        private readonly ObservableCollection<ChatMessageEntry> _chatMessages;
        private readonly RelayCommand _toggleReadyCommand;
        private readonly RelayCommand _sendChatCommand;
        private bool _hasRoom;
        private int _roomId;
        private bool _isLocalPlayerReady;
        private bool _isReadyButtonEnabled;
        private string _chatInput;
        private string _pendingChatText;
        private bool _isChatEnabled;
        private bool _isGuestSession;
        private string _lastErrorKey;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LobbyViewModel"/>.
        /// </summary>
        public LobbyViewModel()
        {
            _players = new ObservableCollection<LobbyPlayerEntry>();
            _chatMessages = new ObservableCollection<ChatMessageEntry>();
            _chatInput = string.Empty;
            _pendingChatText = string.Empty;

            LeaveLobbyCommand = new RelayCommand(ExecuteLeaveLobby);
            InviteFriendsCommand = new RelayCommand(ExecuteInviteFriends);
            _toggleReadyCommand = new RelayCommand(ExecuteToggleReady, CanExecuteToggleReady);
            CancelCountdownCommand = new RelayCommand(ExecuteCancelCountdown);
            _sendChatCommand = new RelayCommand(ExecuteSendChat, CanExecuteSendChat);
        }

        /// <summary>
        /// Se produce cuando el jugador solicita salir de la sala.
        /// </summary>
        public event EventHandler LeaveRequested;

        /// <summary>
        /// Se produce cuando el jugador solicita invitar amigos.
        /// </summary>
        public event EventHandler InviteFriendsRequested;

        /// <summary>
        /// Se produce cuando el jugador solicita cambiar su estado Listo.
        /// </summary>
        public event EventHandler ReadinessChangeRequested;

        /// <summary>
        /// Se produce cuando el jugador solicita cancelar la cuenta regresiva.
        /// </summary>
        public event EventHandler CancelCountdownRequested;

        /// <summary>
        /// Se produce cuando hay un mensaje de chat válido que la vista debe enviar al servidor.
        /// </summary>
        public event EventHandler ChatSendRequested;

        /// <summary>
        /// Se produce cuando hay una clave de error que la vista debe mostrar.
        /// </summary>
        public event EventHandler ErrorRequested;

        /// <summary>Comando que solicita salir de la sala.</summary>
        public ICommand LeaveLobbyCommand { get; }

        /// <summary>Comando que solicita invitar amigos.</summary>
        public ICommand InviteFriendsCommand { get; }

        /// <summary>Comando que solicita cambiar el estado Listo del jugador local.</summary>
        public ICommand ToggleReadyCommand => _toggleReadyCommand;

        /// <summary>Comando que solicita cancelar la cuenta regresiva de inicio.</summary>
        public ICommand CancelCountdownCommand { get; }

        /// <summary>Comando que valida y solicita enviar el mensaje de chat.</summary>
        public ICommand SendChatCommand => _sendChatCommand;

        /// <summary>
        /// Jugadores de la sala.
        /// </summary>
        public ObservableCollection<LobbyPlayerEntry> Players
        {
            get { return _players; }
        }

        /// <summary>
        /// Mensajes de chat recibidos.
        /// </summary>
        public ObservableCollection<ChatMessageEntry> ChatMessages
        {
            get { return _chatMessages; }
        }

        /// <summary>
        /// Indica si el jugador está en una sala.
        /// </summary>
        public bool HasRoom
        {
            get { return _hasRoom; }
            private set { SetProperty(ref _hasRoom, value); }
        }

        /// <summary>
        /// Identificador de la sala actual.
        /// </summary>
        public int RoomId
        {
            get { return _roomId; }
            private set { SetProperty(ref _roomId, value); }
        }

        /// <summary>
        /// Número de jugadores en la sala.
        /// </summary>
        public int PlayerCount
        {
            get { return _players.Count; }
        }

        /// <summary>
        /// Máximo de jugadores por sala.
        /// </summary>
        public int MaximumPlayers
        {
            get { return MaximumPlayersPerRoom; }
        }

        /// <summary>
        /// Indica si el jugador local está en estado Listo.
        /// </summary>
        public bool IsLocalPlayerReady
        {
            get { return _isLocalPlayerReady; }
            private set { SetProperty(ref _isLocalPlayerReady, value); }
        }

        /// <summary>
        /// Indica si el jugador local puede cambiar su estado Listo.
        /// </summary>
        public bool IsReadyButtonEnabled
        {
            get { return _isReadyButtonEnabled; }
            private set
            {
                if (SetProperty(ref _isReadyButtonEnabled, value))
                {
                    _toggleReadyCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Texto escrito en la caja del chat.
        /// </summary>
        public string ChatInput
        {
            get { return _chatInput; }
            set { SetProperty(ref _chatInput, value); }
        }

        /// <summary>
        /// Último mensaje validado, listo para enviarse.
        /// </summary>
        public string PendingChatText
        {
            get { return _pendingChatText; }
            private set { SetProperty(ref _pendingChatText, value); }
        }

        /// <summary>
        /// Indica si el chat acepta mensajes.
        /// </summary>
        public bool IsChatEnabled
        {
            get { return _isChatEnabled; }
            private set
            {
                if (SetProperty(ref _isChatEnabled, value))
                {
                    _sendChatCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Indica si la sesión es de invitado, que no puede usar el chat.
        /// </summary>
        public bool IsGuestSession
        {
            get { return _isGuestSession; }
            private set { SetProperty(ref _isGuestSession, value); }
        }

        /// <summary>
        /// Última clave de error que la vista debe mostrar.
        /// </summary>
        public string LastErrorKey
        {
            get { return _lastErrorKey; }
            private set { SetProperty(ref _lastErrorKey, value); }
        }

        /// <summary>
        /// Muestra la sala recibida del servidor ya traducida a entradas de jugador.
        /// </summary>
        /// <param name="roomId">Identificador de la sala.</param>
        /// <param name="isWaitingRoom">Indica si la sala está esperando y aún no está en partida.</param>
        /// <param name="playerEntries">Jugadores de la sala.</param>
        public void ShowRoom(int roomId, bool isWaitingRoom, IEnumerable<LobbyPlayerEntry> playerEntries)
        {
            bool hasLocalPlayer = false;
            bool isLocalReady = false;

            _players.Clear();

            foreach (LobbyPlayerEntry playerEntry in playerEntries)
            {
                _players.Add(playerEntry);

                if (playerEntry.IsLocalPlayer)
                {
                    hasLocalPlayer = true;
                    isLocalReady = playerEntry.IsReady;
                }
            }

            RoomId = roomId;
            HasRoom = true;
            IsLocalPlayerReady = isLocalReady;
            IsReadyButtonEnabled = hasLocalPlayer && isWaitingRoom;
        }

        /// <summary>
        /// Limpia la sala cuando el jugador no está en ninguna.
        /// </summary>
        public void ClearRoom()
        {
            _players.Clear();
            RoomId = NoRoomId;
            HasRoom = false;
            IsLocalPlayerReady = false;
            IsReadyButtonEnabled = false;
        }

        /// <summary>
        /// Agrega un mensaje recibido a la lista del chat.
        /// </summary>
        /// <param name="chatMessageEntry">Mensaje a mostrar.</param>
        public void AddChatMessage(ChatMessageEntry chatMessageEntry)
        {
            _chatMessages.Add(chatMessageEntry);
        }

        /// <summary>
        /// Vacía la lista de mensajes del chat.
        /// </summary>
        public void ClearChatMessages()
        {
            _chatMessages.Clear();
        }

        /// <summary>
        /// Vacía la caja de escritura del chat.
        /// </summary>
        public void ClearChatInput()
        {
            ChatInput = string.Empty;
        }

        /// <summary>
        /// Actualiza si el chat está disponible y si la sesión es de invitado.
        /// </summary>
        /// <param name="isChatSubscribed">Indica si el jugador está suscrito al chat.</param>
        /// <param name="hasRegisteredSession">Indica si la sesión es de un jugador registrado.</param>
        public void ApplyChatAvailability(bool isChatSubscribed, bool hasRegisteredSession)
        {
            IsChatEnabled = isChatSubscribed;
            IsGuestSession = !hasRegisteredSession;
        }

        private void ExecuteLeaveLobby()
        {
            LeaveRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteInviteFriends()
        {
            InviteFriendsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteToggleReady()
        {
            ReadinessChangeRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteToggleReady()
        {
            return IsReadyButtonEnabled;
        }

        private void ExecuteCancelCountdown()
        {
            CancelCountdownRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteSendChat()
        {
            string messageText = (ChatInput ?? string.Empty).Trim();
            bool isEmptyMessage = string.IsNullOrEmpty(messageText);
            bool canSendMessage = !isEmptyMessage && ChatInputHelper.CanEnterText(messageText);

            if (isEmptyMessage)
            {
                LastErrorKey = ChatEmptyErrorKey;
                ErrorRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (canSendMessage)
            {
                PendingChatText = messageText;
                ChatSendRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanExecuteSendChat()
        {
            return IsChatEnabled;
        }
    }
}