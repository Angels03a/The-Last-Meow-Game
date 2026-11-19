using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
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
        private readonly RelayCommand<HandCardEntry> _choosePunishmentCommand;
        private readonly RelayCommand<MatchPlayerEntry> _revealCardCommand;
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
        private Guid? _finishedMatchId;
        private MatchView _previousChallengeView;
        private GameNotification _notification;
        private MatchBoard _board;
        private bool _isMatchUnavailable;
        private readonly DispatcherTimer _recoveryDisplayTimer;
        private string _recoveryMessage = string.Empty;
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
            RetryRecoveryCommand = new RelayCommand(_matchConnection.RetryMatchRecovery);
            _recoveryDisplayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _recoveryDisplayTimer.Tick += UpdateRecoveryDisplay;

            _matchMode = MatchModeSettings.FromCardsPerPlayer(ReadCardsPerPlayer(lobbyConnection));
            _board = MatchBoardHelper.CreateEmptyBoard();
            _selectedPlayerName = string.Empty;
            _chatInput = string.Empty;
            _chatMessages = new ObservableCollection<ChatMessageEntry>();

            _sendChatCommand = new RelayCommand(ExecuteSendChat, CanExecuteSendChat);
            RetryChatCommand = new RelayCommand(_lobbyConnection.RetryChatConnection);
            _placeCardCommand = new RelayCommand<HandCardEntry>(ExecutePlaceCard, CanExecutePlaceCard);
            _choosePunishmentCommand = new RelayCommand<HandCardEntry>(ExecuteChoosePunishment, CanChoosePunishment);
            _revealCardCommand = new RelayCommand<MatchPlayerEntry>(ExecuteRevealCard, CanExecuteRevealCard);
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
        public ICommand ChoosePunishmentCommand => _choosePunishmentCommand;
        public ICommand RevealCardCommand => _revealCardCommand;
        public ICommand PlaceBetCommand => _placeBetCommand;
        public ICommand WithdrawBetCommand => _withdrawBetCommand;
        public ICommand IncreaseBetCommand => _increaseBetCommand;
        public ICommand DecreaseBetCommand => _decreaseBetCommand;
        public ICommand SendChatCommand => _sendChatCommand;
        public ICommand RetryChatCommand { get; }
        public bool IsChatUnavailable => _lobbyConnection.HasRegisteredSession
            && _lobbyConnection.CurrentRoom != null && !_lobbyConnection.IsChatSubscribed;
        public ICommand ExitCommand { get; }
        public ICommand ReportPlayerCommand { get; }
        public ICommand KickPlayerCommand { get; }
        public ICommand BlockPlayerCommand { get; }
        public ICommand DemoRevealCardsCommand { get; }
        public ICommand DemoGameOverCommand { get; }
        public ICommand DemoInactivityCommand { get; }

        public GameNotification Notification
        {
            get { return _notification; }
            private set { SetProperty(ref _notification, value); }
        }
        public int CurrentRoundNumber => _board.RoundNumber;
        public string PhaseLabelKey => _board.PhaseLabelKey;
        public bool HasMatchView => _board.HasMatchView;
        public ICommand RetryRecoveryCommand { get; }
        public bool CanRetryRecovery => _matchConnection.IsMatchRecoveryExhausted;
        public string RecoveryMessage
        {
            get => _recoveryMessage;
            private set => SetProperty(ref _recoveryMessage, value);
        }

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
            _matchConnection.RecoveryStateChanged += OnRecoveryStateChanged;
            _recoveryDisplayTimer.Start();
            UpdateRecoveryDisplay(this, EventArgs.Empty);
            _sendChatCommand.NotifyCanExecuteChanged();
            MatchView currentView = _matchConnection.CurrentMatchView;
            if (IsCurrentMatch(currentView))
            {
                _previousChallengeView = currentView;
                ShowBoardOf(currentView);
                if (ShowFinishedMatch(currentView))
                {
                    return;
                }
            }

            SubscribeToMatchIfNeeded();
        }

        public void Deactivate()
        {
            _lobbyConnection.ChatMessageReceived -= OnChatMessageReceived;
            _lobbyConnection.ChatAvailabilityChanged -= OnChatAvailabilityChanged;
            _matchConnection.MatchViewChanged -= OnMatchViewChanged;
            _matchConnection.MatchErrorReceived -= OnMatchErrorReceived;
            _matchConnection.MatchAvailabilityChanged -= OnMatchAvailabilityChanged;
            _matchConnection.RecoveryStateChanged -= OnRecoveryStateChanged;
            _recoveryDisplayTimer.Stop();
            _chatMessages.Clear();
            _previousChallengeView = null;
            Notification = null;
        }

        protected override void OnDisposed()
        {
            Deactivate();
            _recoveryDisplayTimer.Tick -= UpdateRecoveryDisplay;
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

        private void OnRecoveryStateChanged(object sender, EventArgs args)
        {
            UpdateRecoveryDisplay(sender, args);
            _placeCardCommand.NotifyCanExecuteChanged();
            _choosePunishmentCommand.NotifyCanExecuteChanged();
            _revealCardCommand.NotifyCanExecuteChanged();
            RefreshBetCommands();
        }

        private void UpdateRecoveryDisplay(object sender, EventArgs args)
        {
            RecoveryMessage = IsRealMatchAuthorized() ? MatchRecoveryText.Describe(_matchConnection) : string.Empty;
            OnPropertyChanged(nameof(CanRetryRecovery));
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
            _choosePunishmentCommand.NotifyCanExecuteChanged();
            _revealCardCommand.NotifyCanExecuteChanged();
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
            MatchView view = eventArgs.Payload;
            if (!IsCurrentMatch(view))
            {
                return;
            }

            MatchView previous = _previousChallengeView;
            if (previous != null && previous.MatchId == view.MatchId && view.Version <= previous.Version)
            {
                return;
            }

            if (view?.Challenge != null && previous?.Challenge != null
                && view.MatchId == previous.MatchId
                && previous.Challenge.Outcome == MatchChallengeOutcome.InProgress
                && (view.Challenge.Outcome == MatchChallengeOutcome.Succeeded
                    || view.Challenge.Outcome == MatchChallengeOutcome.Failed)
                && view.Challenge.BettorUserId == previous.Challenge.BettorUserId
                && view.Challenge.PromisedCount == previous.Challenge.PromisedCount
                && view.RoundNumber >= previous.RoundNumber && view.RoundNumber <= previous.RoundNumber + 1
                && (view.Challenge.RevealedCards?.Length ?? 0) > (previous.Challenge.RevealedCards?.Length ?? 0))
            {
                string bettor = view.Players?.FirstOrDefault(
                    player => player.UserId == view.Challenge.BettorUserId)?.Nickname ?? string.Empty;
                bool succeeded = view.Challenge.Outcome == MatchChallengeOutcome.Succeeded;
                Notification = GameNotificationFactory.CreatePublic(
                    succeeded ? "Gameplay_NoticeSuccessTitle" : "Gameplay_NoticeFailedTitle",
                    succeeded ? "Gameplay_NoticeSuccessBody" : "Gameplay_NoticeFailedBody",
                    bettor, succeeded ? "✓" : "!", succeeded);
            }
            else if (view?.Challenge?.Outcome == MatchChallengeOutcome.InProgress || view?.MatchId != previous?.MatchId)
            {
                Notification = null;
            }

            _previousChallengeView = view;
            ShowBoardOf(view);
            UpdateRecoveryDisplay(this, EventArgs.Empty);
            ShowFinishedMatch(view);
        }

        private bool IsCurrentMatch(MatchView view)
        {
            LobbyRoom room = _lobbyConnection.CurrentRoom;
            return view != null && room != null && room.MatchId.HasValue && view.MatchId == room.MatchId.Value;
        }

        private bool ShowFinishedMatch(MatchView view)
        {
            bool isFinished = view.Phase == MatchRoundPhase.MatchFinished && _finishedMatchId != view.MatchId;
            if (isFinished)
            {
                _finishedMatchId = view.MatchId;
                Notification = null;
                _navigation.NavigateToWithoutHistory(ScreenId.GameOver, view);
            }

            return isFinished;
        }

        private void OnMatchErrorReceived(object sender, ServerEventArgs<string> eventArgs)
        {
            if (!_matchConnection.IsMatchRecoveryPending && !_matchConnection.IsMatchRecoveryExhausted)
            {
                _dialogs.ShowError(eventArgs.Payload);
            }
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
                _previousChallengeView = _matchConnection.CurrentMatchView;
                ShowBoardOf(_previousChallengeView);
            }
            else
            {
                IsMatchUnavailable = true;
                _previousChallengeView = null;
                Notification = null;
                if (!IsCurrentMatch(_matchConnection.CurrentMatchView))
                {
                    Board = MatchBoardHelper.CreateEmptyBoard();
                    OnPropertyChanged(nameof(HasMatchView));
                }
            }

            _placeCardCommand.NotifyCanExecuteChanged();
            _choosePunishmentCommand.NotifyCanExecuteChanged();
            _revealCardCommand.NotifyCanExecuteChanged();
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
            return _matchConnection.CanSendGameplayAction && Board.CanPlaceBet
                && BidAmount >= Board.MinimumBetValue && BidAmount <= Board.MaximumBetValue;
        }

        private void ExecutePlaceBet()
        {
            _matchConnection.PlaceBet(BidAmount);
        }

        private bool CanExecuteWithdrawBet()
        {
            return _matchConnection.CanSendGameplayAction && Board.CanWithdrawFromBet;
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

        private bool CanExecuteRevealCard(MatchPlayerEntry player)
        {
            return player != null && player.PileCardCount > 0 && _matchConnection.CanSendGameplayAction && Board.CanRevealCard;
        }

        private void ExecuteRevealCard(MatchPlayerEntry player)
        {
            _matchConnection.RevealCard(player.UserId);
        }

        private bool CanChoosePunishment(HandCardEntry card)
        {
            return card != null && _matchConnection.CanSendGameplayAction && Board.CanChoosePunishment;
        }

        private void ExecuteChoosePunishment(HandCardEntry card)
        {
            _matchConnection.ChoosePunishmentCard(card.Index);
        }

        private void ExecutePlaceCard(HandCardEntry handCard)
        {
            _matchConnection.PlaceCard(MatchBoardHelper.ConvertToServerCard(handCard.Kind));
        }

        private bool CanExecutePlaceCard(HandCardEntry handCard)
        {
            bool canPlaceCard = handCard != null && _board.CanPlaceCards && _matchConnection.CanSendGameplayAction;

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
            OnPropertyChanged(nameof(IsChatUnavailable));
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