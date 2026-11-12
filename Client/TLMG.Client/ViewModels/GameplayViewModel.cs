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
        private const int CurrentRoundNumberValue = 3;
        private const int InitialBidAmount = 4;
        private const int MinimumBidAmount = 1;
        private const int BidStep = 1;
        private const int DemoFaceDownCardsOnTable = 6;
        private const string PhaseBiddingKey = "Gameplay_PhaseBiddingLabel";
        private const string ChatEmptyErrorKey = "Lobby_ChatEmptyError";

        private readonly RelayCommand _decreaseBidCommand;
        private readonly RelayCommand _increaseBidCommand;
        private readonly RelayCommand _sendChatCommand;
        private readonly INavigationManager _navigation;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly MatchModeSettings _matchMode;
        private readonly int _maximumBidAmount;
        private readonly List<HandCardEntry> _handCards;
        private readonly ObservableCollection<ChatMessageEntry> _chatMessages;

        private int _bidAmount;
        private int _currentBidAmount;
        private string _selectedPlayerName;
        private string _chatInput;

        public GameplayViewModel(INavigationManager navigation, ILobbyConnection lobbyConnection)
        {
            _navigation = navigation;
            _lobbyConnection = lobbyConnection;

            _matchMode = MatchModeSettings.FromCardsPerPlayer(ReadCardsPerPlayer(lobbyConnection));
            _maximumBidAmount = CalculateMaximumBid(DemoFaceDownCardsOnTable, _matchMode.CardsPerPlayer);
            _handCards = BuildHandCards(_matchMode);
            _bidAmount = Math.Min(InitialBidAmount, _maximumBidAmount);
            _currentBidAmount = InitialBidAmount;
            _selectedPlayerName = string.Empty;
            _chatInput = string.Empty;
            _chatMessages = new ObservableCollection<ChatMessageEntry>();

            _decreaseBidCommand = new RelayCommand(ExecuteDecreaseBid, CanExecuteDecreaseBid);
            _increaseBidCommand = new RelayCommand(ExecuteIncreaseBid, CanExecuteIncreaseBid);
            _sendChatCommand = new RelayCommand(ExecuteSendChat, CanExecuteSendChat);

            ExitCommand = new RelayCommand(ExecuteExit);
            ReportPlayerCommand = new RelayCommand(ExecuteReportPlayer);
            KickPlayerCommand = new RelayCommand(ExecuteKickPlayer);
            BlockPlayerCommand = new RelayCommand(ExecuteBlockPlayer);
            DemoRevealCardsCommand = new RelayCommand(ExecuteDemoRevealCards);
            DemoGameOverCommand = new RelayCommand(ExecuteDemoGameOver);
            DemoInactivityCommand = new RelayCommand(ExecuteDemoInactivity);
        }

        public event EventHandler KickVoteRequested;
        public ICommand DecreaseBidCommand => _decreaseBidCommand;
        public ICommand IncreaseBidCommand => _increaseBidCommand;
        public ICommand SendChatCommand => _sendChatCommand;
        public ICommand ExitCommand { get; }
        public ICommand ReportPlayerCommand { get; }
        public ICommand KickPlayerCommand { get; }
        public ICommand BlockPlayerCommand { get; }
        public ICommand DemoRevealCardsCommand { get; }
        public ICommand DemoGameOverCommand { get; }
        public ICommand DemoInactivityCommand { get; }

        public int CurrentRoundNumber => CurrentRoundNumberValue;
        public string PhaseLabelKey => PhaseBiddingKey;
        public int ChallengesToWin => _matchMode.ChallengesToWin;
        public bool IsCompactHand => _matchMode.IsCompactHand;
        public int HandColumnCount => _matchMode.HandColumnCount;
        public IReadOnlyList<HandCardEntry> HandCards => _handCards;
        public ObservableCollection<ChatMessageEntry> ChatMessages => _chatMessages;

        public int BidAmount
        {
            get { return _bidAmount; }
            private set
            {
                if (SetProperty(ref _bidAmount, value))
                {
                    _decreaseBidCommand.NotifyCanExecuteChanged();
                    _increaseBidCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public int CurrentBidAmount
        {
            get { return _currentBidAmount; }
            private set { SetProperty(ref _currentBidAmount, value); }
        }

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
            _lobbyConnection.ChatMessageReceived += OnChatMessageReceived;
        }

        public void Deactivate()
        {
            _lobbyConnection.ChatMessageReceived -= OnChatMessageReceived;
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

        private static int CalculateMaximumBid(int faceDownCardsOnTable, int cardsPerPlayer)
        {
            return Math.Min(faceDownCardsOnTable, cardsPerPlayer);
        }

        private static List<HandCardEntry> BuildHandCards(MatchModeSettings matchMode)
        {
            List<HandCardEntry> handCards = new List<HandCardEntry>();
            bool isNextSafeCardEgyptian = true;

            for (int safeCardNumber = 0; safeCardNumber < matchMode.SafeCardCount; safeCardNumber++)
            {
                HandCardKind safeCardKind = isNextSafeCardEgyptian ? HandCardKind.Egyptian : HandCardKind.Zombie;
                handCards.Add(new HandCardEntry { Kind = safeCardKind });
                isNextSafeCardEgyptian = !isNextSafeCardEgyptian;
            }

            for (int fatalCardNumber = 0; fatalCardNumber < matchMode.FatalCatCount; fatalCardNumber++)
            {
                handCards.Add(new HandCardEntry { Kind = HandCardKind.Skull });
            }

            return handCards;
        }

        private void ExecuteDecreaseBid()
        {
            BidAmount -= BidStep;
        }

        private bool CanExecuteDecreaseBid()
        {
            return BidAmount > MinimumBidAmount;
        }

        private void ExecuteIncreaseBid()
        {
            BidAmount += BidStep;
        }

        private bool CanExecuteIncreaseBid()
        {
            return BidAmount < _maximumBidAmount;
        }

        private void ExecuteSendChat()
        {
            string messageText = (ChatInput ?? string.Empty).Trim();
            bool isEmptyMessage = string.IsNullOrEmpty(messageText);

            if (!isEmptyMessage)
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
            string messageText = (ChatInput ?? string.Empty).Trim();
            bool isEmptyMessage = string.IsNullOrEmpty(messageText);

            return !isEmptyMessage;
        }

        private void OnChatMessageReceived(object sender, ServerEventArgs<ChatMessage> eventArgs)
        {
            ChatMessage chatMessage = eventArgs.Payload;
            ChatMessageEntry newEntry = new ChatMessageEntry
            {
                SenderNickname = chatMessage.SenderNickname,
                Text = chatMessage.Text,
                IsLocalSender = chatMessage.SenderUserId == _lobbyConnection.PlayerUserId
            };

            _chatMessages.Add(newEntry);
        }

        private void ExecuteExit()
        {
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