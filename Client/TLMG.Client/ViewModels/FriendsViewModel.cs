using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
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

        private readonly IFriendsConnection _friendsConnection;
        private readonly INavigationManager _navigationManager;
        private readonly RelayCommand _addFriendCommand;
        private readonly RelayCommand<FriendRequestEntry> _acceptRequestCommand;
        private readonly RelayCommand<FriendRequestEntry> _declineRequestCommand;

        private string _newFriendNickname = string.Empty;
        private string _friendsBadgeCount = "0";
        private string _pendingBadgeCount = "0";
        private bool _isRegisteredPlayer;
        private bool _isEmptyFriendsVisible;
        private bool _isEmptyPendingVisible;
        private bool _isBusy;

        public FriendsViewModel(IFriendsConnection friendsConnection, INavigationManager navigationManager)
        {
            _friendsConnection = friendsConnection;
            _navigationManager = navigationManager;

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

            RefreshLists();

            if (_friendsConnection.HasSession && _friendsConnection.FriendList == null)
            {
                _friendsConnection.RequestFriendList();
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
        }

        protected override void OnDisposed()
        {
            Deactivate();
            FriendsMessage.Dispose();

            base.OnDisposed();
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteInviteFriend(FriendEntry friendEntry)
        {
            bool wasSent = _friendsConnection.InviteFriendToRoom(friendEntry.UserId);

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
            bool wasSent = _friendsConnection.SendFriendRequest(NewFriendNickname.Trim());

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
            bool wasSent = _friendsConnection.AcceptFriendRequest(requestEntry.FriendRequestId);

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
            bool wasSent = _friendsConnection.DeclineFriendRequest(requestEntry.FriendRequestId);

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

        private void OnFriendListReceived(object sender, ServerEventArgs<OperationSubscribeToFriendsResult> eventArgs)
        {
            if (eventArgs.Payload.IsSuccess)
            {
                RefreshLists();
            }
            else
            {
                ShowError(eventArgs.Payload.ErrorKey);
            }
        }

        private void OnFriendPresenceChanged(object sender, ServerEventArgs<Friend> eventArgs)
        {
            RefreshLists();
        }

        private void OnFriendRequestReceived(object sender, ServerEventArgs<FriendRequest> eventArgs)
        {
            RefreshLists();
            ShowSuccessWithNickname(
                NewRequestFormatKey,
                eventArgs.Payload.UserId,
                eventArgs.Payload.Nickname);
        }

        private void OnFriendAdded(object sender, ServerEventArgs<Friend> eventArgs)
        {
            RefreshLists();
            ShowSuccessWithNickname(
                RequestAcceptedFormatKey,
                eventArgs.Payload.UserId,
                eventArgs.Payload.Nickname);
        }

        private void OnInviteFriendResultReceived(
            object sender,
            ServerEventArgs<OperationResult> eventArgs)
        {
            if (eventArgs.Payload.IsSuccess)
            {
                FriendsMessage.ShowSuccess(LanguageManager.FindText(InvitationSentMessageKey));
            }
            else
            {
                ShowError(eventArgs.Payload.ErrorKey);
            }
        }

        private void OnSendFriendRequestResultReceived(
            object sender,
            ServerEventArgs<OperationSendFriendRequestResult> eventArgs)
        {
            OperationSendFriendRequestResult sendResult = eventArgs.Payload;
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
            else
            {
                NewFriendNickname = string.Empty;
                FriendsMessage.ShowSuccess(LanguageManager.FindText(RequestSentMessageKey));
            }
        }

        private void OnAcceptFriendRequestResultReceived(
            object sender,
            ServerEventArgs<OperationAcceptFriendRequestResult> eventArgs)
        {
            OperationAcceptFriendRequestResult acceptResult = eventArgs.Payload;
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
        }

        private void OnDeclineFriendRequestResultReceived(
            object sender,
            ServerEventArgs<OperationDeclineFriendRequestResult> eventArgs)
        {
            OperationDeclineFriendRequestResult declineResult = eventArgs.Payload;
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
            bool hasRoom = _friendsConnection.HasCurrentRoom;
            bool hasSession = _friendsConnection.HasSession;

            FriendsList.Clear();
            if (_friendsConnection.FriendList != null)
            {
                foreach (Friend friend in _friendsConnection.FriendList)
                {
                    FriendsList.Add(new FriendEntry
                    {
                        UserId = friend.UserId,
                        Nickname = DisplayNameHelper.Resolve(friend.UserId, friend.Nickname),
                        IsOnline = friend.IsOnline,
                        CanBeInvited = friend.IsOnline && hasRoom
                    });
                }
            }

            PendingRequests.Clear();
            if (_friendsConnection.PendingFriendRequests != null)
            {
                foreach (FriendRequest friendRequest in _friendsConnection.PendingFriendRequests)
                {
                    PendingRequests.Add(new FriendRequestEntry
                    {
                        FriendRequestId = friendRequest.FriendRequestId,
                        Nickname = DisplayNameHelper.Resolve(friendRequest.UserId, friendRequest.Nickname),
                        IsIncoming = friendRequest.IsIncoming
                    });
                }
            }

            IsRegisteredPlayer = _friendsConnection.HasRegisteredSession;
            FriendsBadgeCount = FriendsList.Count.ToString();
            PendingBadgeCount = PendingRequests.Count.ToString();
            IsEmptyFriendsVisible = hasSession && FriendsList.Count == 0;
            IsEmptyPendingVisible = hasSession && PendingRequests.Count == 0;
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