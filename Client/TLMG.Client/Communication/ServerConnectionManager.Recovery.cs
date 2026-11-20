using System;
using System.Diagnostics;
using System.ServiceModel;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager
    {

        private const int RecoveryReplyMilliseconds = 2000;
        private const int RecoveryWindowMilliseconds = 12000;
        private const int SecondaryRecoveryAttemptLimit = 3;
        private static readonly TimeSpan _recoveryOpenTimeout = TimeSpan.FromMilliseconds(650);
        private static readonly TimeSpan _recoveryRequestTimeout = TimeSpan.FromMilliseconds(500);

        public event EventHandler RecoveryStateChanged;
        internal event EventHandler<ServerEventArgs<MatchView>> MatchRecoveryReady;
        internal event EventHandler MatchContextEnded;

        public bool IsMatchRecoveryPending => _recoveryUntil.HasValue;
        public bool IsMatchRecoveryExhausted { get; private set; }

        private bool _startingSession;
        private long _nextRecoveryStepAt;
        private int? _lobbyBoundRoomId;
        private long? _recoveryUntil;
        private long? _recoveryLobbyUntil;
        private OperationRecoveryContextResult _recoveryContext;
        private long? _friendsSubscriptionUntil;
        private long? _chatSubscriptionUntil;
        private int _friendsRecoveryAttempts;
        private int _chatRecoveryAttempts;

        public void RetryMatchRecovery()
        {
            if (HasRegisteredSession && IsMatchRecoveryExhausted)
            {
                IsMatchRecoveryExhausted = false;
                RequestRecovery();
            }
        }

        private void RequestRecovery()
        {
            if (HasRegisteredSession && !_recoveryUntil.HasValue && !IsMatchRecoveryExhausted)
            {
                _recoveryUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryWindowMilliseconds;
                _recoveryContext = null;
                RecoveryStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CancelMatchRecovery()
        {
            bool changed = _recoveryUntil.HasValue || IsMatchRecoveryExhausted;
            _recoveryUntil = null;
            _recoveryLobbyUntil = null;
            _recoveryContext = null;
            IsMatchRecoveryExhausted = false;
            if (changed)
            {
                RecoveryStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ResetRecovery()
        {
            CancelMatchRecovery();
            _nextRecoveryStepAt = 0;
            _lobbyBoundRoomId = null;
            _friendsRecoveryAttempts = 0;
            _chatRecoveryAttempts = 0;
            _friendsSubscriptionUntil = null;
            _chatSubscriptionUntil = null;
        }

        private void DiscoverRecoveryContext()
        {
            var context = GetRecoveryContext();
            if (context.ErrorKey == InvalidSessionErrorKey)
            {
                DisconnectAndNotifyConnectionLost();
            }
            else if (context.IsSuccess)
            {
                if (!context.RoomId.HasValue || !context.MatchId.HasValue)
                {
                    EndRecoveryContext();
                }
                else
                {
                    _recoveryContext = context;
                }
            }
        }

        private void EndRecoveryContext()
        {
            if (CurrentRoom?.MatchId.HasValue == true)
            {
                ClearCurrentRoom();
            }
            CancelMatchRecovery();
            if (!_startingSession)
            {
                MatchContextEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool AdvanceConnectionRecovery(long now)
        {
            if (_matchSubscriptionUntil.HasValue && now >= _matchSubscriptionUntil.Value)
            {
                CloseMatchChannel();
            }
            if (_recoveryLobbyUntil.HasValue && now >= _recoveryLobbyUntil.Value)
            {
                CloseLobbyChannel();
                _recoveryContext = null;
            }
            if (_friendsSubscriptionUntil.HasValue && now >= _friendsSubscriptionUntil.Value)
            {
                CloseFriendsChannel();
            }
            if (_chatSubscriptionUntil.HasValue && now >= _chatSubscriptionUntil.Value)
            {
                CloseChatChannel();
            }
            if (_recoveryUntil.HasValue && now >= _recoveryUntil.Value)
            {
                if (_recoveryContext != null && !_recoveryLobbyUntil.HasValue
                    && !_matchSubscriptionUntil.HasValue && !_enteringRoomId.HasValue
                    && _lobbyClient != null && _lobbyBoundRoomId == _recoveryContext.RoomId
                    && CurrentRoom?.MatchId == _recoveryContext.MatchId && IsMatchSubscribed
                    && CurrentMatchView?.MatchId == _recoveryContext.MatchId)
                {
                    return AdvanceMatchRecovery();
                }
                if (_recoveryLobbyUntil.HasValue)
                {
                    CloseLobbyChannel();
                }
                if (_matchSubscriptionUntil.HasValue)
                {
                    CloseMatchChannel();
                }
                DiscoverRecoveryContext();
                if (_recoveryUntil.HasValue)
                {
                    _recoveryUntil = null;
                    IsMatchRecoveryExhausted = true;
                    RecoveryStateChanged?.Invoke(this, EventArgs.Empty);
                }
                return true;
            }

            if (now < _nextHeartbeatSendAt || now < _nextRecoveryStepAt)
            {
                return false;
            }

            if (_recoveryUntil.HasValue)
            {
                return AdvanceMatchRecovery();
            }

            if (_friendsClient == null && _friendsRecoveryAttempts < SecondaryRecoveryAttemptLimit)
            {
                _friendsRecoveryAttempts++;
                TryOpenFriendsChannel();
                return true;
            }
            if (CurrentRoom != null && _chatClient == null && _chatRecoveryAttempts < SecondaryRecoveryAttemptLimit)
            {
                _chatRecoveryAttempts++;
                OpenChatForCurrentRoom();
                return true;
            }

            return false;
        }

        private bool AdvanceMatchRecovery()
        {
            if (_recoveryContext == null)
            {
                DiscoverRecoveryContext();
                return true;
            }

            if (_recoveryLobbyUntil.HasValue || _matchSubscriptionUntil.HasValue || _enteringRoomId.HasValue)
            {
                return false;
            }

            if (_lobbyClient == null || _lobbyBoundRoomId != _recoveryContext.RoomId
                || CurrentRoom?.MatchId != _recoveryContext.MatchId)
            {
                if (OpenLobbyChannel())
                {
                    int roomId = _recoveryContext.RoomId.Value;
                    _recoveryLobbyUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryReplyMilliseconds;
                    TrySend(
                        () => SendConnectionRequest(
                            _lobbyChannel,
                            () => _lobbyClient.EnterLobby(_sessionToken, roomId)),
                        "revincular la sala", CloseLobbyChannel);
                }
                return true;
            }

            if (!IsMatchSubscribed)
            {
                SubscribeToCurrentMatch();
                return true;
            }

            var view = CurrentMatchView;
            CancelMatchRecovery();
            MatchRecoveryReady?.Invoke(this, new ServerEventArgs<MatchView>(view));
            return false;
        }

        private void CompleteRecoveryLobby(OperationLobbyRoomResult result)
        {
            if (_heartbeatClock.ElapsedMilliseconds >= _recoveryLobbyUntil.Value)
            {
                CloseLobbyChannel();
                return;
            }

            _recoveryLobbyUntil = null;
            if (result?.IsSuccess == true && result.Room != null && _recoveryContext != null
                && result.Room.RoomId == _recoveryContext.RoomId
                && result.Room.MatchId == _recoveryContext.MatchId)
            {
                // Publish navigation only after the match subscription is confirmed.
                _lobbyBoundRoomId = result.Room.RoomId;
                ApplyRoomSnapshot(result.Room, publish: false);
            }
            else
            {
                CloseLobbyChannel();
                _recoveryContext = null;
            }
        }

        private static void OpenConnection(ICommunicationObject client)
        {
            var elapsed = Stopwatch.StartNew();
            try
            {
                client.Open(_recoveryOpenTimeout);
            }
            finally
            {
                _log.DebugFormat("Apertura de canal WCF: {0:F2} ms.", elapsed.Elapsed.TotalMilliseconds);
            }
        }

        private static void SendConnectionRequest(IClientChannel channel, Action send)
        {
            TimeSpan originalTimeout = channel.OperationTimeout;
            var elapsed = Stopwatch.StartNew();
            try
            {
                channel.OperationTimeout = _recoveryRequestTimeout;
                send();
            }
            finally
            {
                if (channel.State == CommunicationState.Opened)
                {
                    channel.OperationTimeout = originalTimeout;
                }
                _log.DebugFormat("Solicitud WCF de vinculación o contexto: {0:F2} ms.",
                    elapsed.Elapsed.TotalMilliseconds);
            }
        }
    }
}
