using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public class LobbyViewModel : ViewModelBase
    {
        private const int MaximumPlayersPerRoom = 6;
        private const int NoRoomId = 0;
        private const int NoSecondsRemaining = 0;
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int NoSelectedIndex = -1;
        private const string RoomInformationFormatKey = "Lobby_RoomInformationFormat";
        private const string GuestCannotCreateRoomErrorKey = "Lobby_GuestCannotCreateRoomError";
        private const string LeaveConfirmMessageKey = "Lobby_LeaveConfirmMessage";
        private const string LeaveConfirmTitleKey = "Lobby_LeaveConfirmTitle";
        private const string ChatEmptyErrorKey = "Lobby_ChatEmptyError";
        private const string UnexpectedErrorKey = "Common_UnexpectedError";

        private static readonly int[] _cardsPerPlayerOptions =
        {
            FourCardsPerPlayer,
            SixCardsPerPlayer,
            EightCardsPerPlayer
        };

        private readonly ILobbyConnection _lobbyConnection;
        private readonly INavigationManager _navigationManager;
        private readonly IDialogManager _dialogManager;
        private readonly ObservableCollection<LobbyPlayerEntry> _players;
        private readonly ObservableCollection<ChatMessageEntry> _chatMessages;
        private readonly RelayCommand _toggleReadyCommand;
        private readonly RelayCommand _sendChatCommand;

        private bool _hasRoom;
        private int _roomId;
        private string _roomInformation = string.Empty;
        private bool _isLocalPlayerReady;
        private bool _isReadyButtonEnabled;
        private int _selectedCardsPerPlayerIndex = NoSelectedIndex;
        private bool _isApplyingRoom;
        private string _chatInput = string.Empty;
        private bool _isChatEnabled;
        private bool _isGuestSession;
        private bool _isCountdownVisible;
        private int _countdownSecondsRemaining;

        public LobbyViewModel(
            ILobbyConnection lobbyConnection,
            INavigationManager navigationManager,
            IDialogManager dialogManager)
        {
            _lobbyConnection = lobbyConnection;
            _navigationManager = navigationManager;
            _dialogManager = dialogManager;

            _players = new ObservableCollection<LobbyPlayerEntry>();
            _chatMessages = new ObservableCollection<ChatMessageEntry>();

            LeaveLobbyCommand = new RelayCommand(ExecuteLeaveLobby);
            InviteFriendsCommand = new RelayCommand(ExecuteInviteFriends);
            OpenOptionsCommand = new RelayCommand(ExecuteOpenOptions);
            _toggleReadyCommand = new RelayCommand(ExecuteToggleReady, CanExecuteToggleReady);
            CancelCountdownCommand = new RelayCommand(ExecuteCancelCountdown);
            _sendChatCommand = new RelayCommand(ExecuteSendChat, CanExecuteSendChat);
            RetryChatCommand = new RelayCommand(_lobbyConnection.RetryChatConnection);
        }

        public ICommand LeaveLobbyCommand { get; }

        public ICommand InviteFriendsCommand { get; }

        public ICommand OpenOptionsCommand { get; }

        public ICommand ToggleReadyCommand => _toggleReadyCommand;

        public ICommand CancelCountdownCommand { get; }

        public ICommand SendChatCommand => _sendChatCommand;

        public ICommand RetryChatCommand { get; }

        public bool IsChatUnavailable => _lobbyConnection.HasRegisteredSession
            && _lobbyConnection.CurrentRoom != null && !_lobbyConnection.IsChatSubscribed;

        public ObservableCollection<LobbyPlayerEntry> Players
        {
            get { return _players; }
        }

        public ObservableCollection<ChatMessageEntry> ChatMessages
        {
            get { return _chatMessages; }
        }

        public bool HasRoom
        {
            get { return _hasRoom; }
            private set { SetProperty(ref _hasRoom, value); }
        }

        public int RoomId
        {
            get { return _roomId; }
            private set { SetProperty(ref _roomId, value); }
        }

        public int PlayerCount
        {
            get { return _players.Count; }
        }

        public int MaximumPlayers
        {
            get { return MaximumPlayersPerRoom; }
        }

        public string RoomInformation
        {
            get { return _roomInformation; }
            private set { SetProperty(ref _roomInformation, value); }
        }

        public bool IsLocalPlayerReady
        {
            get { return _isLocalPlayerReady; }
            private set { SetProperty(ref _isLocalPlayerReady, value); }
        }

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

        public int SelectedCardsPerPlayerIndex
        {
            get { return _selectedCardsPerPlayerIndex; }
            set
            {
                bool wasChanged = SetProperty(ref _selectedCardsPerPlayerIndex, value);
                if (wasChanged && !_isApplyingRoom)
                {
                    RequestCardsPerPlayerChange();
                }
            }
        }

        public string ChatInput
        {
            get { return _chatInput; }
            set { SetProperty(ref _chatInput, value); }
        }

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

        public bool IsGuestSession
        {
            get { return _isGuestSession; }
            private set { SetProperty(ref _isGuestSession, value); }
        }

        public bool IsCountdownVisible
        {
            get { return _isCountdownVisible; }
            private set { SetProperty(ref _isCountdownVisible, value); }
        }

        public int CountdownSecondsRemaining
        {
            get { return _countdownSecondsRemaining; }
            private set
            {
                if (SetProperty(ref _countdownSecondsRemaining, value))
                {
                    OnPropertyChanged(nameof(HasCountdownSeconds));
                }
            }
        }

        public bool HasCountdownSeconds
        {
            get { return _countdownSecondsRemaining > NoSecondsRemaining; }
        }

        public void Activate()
        {
            Deactivate();

            _lobbyConnection.CreateRoomResultReceived += OnCreateRoomResultReceived;
            _lobbyConnection.LobbyRoomChanged += OnLobbyRoomChanged;
            _lobbyConnection.ReadinessResultReceived += OnReadinessResultReceived;
            _lobbyConnection.MatchStartResultReceived += OnMatchStartResultReceived;
            _lobbyConnection.ChangeCardsPerPlayerResultReceived += OnChangeCardsPerPlayerResultReceived;
            _lobbyConnection.ChatMessageReceived += OnChatMessageReceived;
            _lobbyConnection.ChatAvailabilityChanged += OnChatAvailabilityChanged;
            _lobbyConnection.CountdownChanged += OnCountdownChanged;
            LanguageManager.LanguageChanged += OnLanguageChanged;

            ApplyChatAvailability();
            ApplyCountdown();
            ShowOrCreateRoom();
        }

        public void Deactivate()
        {
            _lobbyConnection.CreateRoomResultReceived -= OnCreateRoomResultReceived;
            _lobbyConnection.LobbyRoomChanged -= OnLobbyRoomChanged;
            _lobbyConnection.ReadinessResultReceived -= OnReadinessResultReceived;
            _lobbyConnection.MatchStartResultReceived -= OnMatchStartResultReceived;
            _lobbyConnection.ChangeCardsPerPlayerResultReceived -= OnChangeCardsPerPlayerResultReceived;
            _lobbyConnection.ChatMessageReceived -= OnChatMessageReceived;
            _lobbyConnection.ChatAvailabilityChanged -= OnChatAvailabilityChanged;
            _lobbyConnection.CountdownChanged -= OnCountdownChanged;
            LanguageManager.LanguageChanged -= OnLanguageChanged;

            _chatMessages.Clear();
        }

        protected override void OnDisposed()
        {
            Deactivate();

            base.OnDisposed();
        }

        private void ExecuteLeaveLobby()
        {
            bool hasConfirmedLeave = _dialogManager.Confirm(LeaveConfirmMessageKey, LeaveConfirmTitleKey);

            if (hasConfirmedLeave)
            {
                _lobbyConnection.LeaveLobby();
                _navigationManager.ClearHistory();
                _navigationManager.NavigateToWithoutHistory(ScreenId.MainMenu);
            }
        }

        private void ExecuteInviteFriends()
        {
            _navigationManager.NavigateTo(ScreenId.Friends);
        }

        private void ExecuteOpenOptions()
        {
            _navigationManager.NavigateTo(ScreenId.Options);
        }

        private void ExecuteToggleReady()
        {
            _lobbyConnection.ChangePlayerReadiness(!IsLocalPlayerReady);
        }

        private bool CanExecuteToggleReady()
        {
            return IsReadyButtonEnabled;
        }

        private void ExecuteCancelCountdown()
        {
            _lobbyConnection.ChangePlayerReadiness(false);
        }

        private void ExecuteSendChat()
        {
            string messageText = ChatInputHelper.TrimMessageText(ChatInput);
            bool isEmptyMessage = ChatInputHelper.IsEmptyMessage(messageText);
            bool canSendMessage = !isEmptyMessage && ChatInputHelper.CanEnterText(messageText);

            if (isEmptyMessage)
            {
                _dialogManager.ShowError(ChatEmptyErrorKey);
            }
            else if (canSendMessage)
            {
                bool wasMessageSent = _lobbyConnection.SendChatMessage(messageText);

                if (wasMessageSent)
                {
                    ChatInput = string.Empty;
                }
            }
        }

        private bool CanExecuteSendChat()
        {
            return IsChatEnabled;
        }

        private void RequestCardsPerPlayerChange()
        {
            bool isValidIndex = SelectedCardsPerPlayerIndex >= 0
                && SelectedCardsPerPlayerIndex < _cardsPerPlayerOptions.Length;
            LobbyRoom currentRoom = _lobbyConnection.CurrentRoom;

            if (isValidIndex && currentRoom != null)
            {
                int requestedCardsPerPlayer = _cardsPerPlayerOptions[SelectedCardsPerPlayerIndex];

                if (requestedCardsPerPlayer != currentRoom.CardsPerPlayerCount)
                {
                    bool wasSent = _lobbyConnection.ChangeCardsPerPlayer(requestedCardsPerPlayer);

                    if (!wasSent)
                    {
                        _dialogManager.ShowError(UnexpectedErrorKey);
                    }
                }
            }
        }

        private void ShowOrCreateRoom()
        {
            ShowCurrentRoom();
            bool needsRoom = _lobbyConnection.CurrentRoom == null && !_lobbyConnection.IsRoomCreationPending;

            if (needsRoom && !_lobbyConnection.HasRegisteredSession)
            {
                _dialogManager.ShowError(GuestCannotCreateRoomErrorKey);
                _navigationManager.GoBack();
            }
            else if (needsRoom)
            {
                _lobbyConnection.CreateRoom();
            }
        }

        private void OnCreateRoomResultReceived(object sender, ServerEventArgs<OperationLobbyRoomResult> eventArgs)
        {
            if (eventArgs.Payload.IsSuccess)
            {
                ShowCurrentRoom();
            }
            else
            {
                _dialogManager.ShowError(eventArgs.Payload.ErrorKey);
            }
        }

        private void OnLobbyRoomChanged(object sender, ServerEventArgs<LobbyRoom> eventArgs)
        {
            ShowCurrentRoom();
        }

        private void OnReadinessResultReceived(
            object sender,
            ServerEventArgs<OperationLobbyRoomResult> eventArgs)
        {
            if (!eventArgs.Payload.IsSuccess)
            {
                _dialogManager.ShowError(eventArgs.Payload.ErrorKey);
            }
        }

        private void OnMatchStartResultReceived(
            object sender,
            ServerEventArgs<OperationRequestMatchStartResult> eventArgs)
        {
            if (!eventArgs.Payload.IsSuccess)
            {
                _dialogManager.ShowError(eventArgs.Payload.ErrorKey);
            }
        }

        private void OnChangeCardsPerPlayerResultReceived(
            object sender,
            ServerEventArgs<OperationLobbyRoomResult> eventArgs)
        {
            if (!eventArgs.Payload.IsSuccess)
            {
                ShowCurrentRoom();
                _dialogManager.ShowError(eventArgs.Payload.ErrorKey);
            }
        }

        private void OnChatMessageReceived(object sender, ServerEventArgs<ChatMessage> eventArgs)
        {
            ChatMessage chatMessage = eventArgs.Payload;
            ChatMessageEntry newEntry = ChatEntryHelper.CreateEntry(chatMessage, _lobbyConnection.PlayerUserId);

            _chatMessages.Add(newEntry);
        }

        private void OnChatAvailabilityChanged(object sender, EventArgs eventArgs)
        {
            OnPropertyChanged(nameof(IsChatUnavailable));
            ApplyChatAvailability();
        }

        private void OnCountdownChanged(object sender, EventArgs eventArgs)
        {
            ApplyCountdown();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            ShowCurrentRoom();
        }

        private void ApplyChatAvailability()
        {
            IsChatEnabled = _lobbyConnection.IsChatSubscribed;
            IsGuestSession = !_lobbyConnection.HasRegisteredSession;
        }

        private void ApplyCountdown()
        {
            IsCountdownVisible = _lobbyConnection.IsCountdownVisible;
            CountdownSecondsRemaining = _lobbyConnection.CountdownSecondsRemaining;
        }

        private void ShowCurrentRoom()
        {
            LobbyRoom currentRoom = _lobbyConnection.CurrentRoom;

            if (currentRoom != null)
            {
                ShowRoom(currentRoom);
            }
            else
            {
                ClearRoom();
            }

            UpdateRoomInformation();
        }

        private void ShowRoom(LobbyRoom lobbyRoom)
        {
            bool hasLocalPlayer = false;
            bool isLocalReady = false;

            _players.Clear();

            foreach (LobbyPlayerEntry playerEntry in BuildPlayerEntries(lobbyRoom))
            {
                _players.Add(playerEntry);

                if (playerEntry.IsLocalPlayer)
                {
                    hasLocalPlayer = true;
                    isLocalReady = playerEntry.IsReady;
                }
            }

            RoomId = lobbyRoom.RoomId;
            HasRoom = true;
            IsLocalPlayerReady = isLocalReady;
            IsReadyButtonEnabled = hasLocalPlayer && !lobbyRoom.InGame;
            ApplyCardsPerPlayer(Array.IndexOf(_cardsPerPlayerOptions, lobbyRoom.CardsPerPlayerCount));
        }

        private void ClearRoom()
        {
            _players.Clear();
            RoomId = NoRoomId;
            HasRoom = false;
            IsLocalPlayerReady = false;
            IsReadyButtonEnabled = false;
            ApplyCardsPerPlayer(NoSelectedIndex);
        }

        private void ApplyCardsPerPlayer(int cardsPerPlayerIndex)
        {
            _isApplyingRoom = true;
            SelectedCardsPerPlayerIndex = cardsPerPlayerIndex;
            _isApplyingRoom = false;
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
                        Nickname = DisplayNameHelper.Resolve(roomPlayer.UserId, roomPlayer.Nickname),
                        IsLocalPlayer = roomPlayer.UserId == _lobbyConnection.PlayerUserId,
                        IsReady = roomPlayer.IsReady
                    });
                }
            }

            return playerEntries;
        }

        private void UpdateRoomInformation()
        {
            string roomInformation = string.Empty;

            if (HasRoom)
            {
                string roomInformationFormat = LanguageManager.FindText(RoomInformationFormatKey);
                roomInformation = string.Format(roomInformationFormat, RoomId, PlayerCount, MaximumPlayers);
            }

            RoomInformation = roomInformation;
        }
    }
}