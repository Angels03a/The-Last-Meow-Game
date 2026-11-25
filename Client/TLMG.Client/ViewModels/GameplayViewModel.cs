using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.Presentation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public class GameplayViewModel : ViewModelBase
    {
        private const string NotInMatchErrorKey = "Match_NotInMatchError";
        private const string ChallengeSuccessTitleKey = "Gameplay_NoticeSuccessTitle";
        private const string ChallengeSuccessBodyKey = "Gameplay_NoticeSuccessBody";
        private const string ChallengeFailureTitleKey = "Gameplay_NoticeFailedTitle";
        private const string ChallengeFailureBodyKey = "Gameplay_NoticeFailedBody";
        private const string ChallengeSuccessIcon = "✓";
        private const string ChallengeFailureIcon = "!";
        private const int NextRoundOffset = 1;
        private const int NoRevealedCards = 0;
        private const int NoBetAmount = 0;
        private const int EmptyPileCardCount = 0;
        private const int RecoveryDisplayIntervalMilliseconds = 250;

        private readonly RelayCommand _sendChatCommand;
        private readonly RelayCommand<HandCardEntry> _placeCardCommand;
        private readonly RelayCommand<HandCardEntry> _choosePunishmentCommand;
        private readonly RelayCommand<MatchPlayerEntry> _revealCardCommand;
        private readonly RelayCommand _placeBetCommand;
        private readonly RelayCommand _withdrawBetCommand;
        private readonly RelayCommand _increaseBetCommand;
        private readonly RelayCommand _decreaseBetCommand;
        private readonly INavigationManager _navigationManager;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly IMatchConnection _matchConnection;
        private readonly IDialogManager _dialogManager;
        private readonly MatchModeSettings _matchModeSettings;
        private readonly ObservableCollection<ChatMessageEntry> _chatMessages;

        private int _bidAmount;
        private Guid? _finishedMatchId;
        private MatchView _previousMatchView;
        private GameNotification _notification;
        private MatchBoard _board;
        private bool _isMatchUnavailable;
        private readonly DispatcherTimer _recoveryDisplayTimer;
        private string _recoveryMessage = string.Empty;
        private string _selectedPlayerName;
        private string _chatInput;

        public GameplayViewModel(
            INavigationManager navigationManager,
            ILobbyConnection lobbyConnection,
            IMatchConnection matchConnection,
            IDialogManager dialogManager)
        {
            _navigationManager = navigationManager;
            _lobbyConnection = lobbyConnection;
            _matchConnection = matchConnection;
            _dialogManager = dialogManager;
            RetryRecoveryCommand = new RelayCommand(_matchConnection.RetryMatchRecovery);
            _recoveryDisplayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(RecoveryDisplayIntervalMilliseconds) };
            _recoveryDisplayTimer.Tick += UpdateRecoveryDisplay;

            int cardsPerPlayer = lobbyConnection.CurrentRoom?.CardsPerPlayerCount
                ?? MatchModeSettings.FourCardsPerPlayer;
            _matchModeSettings = MatchModeSettings.FromCardsPerPlayer(cardsPerPlayer);
            _board = MatchBoardMapper.CreateEmptyBoard();
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

        /// <summary>Indicates that the board may be out of date while the match channel is unavailable.</summary>
        public bool IsMatchUnavailable
        {
            get { return _isMatchUnavailable; }
            private set { SetProperty(ref _isMatchUnavailable, value); }
        }

        /// <summary>Keeps demo controls hidden during an authorized match.</summary>
        public bool IsDemoPanelVisible
        {
            get
            {
                LobbyRoom currentRoom = _lobbyConnection.CurrentRoom;
                bool isDemoPanelVisible = currentRoom == null || !currentRoom.MatchId.HasValue;

                return isDemoPanelVisible;
            }
        }
        public bool IsCompactHand => _matchModeSettings.IsCompactHand;
        public int HandColumnCount => _matchModeSettings.HandColumnCount;
        public ObservableCollection<ChatMessageEntry> ChatMessages => _chatMessages;

        /// <summary>Exposes the board from the last accepted server view.</summary>
        public MatchBoard Board
        {
            get { return _board; }
            private set { SetProperty(ref _board, value); }
        }

        public int BidAmount
        {
            get { return _bidAmount; }
            private set { SetProperty(ref _bidAmount, value); }
        }

        public int CurrentBidAmount => Board.CurrentBet;
        public string SelectedPlayerName
        {
            get { return _selectedPlayerName; }
            set
            {
                if (SetProperty(ref _selectedPlayerName, value ?? string.Empty))
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
            MatchView currentMatchView = _matchConnection.CurrentMatchView;
            bool hasNavigatedToGameOver = false;

            if (IsCurrentMatch(currentMatchView))
            {
                _previousMatchView = currentMatchView;
                ShowBoardOf(currentMatchView);
                hasNavigatedToGameOver = ShowFinishedMatch(currentMatchView);
            }

            if (!hasNavigatedToGameOver)
            {
                SubscribeToMatchIfNeeded();
            }
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
            _previousMatchView = null;
            Notification = null;
        }

        protected override void OnDisposed()
        {
            Deactivate();
            _recoveryDisplayTimer.Tick -= UpdateRecoveryDisplay;
            base.OnDisposed();
        }

        private void SubscribeToMatchIfNeeded()
        {
            bool wasSubscriptionSent = _matchConnection.IsMatchSubscribed || _matchConnection.SubscribeToCurrentMatch();

            if (!wasSubscriptionSent && IsDemoPanelVisible)
            {
                _dialogManager.ShowError(NotInMatchErrorKey);
            }
        }

        private void OnRecoveryStateChanged(object sender, EventArgs eventArguments)
        {
            UpdateRecoveryDisplay(sender, eventArguments);
            RefreshGameplayCommands();
        }

        private void UpdateRecoveryDisplay(object sender, EventArgs eventArguments)
        {
            RecoveryMessage = !IsDemoPanelVisible ? MatchRecoveryMessageFormatter.Describe(_matchConnection) : string.Empty;
            OnPropertyChanged(nameof(CanRetryRecovery));
        }

        private void ShowBoardOf(MatchView matchView)
        {
            if (matchView != null)
            {
                int localUserId = _lobbyConnection.PlayerUserId;
                Board = MatchBoardMapper.CreateBoard(matchView, localUserId, _matchModeSettings.CardsPerPlayer);
                NotifyBoardPropertiesChanged();
            }

            UpdateBidAmount();
            RefreshGameplayCommands();
        }

        private void NotifyBoardPropertiesChanged()
        {
            OnPropertyChanged(nameof(CurrentRoundNumber));
            OnPropertyChanged(nameof(PhaseLabelKey));
            OnPropertyChanged(nameof(HasMatchView));
            OnPropertyChanged(nameof(CurrentBidAmount));
        }

        private void UpdateBidAmount()
        {
            int validBidAmount = NoBetAmount;

            if (Board.CanPlaceBet && Board.MinimumBetValue <= Board.MaximumBetValue)
            {
                validBidAmount = Math.Max(Board.MinimumBetValue, Math.Min(BidAmount, Board.MaximumBetValue));
            }

            BidAmount = validBidAmount;
        }

        private void RefreshGameplayCommands()
        {
            _placeCardCommand.NotifyCanExecuteChanged();
            _choosePunishmentCommand.NotifyCanExecuteChanged();
            _revealCardCommand.NotifyCanExecuteChanged();
            RefreshBetCommands();
        }

        private void OnMatchViewChanged(object sender, ServerEventArgs<MatchView> eventArgs)
        {
            MatchView currentMatchView = eventArgs.Payload;

            if (IsNewMatchView(currentMatchView))
            {
                UpdateChallengeNotification(currentMatchView);
                _previousMatchView = currentMatchView;
                ShowBoardOf(currentMatchView);
                UpdateRecoveryDisplay(this, EventArgs.Empty);
                ShowFinishedMatch(currentMatchView);
            }
        }

        private bool IsNewMatchView(MatchView currentMatchView)
        {
            bool isNewMatchView = IsCurrentMatch(currentMatchView)
                && (_previousMatchView == null
                    || _previousMatchView.MatchId != currentMatchView.MatchId
                    || currentMatchView.Version > _previousMatchView.Version);

            return isNewMatchView;
        }

        private void UpdateChallengeNotification(MatchView currentMatchView)
        {
            if (HasChallengeFinished(currentMatchView, _previousMatchView))
            {
                string bettorNickname = string.Empty;

                if (currentMatchView.Players != null)
                {
                    foreach (MatchPlayerView matchPlayer in currentMatchView.Players)
                    {
                        if (matchPlayer.UserId == currentMatchView.Challenge.BettorUserId)
                        {
                            bettorNickname = DisplayNameHelper.Resolve(matchPlayer.UserId, matchPlayer.Nickname);
                        }
                    }
                }

                bool wasChallengeSuccessful =
                    currentMatchView.Challenge.Outcome == MatchChallengeOutcome.Succeeded;

                Notification = new GameNotification
                {
                    TitleKey = wasChallengeSuccessful ? ChallengeSuccessTitleKey : ChallengeFailureTitleKey,
                    BodyKey = wasChallengeSuccessful ? ChallengeSuccessBodyKey : ChallengeFailureBodyKey,
                    Subject = bettorNickname,
                    Icon = wasChallengeSuccessful ? ChallengeSuccessIcon : ChallengeFailureIcon,
                    IsSuccess = wasChallengeSuccessful
                };
            }
            else if (currentMatchView.Challenge?.Outcome == MatchChallengeOutcome.InProgress
                || currentMatchView.MatchId != _previousMatchView?.MatchId)
            {
                Notification = null;
            }
        }

        private static bool HasChallengeFinished(MatchView currentMatchView, MatchView previousMatchView)
        {
            bool hasChallengeFinished = false;

            if (currentMatchView?.Challenge != null && previousMatchView?.Challenge != null)
            {
                bool wasChallengeInProgress =
                    previousMatchView.Challenge.Outcome == MatchChallengeOutcome.InProgress;
                bool hasFinalChallengeStatus =
                    currentMatchView.Challenge.Outcome == MatchChallengeOutcome.Succeeded
                    || currentMatchView.Challenge.Outcome == MatchChallengeOutcome.Failed;
                int currentRevealedCardCount =
                    currentMatchView.Challenge.RevealedCards?.Length ?? NoRevealedCards;
                int previousRevealedCardCount =
                    previousMatchView.Challenge.RevealedCards?.Length ?? NoRevealedCards;

                hasChallengeFinished = wasChallengeInProgress && hasFinalChallengeStatus
                    && IsSameChallenge(currentMatchView, previousMatchView)
                    && currentRevealedCardCount > previousRevealedCardCount;
            }

            return hasChallengeFinished;
        }

        private static bool IsSameChallenge(MatchView currentMatchView, MatchView previousMatchView)
        {
            bool isSameChallenge = currentMatchView.MatchId == previousMatchView.MatchId
                && currentMatchView.Challenge.BettorUserId == previousMatchView.Challenge.BettorUserId
                && currentMatchView.Challenge.PromisedCount == previousMatchView.Challenge.PromisedCount
                && currentMatchView.RoundNumber >= previousMatchView.RoundNumber
                && currentMatchView.RoundNumber <= previousMatchView.RoundNumber + NextRoundOffset;

            return isSameChallenge;
        }

        private bool IsCurrentMatch(MatchView matchView)
        {
            LobbyRoom currentRoom = _lobbyConnection.CurrentRoom;
            bool isCurrentMatch = matchView != null && currentRoom != null
                && currentRoom.MatchId.HasValue && matchView.MatchId == currentRoom.MatchId.Value;

            return isCurrentMatch;
        }

        private bool ShowFinishedMatch(MatchView matchView)
        {
            bool isFinished = matchView.Phase == MatchRoundPhase.MatchFinished
                && _finishedMatchId != matchView.MatchId;
            if (isFinished)
            {
                _finishedMatchId = matchView.MatchId;
                Notification = null;
                _navigationManager.NavigateToWithoutHistory(ScreenId.GameOver, matchView);
            }

            return isFinished;
        }

        private void OnMatchErrorReceived(object sender, ServerEventArgs<string> eventArgs)
        {
            if (!_matchConnection.IsMatchRecoveryPending && !_matchConnection.IsMatchRecoveryExhausted)
            {
                _dialogManager.ShowError(eventArgs.Payload);
            }
        }

        private void OnMatchAvailabilityChanged(object sender, EventArgs eventArguments)
        {
            IsMatchUnavailable = !_matchConnection.IsMatchSubscribed;
            MatchView currentMatchView = _matchConnection.CurrentMatchView;

            if (!IsMatchUnavailable)
            {
                _previousMatchView = currentMatchView;
                ShowBoardOf(currentMatchView);
            }
            else
            {
                _previousMatchView = null;
                Notification = null;

                if (!IsCurrentMatch(currentMatchView))
                {
                    Board = MatchBoardMapper.CreateEmptyBoard();
                    NotifyBoardPropertiesChanged();
                }

                UpdateBidAmount();
                RefreshGameplayCommands();
            }
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
            bool canPlaceBet = _matchConnection.CanSendGameplayAction && Board.CanPlaceBet
                && BidAmount >= Board.MinimumBetValue && BidAmount <= Board.MaximumBetValue;

            return canPlaceBet;
        }

        private void ExecutePlaceBet()
        {
            if (CanExecutePlaceBet())
            {
                _matchConnection.PlaceBet(BidAmount);
            }
        }

        private bool CanExecuteWithdrawBet()
        {
            bool canWithdrawFromBet = _matchConnection.CanSendGameplayAction && Board.CanWithdrawFromBet;

            return canWithdrawFromBet;
        }

        private void ExecuteWithdrawBet()
        {
            if (CanExecuteWithdrawBet())
            {
                _matchConnection.WithdrawFromBet();
            }
        }

        private bool CanIncreaseBet()
        {
            bool canIncreaseBet = CanExecutePlaceBet() && BidAmount < Board.MaximumBetValue;

            return canIncreaseBet;
        }

        private void ExecuteIncreaseBet()
        {
            if (CanIncreaseBet())
            {
                BidAmount++;
                RefreshBetCommands();
            }
        }

        private bool CanDecreaseBet()
        {
            bool canDecreaseBet = CanExecutePlaceBet() && BidAmount > Board.MinimumBetValue;

            return canDecreaseBet;
        }

        private void ExecuteDecreaseBet()
        {
            if (CanDecreaseBet())
            {
                BidAmount--;
                RefreshBetCommands();
            }
        }

        private bool CanExecuteRevealCard(MatchPlayerEntry pileOwner)
        {
            bool canRevealCard = pileOwner != null && pileOwner.PileCardCount > EmptyPileCardCount
                && _matchConnection.CanSendGameplayAction && Board.CanRevealCard;

            return canRevealCard;
        }

        private void ExecuteRevealCard(MatchPlayerEntry pileOwner)
        {
            if (CanExecuteRevealCard(pileOwner))
            {
                _matchConnection.RevealCard(pileOwner.UserId);
            }
        }

        private bool CanChoosePunishment(HandCardEntry card)
        {
            bool canChoosePunishment = card != null && _matchConnection.CanSendGameplayAction && Board.CanChoosePunishment;

            return canChoosePunishment;
        }

        private void ExecuteChoosePunishment(HandCardEntry card)
        {
            if (CanChoosePunishment(card))
            {
                _matchConnection.ChoosePunishmentCard(card.Index);
            }
        }

        private void ExecutePlaceCard(HandCardEntry handCard)
        {
            if (CanExecutePlaceCard(handCard))
            {
                _matchConnection.PlaceCard(MatchBoardMapper.ConvertToServerCard(handCard.Kind));
            }
        }

        private bool CanExecutePlaceCard(HandCardEntry handCard)
        {
            bool canPlaceCard = handCard != null && _board.CanPlaceCards && _matchConnection.CanSendGameplayAction;

            return canPlaceCard;
        }

        private void ExecuteSendChat()
        {
            string messageText = ChatInputHelper.TrimMessageText(ChatInput);

            if (CanSendChatMessage(messageText) && _lobbyConnection.SendChatMessage(messageText))
            {
                ChatInput = string.Empty;
            }
        }

        private bool CanExecuteSendChat()
        {
            string messageText = ChatInputHelper.TrimMessageText(ChatInput);
            bool canSendMessage = CanSendChatMessage(messageText);

            return canSendMessage;
        }

        private bool CanSendChatMessage(string messageText)
        {
            bool canSendMessage = _lobbyConnection.IsChatSubscribed
                && !ChatInputHelper.IsEmptyMessage(messageText)
                && ChatInputHelper.CanEnterText(messageText);

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
            _navigationManager.ClearHistory();
            _navigationManager.NavigateToWithoutHistory(ScreenId.MainMenu);
        }

        private void ExecuteReportPlayer()
        {
            _navigationManager.NavigateToWithoutHistory(
                ScreenId.Moderation,
                new ModerationNavigationParameter(SelectedPlayerName, ModerationMode.Report));
        }

        private void ExecuteKickPlayer()
        {
            KickVoteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteBlockPlayer()
        {
            _navigationManager.NavigateToWithoutHistory(
                ScreenId.Moderation,
                new ModerationNavigationParameter(SelectedPlayerName, ModerationMode.Block));
        }

        private void ExecuteDemoRevealCards()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.RoundResult);
        }

        private void ExecuteDemoGameOver()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.GameOver);
        }

        private void ExecuteDemoInactivity()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.MatchClosed);
        }
    }
}
