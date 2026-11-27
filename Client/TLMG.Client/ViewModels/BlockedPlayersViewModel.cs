using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;
using log4net;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public sealed class BlockedPlayersViewModel : ViewModelBase
    {
        private const int NoUserId = 0;
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private const string ModerationNotSentErrorKey = "Moderation_NotSentError";
        private static readonly ILog _log = LogManager.GetLogger(typeof(BlockedPlayersViewModel));

        private readonly IFriendsConnection _friendsConnection;
        private readonly IModerationConnection _moderationConnection;
        private readonly IDialogManager _dialogManager;
        private readonly ObservableCollection<BlockedPlayer> _players;
        private bool _isActive;
        private bool _isAwaitingList;
        private bool _isBusy;
        private bool _hasLoaded;
        private bool _hasLookupFailed;

        public BlockedPlayersViewModel(IFriendsConnection friendsConnection,
            IModerationConnection moderationConnection, IDialogManager dialogManager)
        {
            _friendsConnection = friendsConnection;
            _moderationConnection = moderationConnection;
            _dialogManager = dialogManager;
            _players = new ObservableCollection<BlockedPlayer>();
            Players = new ReadOnlyObservableCollection<BlockedPlayer>(_players);
            RefreshCommand = new RelayCommand(Refresh);
            UnblockCommand = new RelayCommand<BlockedPlayer>(Unblock);
        }

        public ReadOnlyObservableCollection<BlockedPlayer> Players { get; }
        public ICommand RefreshCommand { get; }
        public ICommand UnblockCommand { get; }
        public bool CanInteract => !IsBusy;
        public bool IsEmpty => HasLoaded && Players.Count == 0;

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanInteract));
                }
            }
        }

        public bool HasLoaded
        {
            get { return _hasLoaded; }
            private set
            {
                if (SetProperty(ref _hasLoaded, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool HasLookupFailed
        {
            get { return _hasLookupFailed; }
            private set { SetProperty(ref _hasLookupFailed, value); }
        }

        public void Activate()
        {
            if (!_isActive)
            {
                _isActive = true;
                _moderationConnection.BlockedPlayersReceived += OnBlockedPlayersReceived;
                _moderationConnection.UnblockPlayerResultReceived += OnUnblockResultReceived;

                Refresh();
            }
        }

        public void Deactivate()
        {
            _isActive = false;
            _moderationConnection.BlockedPlayersReceived -= OnBlockedPlayersReceived;
            _moderationConnection.UnblockPlayerResultReceived -= OnUnblockResultReceived;
            _isAwaitingList = false;
            IsBusy = false;
            HasLoaded = false;
            HasLookupFailed = false;
            _players.Clear();
        }

        protected override void OnDisposed()
        {
            Deactivate();
            base.OnDisposed();
        }

        private void Refresh()
        {
            if (_isActive && _friendsConnection.HasRegisteredSession && !IsBusy)
            {
                HasLoaded = false;
                HasLookupFailed = false;
                _players.Clear();
                _isAwaitingList = true;
                IsBusy = true;

                if (!_moderationConnection.GetBlockedPlayers())
                {
                    ShowFailure(ModerationNotSentErrorKey);
                }
            }
        }

        private void Unblock(BlockedPlayer player)
        {
            bool canUnblock = _isActive && _friendsConnection.HasRegisteredSession && !IsBusy
                && HasLoaded && player != null && player.UserId > NoUserId && _players.Contains(player);

            if (canUnblock)
            {
                _isAwaitingList = false;
                IsBusy = true;
                if (!_moderationConnection.UnblockPlayer(player.UserId))
                {
                    ShowFailure(ModerationNotSentErrorKey);
                }
            }
        }

        private void OnBlockedPlayersReceived(object sender, ServerEventArgs<BlockedPlayerList> eventArguments)
        {
            if (_isActive && _friendsConnection.HasRegisteredSession && IsBusy && _isAwaitingList)
            {
                BlockedPlayerList result = ServerConnectionManager.NormalizeOperationResult(eventArguments?.Payload);

                if (!result.IsSuccess)
                {
                    ShowFailure(result.ErrorKey);
                }
                else if (!IsValidList(result.Players))
                {
                    _log.Warn("The server returned invalid blocked-player entries.");
                    ShowFailure(UnexpectedErrorKey);
                }
                else
                {
                    foreach (BlockedPlayer player in result.Players)
                    {
                        _players.Add(player);
                    }

                    _isAwaitingList = false;
                    HasLoaded = true;
                    IsBusy = false;
                }
            }
        }

        private void OnUnblockResultReceived(object sender, ServerEventArgs<OperationResult> eventArguments)
        {
            if (_isActive && _friendsConnection.HasRegisteredSession && IsBusy && !_isAwaitingList)
            {
                OperationResult result = ServerConnectionManager.NormalizeOperationResult(eventArguments?.Payload);

                if (result.IsSuccess)
                {
                    IsBusy = false;
                    Refresh();
                }
                else
                {
                    ShowFailure(result.ErrorKey);
                }
            }
        }

        private void ShowFailure(string errorKey)
        {
            _isAwaitingList = false;
            HasLoaded = false;
            HasLookupFailed = true;
            _players.Clear();
            IsBusy = false;

            if (string.IsNullOrWhiteSpace(errorKey))
            {
                errorKey = UnexpectedErrorKey;
            }

            _dialogManager.ShowError(errorKey);
        }

        private static bool IsValidList(IEnumerable<BlockedPlayer> players)
        {
            bool isValid = players != null;
            HashSet<int> userIds = new HashSet<int>();

            if (isValid)
            {
                foreach (BlockedPlayer player in players)
                {
                    if (player == null || player.UserId <= NoUserId || string.IsNullOrWhiteSpace(player.Nickname)
                        || !userIds.Add(player.UserId))
                    {
                        isValid = false;
                    }
                }
            }

            return isValid;
        }
    }
}