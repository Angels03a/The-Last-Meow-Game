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
    public class GameplayViewModel : ViewModelBase
    {
        private const string ChatEmptyErrorKey = "Lobby_ChatEmptyError";
        private const string NotInMatchErrorKey = "Match_NotInMatchError";

        private readonly RelayCommand _sendChatCommand;
        private readonly RelayCommand<HandCardEntry> _placeCardCommand;
        private readonly RelayCommand _placeBetCommand;
        private readonly RelayCommand _withdrawBetCommand;
        private readonly RelayCommand _increaseBetCommand;
        private readonly RelayCommand _decreaseBetCommand;
        private readonly INavigationManager _navigation;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly IMatchConnection _matchConnection;
        private readonly IDialogManager _dialogs;
        private readonly MatchModeSettings _matchMode;
        private readonly ObservableCollection<ChatMessageEntry> _chatMessages;

        private int _bidAmount;
        private MatchBoard _board;
        private bool _isMatchUnavailable;
        private string _selectedPlayerName;
        private string _chatInput;

        public GameplayViewModel(
            INavigationManager navigation,
            ILobbyConnection lobbyConnection,
            IMatchConnection matchConnection,
            IDialogManager dialogs)
        {
            _navigation = navigation;
            _lobbyConnection = lobbyConnection;
            _matchConnection = matchConnection;
            _dialogs = dialogs;

            _matchMode = MatchModeSettings.FromCardsPerPlayer(ReadCardsPerPlayer(lobbyConnection));
            _board = MatchBoardHelper.CreateEmptyBoard();
            _selectedPlayerName = string.Empty;
            _chatInput = string.Empty;
            _chatMessages = new ObservableCollection<ChatMessageEntry>();

            _sendChatCommand = new RelayCommand(ExecuteSendChat, CanExecuteSendChat);
            _placeCardCommand = new RelayCommand<HandCardEntry>(ExecutePlaceCard, CanExecutePlaceCard);
            _placeBetCommand = new RelayCommand(ExecutePlaceBet, CanExecutePlaceBet);
            _withdrawBetCommand = new RelayCommand(ExecuteWithdrawBet, CanExecuteWithdrawBet);
            _increaseBetCommand = new RelayCommand(ExecuteIncreaseBet, CanIncreaseBet);
            _decreaseBetCommand = new RelayCommand(ExecuteDecreaseBet, CanDecreaseBet);

            ExitCommand = new RelayCommand(ExecuteExit);
            ReportPlayerCommand = new RelayCommand(ExecuteReportPlayer);
            KickPlayerCommand = new RelayCommand(ExecuteKickPlayer);
            BlockPlayerCommand = new RelayCommand(ExecuteBlockPlayer);
            DemoRevealCardsCommand = new RelayCommand(ExecuteDemoRevealCards);
            DemoGameOverCommand = new RelayCommand(ExecuteDemoGameOver);
            DemoInactivityCommand = new RelayCommand(ExecuteDemoInactivity);
        }

        public event EventHandler KickVoteRequested;
        public ICommand PlaceCardCommand => _placeCardCommand;
        public ICommand PlaceBetCommand => _placeBetCommand;
        public ICommand WithdrawBetCommand => _withdrawBetCommand;
        public ICommand IncreaseBetCommand => _increaseBetCommand;
        public ICommand DecreaseBetCommand => _decreaseBetCommand;
        public ICommand SendChatCommand => _sendChatCommand;
        public ICommand ExitCommand { get; }
        public ICommand ReportPlayerCommand { get; }
        public ICommand KickPlayerCommand { get; }
        public ICommand BlockPlayerCommand { get; }
        public ICommand DemoRevealCardsCommand { get; }
        public ICommand DemoGameOverCommand { get; }
        public ICommand DemoInactivityCommand { get; }

        public int CurrentRoundNumber => _board.RoundNumber;
        public string PhaseLabelKey => _board.PhaseLabelKey;
        public bool HasMatchView => _board.HasMatchView;

        /// <summary>Obtiene si la partida dejó de estar disponible y el tablero ya no se muestra actualizado.</summary>
        public bool IsMatchUnavailable
        {
            get { return _isMatchUnavailable; }
            private set { SetProperty(ref _isMatchUnavailable, value); }
        }

        /// <summary>Obtiene si se muestran los controles de demostración; no se muestran en una partida real.</summary>
        public bool IsDemoPanelVisible
        {
            get
            {
                LobbyRoom currentRoom = _lobbyConnection.CurrentRoom;
                bool isRealMatch = currentRoom != null && currentRoom.MatchId.HasValue;

                return !isRealMatch;
            }
        }
        public bool IsCompactHand => _matchMode.IsCompactHand;
        public int HandColumnCount => _matchMode.HandColumnCount;
        public ObservableCollection<ChatMessageEntry> ChatMessages => _chatMessages;

        /// <summary>Obtiene lo que la pantalla dibuja de la última vista aceptada del servidor.</summary>
        public MatchBoard Board
        {
            get { return _board; }
            private set { SetProperty(ref _board, value); }
        }

        public int BidAmount
        {
            get { return _bidAmount; }
            private set
            {
                if (SetProperty(ref _bidAmount, value))
                {
                    RefreshBetCommands();
                }
            }
        }

        public int CurrentBidAmount => Board.CurrentBet;
        public string SelectedPlayerName
        {
            get { return _selectedPlayerName; }
            set
            {
                if (SetProperty(ref _selectedPlayerName, value))
                {
                    OnPropertyChanged(nameof(ContextMenuTitle));
                }
            }
        }

        public string ChatInput
        {
            get { return _chatInput; }
            set
            {
                if (SetProperty(ref _chatInput, value))
                {
                    _sendChatCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string ContextMenuTitle => SelectedPlayerName.ToUpperInvariant();

        public void Activate()
        {
            Deactivate();

            _lobbyConnection.ChatMessageReceived += OnChatMessageReceived;
            _lobbyConnection.ChatAvailabilityChanged += OnChatAvailabilityChanged;
            _matchConnection.MatchViewChanged += OnMatchViewChanged;
            _matchConnection.MatchErrorReceived += OnMatchErrorReceived;
            _matchConnection.MatchAvailabilityChanged += OnMatchAvailabilityChanged;
            _sendChatCommand.NotifyCanExecuteChanged();
            ShowBoardOf(_matchConnection.CurrentMatchView);
            SubscribeToMatchIfNeeded();
        }

        public void Deactivate()
        {
            _lobbyConnection.ChatMessageReceived -= OnChatMessageReceived;
            _lobbyConnection.ChatAvailabilityChanged -= OnChatAvailabilityChanged;
            _matchConnection.MatchViewChanged -= OnMatchViewChanged;
            _matchConnection.MatchErrorReceived -= OnMatchErrorReceived;
            _matchConnection.MatchAvailabilityChanged -= OnMatchAvailabilityChanged;
            _chatMessages.Clear();
        }

        protected override void OnDisposed()
        {
            Deactivate();
            base.OnDisposed();
        }

        private static int ReadCardsPerPlayer(ILobbyConnection lobbyConnection)
        {
            int cardsPerPlayer = MatchModeSettings.FourCardsPerPlayer;
            LobbyRoom currentRoom = lobbyConnection.CurrentRoom;

            if (currentRoom != null)
            {
                cardsPerPlayer = currentRoom.CardsPerPlayerCount;
            }

            return cardsPerPlayer;
        }

        private void SubscribeToMatchIfNeeded()
        {
            bool wasSubscriptionSent = _matchConnection.IsMatchSubscribed || _matchConnection.SubscribeToCurrentMatch();

            if (!wasSubscriptionSent && !IsRealMatchAuthorized())
            {
                _dialogs.ShowError(NotInMatchErrorKey);
            }
        }

        private void ShowBoardOf(MatchView matchView)
        {
            if (matchView != null)
            {
                int localUserId = _lobbyConnection.PlayerUserId;
                Board = MatchBoardHelper.CreateBoard(matchView, localUserId, _matchMode.CardsPerPlayer);
                OnPropertyChanged(nameof(CurrentRoundNumber));
                OnPropertyChanged(nameof(PhaseLabelKey));
                OnPropertyChanged(nameof(HasMatchView));
                OnPropertyChanged(nameof(CurrentBidAmount));
            }

            _placeCardCommand.NotifyCanExecuteChanged();
            if (Board.CanPlaceBet && Board.MinimumBetValue <= Board.MaximumBetValue)
            {
                BidAmount = Math.Max(Board.MinimumBetValue, Math.Min(BidAmount, Board.MaximumBetValue));
            }
            else
            {
                BidAmount = 0;
            }

            RefreshBetCommands();
        }

        private void OnMatchViewChanged(object sender, ServerEventArgs<MatchView> eventArgs)
        {
            ShowBoardOf(eventArgs.Payload);
        }

        private void OnMatchErrorReceived(object sender, ServerEventArgs<string> eventArgs)
        {
            _dialogs.ShowError(eventArgs.Payload);
        }

        private bool IsRealMatchAuthorized()
        {
            return !IsDemoPanelVisible;
        }

        /// <summary>
        /// Al perder la disponibilidad el tablero deja de presentarse como actualizado: se vacía y se avisa al
        /// jugador. Un fallo del canal de partida no lo saca de la sala ni de la sesión.
        /// </summary>
        private void OnMatchAvailabilityChanged(object sender, EventArgs eventArgs)
        {
            if (_matchConnection.IsMatchSubscribed)
            {
                IsMatchUnavailable = false;
            }
            else
            {
                IsMatchUnavailable = true;
                Board = MatchBoardHelper.CreateEmptyBoard();
                OnPropertyChanged(nameof(CurrentRoundNumber));
                OnPropertyChanged(nameof(PhaseLabelKey));
                OnPropertyChanged(nameof(HasMatchView));
                OnPropertyChanged(nameof(CurrentBidAmount));
            }

            _placeCardCommand.NotifyCanExecuteChanged();
            if (Board.CanPlaceBet && Board.MinimumBetValue <= Board.MaximumBetValue)
            {
                BidAmount = Math.Max(Board.MinimumBetValue, Math.Min(BidAmount, Board.MaximumBetValue));
            }
            else
            {
                BidAmount = 0;
            }

            RefreshBetCommands();
        }

        private void RefreshBetCommands()
        {
            _placeBetCommand.NotifyCanExecuteChanged();
            _withdrawBetCommand.NotifyCanExecuteChanged();
            _increaseBetCommand.NotifyCanExecuteChanged();
            _decreaseBetCommand.NotifyCanExecuteChanged();
        }

        private bool CanExecutePlaceBet()
        {
            return _matchConnection.IsMatchSubscribed && Board.CanPlaceBet
                && BidAmount >= Board.MinimumBetValue && BidAmount <= Board.MaximumBetValue;
        }

        private void ExecutePlaceBet()
        {
            _matchConnection.PlaceBet(BidAmount);
        }

        private bool CanExecuteWithdrawBet()
        {
            return _matchConnection.IsMatchSubscribed && Board.CanWithdrawFromBet;
        }

        private void ExecuteWithdrawBet()
        {
            _matchConnection.WithdrawFromBet();
        }

        private bool CanIncreaseBet()
        {
            return CanExecutePlaceBet() && BidAmount < Board.MaximumBetValue;
        }

        private void ExecuteIncreaseBet()
        {
            BidAmount++;
        }

        private bool CanDecreaseBet()
        {
            return CanExecutePlaceBet() && BidAmount > Board.MinimumBetValue;
        }

        private void ExecuteDecreaseBet()
        {
            BidAmount--;
        }

        private void ExecutePlaceCard(HandCardEntry handCard)
        {
            _matchConnection.PlaceCard(MatchBoardHelper.ConvertToServerCard(handCard.Kind));
        }

        private bool CanExecutePlaceCard(HandCardEntry handCard)
        {
            bool canPlaceCard = handCard != null && _board.CanPlaceCards && _matchConnection.IsMatchSubscribed;

            return canPlaceCard;
        }

        private void ExecuteSendChat()
        {
            string messageText = ChatInputHelper.TrimMessageText(ChatInput);
            bool canSendMessage = !ChatInputHelper.IsEmptyMessage(messageText)
                && ChatInputHelper.CanEnterText(messageText);

            if (canSendMessage)
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
            string messageText = ChatInputHelper.TrimMessageText(ChatInput);
            bool isEmptyMessage = ChatInputHelper.IsEmptyMessage(messageText);
            bool canSendMessage = !isEmptyMessage && _lobbyConnection.IsChatSubscribed;

            return canSendMessage;
        }

        private void OnChatAvailabilityChanged(object sender, EventArgs eventArgs)
        {
            _sendChatCommand.NotifyCanExecuteChanged();
        }

        private void OnChatMessageReceived(object sender, ServerEventArgs<ChatMessage> eventArgs)
        {
            ChatMessage chatMessage = eventArgs.Payload;
            ChatMessageEntry newEntry = ChatEntryHelper.CreateEntry(chatMessage, _lobbyConnection.PlayerUserId);

            _chatMessages.Add(newEntry);
        }

        private void ExecuteExit()
        {
            _lobbyConnection.LeaveLobby();
            _navigation.ClearHistory();
            _navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }

        private void ExecuteReportPlayer()
        {
            _navigation.NavigateToWithoutHistory(
                ScreenId.Moderation,
                Tuple.Create(SelectedPlayerName, ModerationMode.Report));
        }

        private void ExecuteKickPlayer()
        {
            KickVoteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteBlockPlayer()
        {
            _navigation.NavigateToWithoutHistory(
                ScreenId.Moderation,
                Tuple.Create(SelectedPlayerName, ModerationMode.Block));
        }

        private void ExecuteDemoRevealCards()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.RoundResult);
        }

        private void ExecuteDemoGameOver()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.GameOver);
        }

        private void ExecuteDemoInactivity()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.MatchClosed);
        }
    }
}