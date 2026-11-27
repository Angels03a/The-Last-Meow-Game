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

        public bool IsMatchRecoveryPending { get { return _recoveryUntil.HasValue; } }
        public bool IsMatchRecoveryExhausted { get; private set; }

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
            if (HasSession && IsMatchRecoveryExhausted)
            {
                IsMatchRecoveryExhausted = false;
                RequestRecovery();
            }
        }

        private void RequestRecovery()
        {
            if (HasSession && !_recoveryUntil.HasValue && !IsMatchRecoveryExhausted)
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

        private void DiscoverRecoveryContext(bool notifyMatchContextEnded = true)
        {
            OperationRecoveryContextResult context = GetRecoveryContext();
            if (!context.IsSuccess && context.ErrorKey == InvalidSessionErrorKey)
            {
                DisconnectAndNotifyConnectionLost();
            }
            else if (context.IsSuccess)
            {
                if (!context.RoomId.HasValue || !context.MatchId.HasValue)
                {
                    EndRecoveryContext(notifyMatchContextEnded);
                }
                else
                {
                    _recoveryContext = context;
                }
            }
        }

        private void EndRecoveryContext(bool notifyMatchContextEnded = true)
        {
            if (CurrentRoom?.MatchId.HasValue == true)
            {
                ClearCurrentRoom();
            }
            CancelMatchRecovery();
            if (notifyMatchContextEnded)
            {
                MatchContextEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool AdvanceConnectionRecovery(long now)
        {
            bool hasAdvanced = false;

            CloseExpiredSubscriptionChannels(now);

            if (_recoveryUntil.HasValue && now >= _recoveryUntil.Value)
            {
                hasAdvanced = HandleExpiredRecoveryWindow();
            }
            else if (now >= _nextHeartbeatSendAt && now >= _nextRecoveryStepAt)
            {
                hasAdvanced = AdvanceScheduledRecoveryStep();
            }

            return hasAdvanced;
        }

        private void CloseExpiredSubscriptionChannels(long now)
        {
            CloseExpiredPrimaryRecoveryChannels(now);
            CloseExpiredSecondaryChannels(now);

            if (_roomEntryUntil.HasValue && now >= _roomEntryUntil.Value)
            {
                CloseLobbyChannel();
            }

            if (_invitationAcceptanceUntil.HasValue && now >= _invitationAcceptanceUntil.Value)
            {
                CloseFriendsChannel();
            }
        }

        private void CloseExpiredPrimaryRecoveryChannels(long now)
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
        }

        private void CloseExpiredSecondaryChannels(long now)
        {
            if (_friendsSubscriptionUntil.HasValue && now >= _friendsSubscriptionUntil.Value)
            {
                CloseFriendsChannel();
            }
            if (_chatSubscriptionUntil.HasValue && now >= _chatSubscriptionUntil.Value)
            {
                CloseChatChannel();
            }
        }

        private bool HandleExpiredRecoveryWindow()
        {
            bool hasAdvanced;

            if (IsReadyToFinalizeExpiredRecovery())
            {
                hasAdvanced = AdvanceMatchRecovery();
            }
            else
            {
                ResetPendingRecoveryChannels();
                DiscoverRecoveryContext();
                MarkRecoveryExhaustedIfStillPending();
                hasAdvanced = true;
            }

            return hasAdvanced;
        }

        private bool IsReadyToFinalizeExpiredRecovery()
        {
            bool isIdleWithContext = _recoveryContext != null
                && !_recoveryLobbyUntil.HasValue
                && !_matchSubscriptionUntil.HasValue
                && !_enteringRoomId.HasValue;
            bool isReady = isIdleWithContext && IsMatchLinkBoundToRecoveryContext();

            return isReady;
        }

        private bool IsMatchLinkBoundToRecoveryContext()
        {
            bool isLobbyBound = _lobbyClient != null
                && _lobbyBoundRoomId == _recoveryContext.RoomId
                && CurrentRoom?.MatchId == _recoveryContext.MatchId;
            bool isMatchBound = IsMatchSubscribed
                && CurrentMatchView?.MatchId == _recoveryContext.MatchId;
            bool isBound = isLobbyBound && isMatchBound;

            return isBound;
        }

        private void ResetPendingRecoveryChannels()
        {
            if (_recoveryLobbyUntil.HasValue)
            {
                CloseLobbyChannel();
            }
            if (_matchSubscriptionUntil.HasValue)
            {
                CloseMatchChannel();
            }
        }

        private void MarkRecoveryExhaustedIfStillPending()
        {
            if (_recoveryUntil.HasValue)
            {
                _recoveryUntil = null;
                IsMatchRecoveryExhausted = true;
                RecoveryStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool AdvanceScheduledRecoveryStep()
        {
            bool hasAdvanced = false;

            if (_recoveryUntil.HasValue)
            {
                hasAdvanced = AdvanceMatchRecovery();
            }
            else if (_friendsClient == null && _friendsRecoveryAttempts < SecondaryRecoveryAttemptLimit)
            {
                _friendsRecoveryAttempts++;
                TryOpenFriendsChannel();
                hasAdvanced = true;
            }
            else if (CanRetryChatRecovery())
            {
                _chatRecoveryAttempts++;
                OpenChatForCurrentRoom();
                hasAdvanced = true;
            }

            return hasAdvanced;
        }

        private bool CanRetryChatRecovery()
        {
            bool hasRoomWithoutChat = CurrentRoom != null && _chatClient == null;
            bool hasRemainingAttempts = _chatRecoveryAttempts < SecondaryRecoveryAttemptLimit;
            bool canRetry = HasRegisteredSession && hasRoomWithoutChat && hasRemainingAttempts;

            return canRetry;
        }

        private bool AdvanceMatchRecovery()
        {
            bool hasAdvanced = false;

            if (_recoveryContext == null)
            {
                DiscoverRecoveryContext();
                hasAdvanced = true;
            }
            else if (!IsMatchRecoveryStepInFlight())
            {
                if (NeedsLobbyRebindForRecovery())
                {
                    RebindLobbyForRecovery();
                    hasAdvanced = true;
                }
                else if (!IsMatchSubscribed)
                {
                    SubscribeToCurrentMatch();
                    hasAdvanced = true;
                }
                else
                {
                    CompleteMatchRecovery();
                }
            }

            return hasAdvanced;
        }

        private bool IsMatchRecoveryStepInFlight()
        {
            bool hasPendingChannelTimeout = _recoveryLobbyUntil.HasValue || _matchSubscriptionUntil.HasValue;
            bool isInFlight = hasPendingChannelTimeout || _enteringRoomId.HasValue;

            return isInFlight;
        }

        private bool NeedsLobbyRebindForRecovery()
        {
            bool isLobbyMissing = _lobbyClient == null;
            bool isRoomMismatched = _lobbyBoundRoomId != _recoveryContext.RoomId
                || CurrentRoom?.MatchId != _recoveryContext.MatchId;
            bool needsRebind = isLobbyMissing || isRoomMismatched;

            return needsRebind;
        }

        private void RebindLobbyForRecovery()
        {
            if (OpenLobbyChannel())
            {
                int roomId = _recoveryContext.RoomId.Value;
                _recoveryLobbyUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryReplyMilliseconds;
                void EnterRoom()
                {
                    _lobbyClient.EnterLobby(_sessionToken, roomId);
                }

                void SendRebind()
                {
                    SendConnectionRequest(_lobbyChannel, EnterRoom);
                }

                TrySend(SendRebind, "revincular la sala", CloseLobbyChannel);
            }
        }

        private void CompleteMatchRecovery()
        {
            MatchView view = CurrentMatchView;
            CancelMatchRecovery();
            MatchRecoveryReady?.Invoke(this, new ServerEventArgs<MatchView>(view));
        }

        private void CompleteRecoveryLobby(OperationLobbyRoomResult result)
        {
            if (_heartbeatClock.ElapsedMilliseconds >= _recoveryLobbyUntil.Value)
            {
                CloseLobbyChannel();
            }
            else
            {
                _recoveryLobbyUntil = null;
                if (IsMatchingRecoveryLobbyResult(result))
                {
                    _lobbyBoundRoomId = result.Room.RoomId;
                    ApplyRoomSnapshot(result.Room, publish: false);
                }
                else
                {
                    CloseLobbyChannel();
                    _recoveryContext = null;
                }
            }
        }

        private bool IsMatchingRecoveryLobbyResult(OperationLobbyRoomResult result)
        {
            bool isMatchingResult = false;
            bool hasValidRoomResult = result != null && result.IsSuccess && result.Room != null;

            if (hasValidRoomResult && _recoveryContext != null)
            {
                bool isSameRoom = result.Room.RoomId == _recoveryContext.RoomId;
                bool isSameMatch = result.Room.MatchId == _recoveryContext.MatchId;
                isMatchingResult = isSameRoom && isSameMatch;
            }

            return isMatchingResult;
        }

        internal static void OpenConnection(ICommunicationObject client)
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            client.Open(_recoveryOpenTimeout);
            _log.DebugFormat("Apertura de canal WCF: {0:F2} ms.", elapsed.Elapsed.TotalMilliseconds);
        }

        private static void SendConnectionRequest(IClientChannel channel, Action send)
        {
            TimeSpan originalTimeout = channel.OperationTimeout;
            Stopwatch elapsed = Stopwatch.StartNew();
            channel.OperationTimeout = _recoveryRequestTimeout;
            send();
            RestoreOperationTimeoutIfOpened(channel, originalTimeout);
            _log.DebugFormat(
                "Solicitud WCF de vinculación o contexto: {0:F2} ms.",
                elapsed.Elapsed.TotalMilliseconds);
        }
    }
}