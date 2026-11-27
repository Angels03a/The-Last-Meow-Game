using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using log4net;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public class FriendsViewModel : ViewModelBase
    {
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private const string InvitationSentMessageKey = "Friends_InvitationSentMessage";
        private const string RequestSentMessageKey = "Friends_RequestSentMessage";
        private const string RequestDeclinedMessageKey = "Friends_RequestDeclinedMessage";
        private const string RequestAcceptedFormatKey = "Friends_RequestAcceptedFormat";
        private const string NewRequestFormatKey = "Friends_NewRequestFormat";

        private static readonly ILog _log = LogManager.GetLogger(typeof(FriendsViewModel));

        private readonly IFriendsConnection _friendsConnection;
        private readonly INavigationManager _navigationManager;
        private readonly RelayCommand _addFriendCommand;
        private readonly RelayCommand<FriendRequestEntry> _acceptRequestCommand;
        private readonly RelayCommand<FriendRequestEntry> _declineRequestCommand;

        private string _newFriendNickname = string.Empty;
        private string _friendsBadgeCount = string.Empty;
        private string _pendingBadgeCount = string.Empty;
        private bool _isRegisteredPlayer;
        private bool _isEmptyFriendsVisible;
        private bool _isEmptyPendingVisible;
        private bool _isBusy;

        public FriendsViewModel(IFriendsConnection friendsConnection, INavigationManager navigationManager,
            BlockedPlayersViewModel blockedPlayers)
        {
            _friendsConnection = friendsConnection;
            _navigationManager = navigationManager;
            BlockedPlayers = blockedPlayers;

            FriendsList = new ObservableCollection<FriendEntry>();
            PendingRequests = new ObservableCollection<FriendRequestEntry>();
            FriendsMessage = new StatusMessage();

            BackCommand = new RelayCommand(ExecuteBack);
            InviteFriendCommand = new RelayCommand<FriendEntry>(ExecuteInviteFriend, CanExecuteInviteFriend);
            _addFriendCommand = new RelayCommand(ExecuteAddFriend, CanExecuteAddFriend);
            _acceptRequestCommand = new RelayCommand<FriendRequestEntry>(
                ExecuteAcceptRequest, CanExecuteAnswerRequest);
            _declineRequestCommand = new RelayCommand<FriendRequestEntry>(
                ExecuteDeclineRequest, CanExecuteAnswerRequest);

            RefreshLists();
        }

        public ICommand BackCommand { get; }



        public ICommand InviteFriendCommand { get; }

        public ICommand AddFriendCommand => _addFriendCommand;

        public ICommand AcceptRequestCommand => _acceptRequestCommand;

        public ICommand DeclineRequestCommand => _declineRequestCommand;

        public ObservableCollection<FriendEntry> FriendsList { get; }

        public ObservableCollection<FriendRequestEntry> PendingRequests { get; }

        public StatusMessage FriendsMessage { get; }

        public BlockedPlayersViewModel BlockedPlayers { get; }

        public string NewFriendNickname
        {
            get { return _newFriendNickname; }
            set
            {
                bool wasChanged = SetProperty(ref _newFriendNickname, value);
                if (wasChanged)
                {
                    _addFriendCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string FriendsBadgeCount
        {
            get { return _friendsBadgeCount; }
            private set { SetProperty(ref _friendsBadgeCount, value); }
        }

        public string PendingBadgeCount
        {
            get { return _pendingBadgeCount; }
            private set { SetProperty(ref _pendingBadgeCount, value); }
        }

        public bool IsRegisteredPlayer
        {
            get { return _isRegisteredPlayer; }
            private set
            {
                bool wasChanged = SetProperty(ref _isRegisteredPlayer, value);
                if (wasChanged)
                {
                    OnPropertyChanged(nameof(IsGuest));
                    _addFriendCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsGuest
        {
            get { return !_isRegisteredPlayer; }
        }

        public bool IsEmptyFriendsVisible
        {
            get { return _isEmptyFriendsVisible; }
            private set { SetProperty(ref _isEmptyFriendsVisible, value); }
        }

        public bool IsEmptyPendingVisible
        {
            get { return _isEmptyPendingVisible; }
            private set { SetProperty(ref _isEmptyPendingVisible, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                bool wasChanged = SetProperty(ref _isBusy, value);
                if (wasChanged)
                {
                    NotifyRequestCommandsCanExecuteChanged();
                }
            }
        }

        public void Activate()
        {
            Deactivate();

            _friendsConnection.FriendListReceived += OnFriendListReceived;
            _friendsConnection.FriendPresenceChanged += OnFriendPresenceChanged;
            _friendsConnection.FriendRequestReceived += OnFriendRequestReceived;
            _friendsConnection.FriendAdded += OnFriendAdded;
            _friendsConnection.InviteFriendResultReceived += OnInviteFriendResultReceived;
            _friendsConnection.SendFriendRequestResultReceived += OnSendFriendRequestResultReceived;
            _friendsConnection.AcceptFriendRequestResultReceived += OnAcceptFriendRequestResultReceived;
            _friendsConnection.DeclineFriendRequestResultReceived += OnDeclineFriendRequestResultReceived;
            LanguageManager.LanguageChanged += OnLanguageChanged;
            BlockedPlayers.Activate();

            RefreshLists();

            if (_friendsConnection.HasSession && !_friendsConnection.RequestFriendList())
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        public void Deactivate()
        {
            _friendsConnection.FriendListReceived -= OnFriendListReceived;
            _friendsConnection.FriendPresenceChanged -= OnFriendPresenceChanged;
            _friendsConnection.FriendRequestReceived -= OnFriendRequestReceived;
            _friendsConnection.FriendAdded -= OnFriendAdded;
            _friendsConnection.InviteFriendResultReceived -= OnInviteFriendResultReceived;
            _friendsConnection.SendFriendRequestResultReceived -= OnSendFriendRequestResultReceived;
            _friendsConnection.AcceptFriendRequestResultReceived -= OnAcceptFriendRequestResultReceived;
            _friendsConnection.DeclineFriendRequestResultReceived -= OnDeclineFriendRequestResultReceived;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            BlockedPlayers.Deactivate();
        }

        protected override void OnDisposed()
        {
            Deactivate();
            FriendsMessage.Dispose();
            BlockedPlayers.Dispose();

            base.OnDisposed();
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteInviteFriend(FriendEntry friendEntry)
        {
            bool wasSent = CanExecuteInviteFriend(friendEntry)
                && _friendsConnection.InviteFriendToRoom(friendEntry.UserId);

            if (!wasSent)
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        private bool CanExecuteInviteFriend(FriendEntry friendEntry)
        {
            bool canInvite = friendEntry != null && friendEntry.CanBeInvited;
            return canInvite;
        }

        private void ExecuteAddFriend()
        {
            FriendsMessage.Hide();
            bool wasSent = CanExecuteAddFriend()
                && _friendsConnection.SendFriendRequest(NewFriendNickname.Trim());

            if (wasSent)
            {
                IsBusy = true;
            }
            else
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        private bool CanExecuteAddFriend()
        {
            bool hasNickname = !string.IsNullOrWhiteSpace(NewFriendNickname);
            bool canAddFriend = IsRegisteredPlayer && hasNickname && !IsBusy;
            return canAddFriend;
        }

        private void ExecuteAcceptRequest(FriendRequestEntry requestEntry)
        {
            FriendsMessage.Hide();
            bool wasSent = CanExecuteAnswerRequest(requestEntry)
                && _friendsConnection.AcceptFriendRequest(requestEntry.FriendRequestId);

            if (wasSent)
            {
                IsBusy = true;
            }
            else
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        private void ExecuteDeclineRequest(FriendRequestEntry requestEntry)
        {
            FriendsMessage.Hide();
            bool wasSent = CanExecuteAnswerRequest(requestEntry)
                && _friendsConnection.DeclineFriendRequest(requestEntry.FriendRequestId);

            if (wasSent)
            {
                IsBusy = true;
            }
            else
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        private bool CanExecuteAnswerRequest(FriendRequestEntry requestEntry)
        {
            bool canAnswer = requestEntry != null && requestEntry.IsIncoming && !IsBusy;
            return canAnswer;
        }

        private void NotifyRequestCommandsCanExecuteChanged()
        {
            _addFriendCommand.NotifyCanExecuteChanged();
            _acceptRequestCommand.NotifyCanExecuteChanged();
            _declineRequestCommand.NotifyCanExecuteChanged();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            RefreshLists();
        }

        private void OnFriendListReceived(object sender, ServerEventArgs<OperationSubscribeToFriendsResult> eventArguments)
        {
            OperationSubscribeToFriendsResult result = ServerConnectionManager.NormalizeOperationResult(eventArguments?.Payload);

            if (result.IsSuccess)
            {
                RefreshLists();
            }
            else
            {
                ShowError(result.ErrorKey);
            }
        }

        private void OnFriendPresenceChanged(object sender, ServerEventArgs<Friend> eventArgs)
        {
            RefreshLists();
        }

        private void OnFriendRequestReceived(object sender, ServerEventArgs<FriendRequest> eventArgs)
        {
            FriendRequest request = eventArgs?.Payload;
            if (request != null)
            {
                RefreshLists();
                ShowSuccessWithNickname(NewRequestFormatKey, request.UserId, request.Nickname);
            }
            else
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        private void OnFriendAdded(object sender, ServerEventArgs<Friend> eventArgs)
        {
            Friend friend = eventArgs?.Payload;
            if (friend != null)
            {
                RefreshLists();
                ShowSuccessWithNickname(RequestAcceptedFormatKey, friend.UserId, friend.Nickname);
            }
            else
            {
                ShowError(UnexpectedErrorKey);
            }
        }

        private void OnInviteFriendResultReceived(
            object sender,
            ServerEventArgs<OperationResult> eventArgs)
        {
            OperationResult result = ServerConnectionManager.NormalizeOperationResult(eventArgs?.Payload);
            if (result.IsSuccess)
            {
                FriendsMessage.ShowSuccess(LanguageManager.FindText(InvitationSentMessageKey));
            }
            else
            {
                ShowError(result.ErrorKey);
            }
        }

        private void OnSendFriendRequestResultReceived(
            object sender,
            ServerEventArgs<OperationSendFriendRequestResult> eventArgs)
        {
            OperationSendFriendRequestResult sendResult = ServerConnectionManager.NormalizeOperationResult(eventArgs?.Payload);
            IsBusy = false;
            RefreshLists();

            if (!sendResult.IsSuccess)
            {
                ShowError(sendResult.ErrorKey);
            }
            else if (sendResult.NewFriend != null)
            {
                NewFriendNickname = string.Empty;
                ShowSuccessWithNickname(
                    RequestAcceptedFormatKey,
                    sendResult.NewFriend.UserId,
                    sendResult.NewFriend.Nickname);
            }
            else if (sendResult.SentFriendRequest != null)
            {
                NewFriendNickname = string.Empty;
                FriendsMessage.ShowSuccess(LanguageManager.FindText(RequestSentMessageKey));
            }
            else
            {
                _log.Warn("A successful friendship response did not include a friend or a pending friendship.");
                ShowError(UnexpectedErrorKey);
            }
        }

        private void OnAcceptFriendRequestResultReceived(
            object sender,
            ServerEventArgs<OperationAcceptFriendRequestResult> eventArgs)
        {
            OperationAcceptFriendRequestResult acceptResult = ServerConnectionManager.NormalizeOperationResult(eventArgs?.Payload);
            IsBusy = false;
            RefreshLists();

            if (!acceptResult.IsSuccess)
            {
                ShowError(acceptResult.ErrorKey);
            }
            else if (acceptResult.NewFriend != null)
            {
                ShowSuccessWithNickname(
                    RequestAcceptedFormatKey,
                    acceptResult.NewFriend.UserId,
                    acceptResult.NewFriend.Nickname);
            }
            else
            {
                _log.Warn("A successful friendship acceptance did not include the new friend.");
                ShowError(UnexpectedErrorKey);
            }
        }

        private void OnDeclineFriendRequestResultReceived(
            object sender,
            ServerEventArgs<OperationDeclineFriendRequestResult> eventArgs)
        {
            OperationDeclineFriendRequestResult declineResult = ServerConnectionManager.NormalizeOperationResult(eventArgs?.Payload);
            IsBusy = false;
            RefreshLists();

            if (declineResult.IsSuccess)
            {
                FriendsMessage.ShowSuccess(LanguageManager.FindText(RequestDeclinedMessageKey));
            }
            else
            {
                ShowError(declineResult.ErrorKey);
            }
        }

        private void RefreshLists()
        {
            bool hasSession = _friendsConnection.HasSession;
            bool hasFriendsSnapshot = FriendsListProjection.PopulateFriends(_friendsConnection, FriendsList);
            bool hasPendingSnapshot = FriendsListProjection.PopulatePending(_friendsConnection, PendingRequests);

            IsRegisteredPlayer = _friendsConnection.HasRegisteredSession;
            FriendsBadgeCount = string.Empty;
            PendingBadgeCount = string.Empty;

            if (hasFriendsSnapshot)
            {
                FriendsBadgeCount = FriendsList.Count.ToString();
            }

            if (hasPendingSnapshot)
            {
                PendingBadgeCount = PendingRequests.Count.ToString();
            }

            IsEmptyFriendsVisible = hasSession && hasFriendsSnapshot && FriendsList.Count == 0;
            IsEmptyPendingVisible = hasSession && hasPendingSnapshot && PendingRequests.Count == 0;
            NotifyRequestCommandsCanExecuteChanged();
        }

        private void ShowError(string errorKey)
        {
            FriendsMessage.ShowError(LanguageManager.FindText(errorKey));
        }

        private void ShowSuccessWithNickname(string formatKey, int userId, string nickname)
        {
            string messageFormat = LanguageManager.FindText(formatKey);
            string displayName = DisplayNameHelper.Resolve(userId, nickname);
            FriendsMessage.ShowSuccess(string.Format(messageFormat, displayName));
        }
    }
}