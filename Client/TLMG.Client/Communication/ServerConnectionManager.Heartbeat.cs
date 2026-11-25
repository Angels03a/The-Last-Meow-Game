using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Windows.Threading;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager
    {
        private const int HeartbeatIntervalMilliseconds = 3000;
        private const int HeartbeatSilenceMilliseconds = 9000;
        private static readonly TimeSpan _heartbeatOperationTimeout = TimeSpan.FromMilliseconds(250);
        private readonly Stopwatch _heartbeatClock = Stopwatch.StartNew();
        private DispatcherTimer _heartbeatTimer;
        private long _nextHeartbeatSendAt;

        private void StartHeartbeat()
        {
            if (_heartbeatTimer == null)
            {
                _heartbeatTimer = new DispatcherTimer(DispatcherPriority.Background, _interfaceDispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                _heartbeatTimer.Tick += OnHeartbeatTick;
            }
            _heartbeatTimer.Start();
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Stop();
            _nextHeartbeatSendAt = 0;
        }

        private void OnHeartbeatTick(object sender, EventArgs args)
        {
            if (!HasSession)
            {
                _heartbeatTimer.Stop();
            }
            else if (AdvanceConnectionRecovery(_heartbeatClock.ElapsedMilliseconds))
            {
                long now = _heartbeatClock.ElapsedMilliseconds;
                _nextHeartbeatSendAt = now + HeartbeatIntervalMilliseconds / 6;
                _nextRecoveryStepAt = now + HeartbeatIntervalMilliseconds / 3;
            }
            else
            {
                PulseNextActiveLink();
            }
        }

        private void PulseNextActiveLink()
        {
            bool wasPulsed = PulseLink(
                _friendsCallback,
                _friendsChannel,
                0,
                (token, sequence, confirmed) => _friendsClient.FriendsHeartbeat(token, sequence, confirmed),
                CloseFriendsChannel);

            if (!wasPulsed)
            {
                wasPulsed = PulseLink(
                    _lobbyCallback,
                    _lobbyChannel,
                    750,
                    (token, sequence, confirmed) => _lobbyClient.LobbyHeartbeat(token, sequence, confirmed),
                    () => DisconnectIfCurrentChannelEnded(_lobbyChannel));
            }

            if (!wasPulsed)
            {
                wasPulsed = PulseLink(
                    _chatCallback,
                    _chatClient?.InnerChannel,
                    1500,
                    (token, sequence, confirmed) => _chatClient.ChatHeartbeat(token, sequence, confirmed),
                    CloseChatChannel);
            }

            if (!wasPulsed)
            {
                PulseLink(
                    _matchCallback,
                    _matchChannel,
                    2250,
                    (token, sequence, confirmed) => _matchClient.MatchHeartbeat(token, sequence, confirmed),
                    () => CloseMatchChannelIfCurrent(_matchChannel));
            }
        }

        private bool PulseLink(
            ServerCallback callback,
            IClientChannel channel,
            int offset,
            Action<string, int, int> send,
            Action close)
        {
            bool wasPulsed = false;

            if (IsLinkReadyForHeartbeat(callback, channel, send))
            {
                long now = _heartbeatClock.ElapsedMilliseconds;
                InitializeHeartbeatScheduleIfNeeded(callback, offset, now);

                if (now - callback.LastHeartbeatEchoAt >= HeartbeatSilenceMilliseconds)
                {
                    channel.Abort();
                    close();
                    wasPulsed = true;
                }
                else if (now >= callback.HeartbeatDue.Value && now >= _nextHeartbeatSendAt)
                {
                    SendHeartbeatPacket(callback, channel, send, close, now);
                    wasPulsed = true;
                }
            }

            return wasPulsed;
        }

        private static bool IsLinkReadyForHeartbeat(
            ServerCallback callback,
            IClientChannel channel,
            Action<string, int, int> send)
        {
            bool hasCallbackAndSender = callback != null && send != null;
            bool isChannelOpened = channel != null && channel.State == CommunicationState.Opened;
            bool isReady = hasCallbackAndSender && isChannelOpened;

            return isReady;
        }

        private static void InitializeHeartbeatScheduleIfNeeded(ServerCallback callback, int offset, long now)
        {
            if (!callback.HeartbeatDue.HasValue)
            {
                callback.HeartbeatDue = now + offset;
                callback.LastHeartbeatEchoAt = now;
            }
        }

        private void SendHeartbeatPacket(
            ServerCallback callback,
            IClientChannel channel,
            Action<string, int, int> send,
            Action close,
            long now)
        {
            _nextHeartbeatSendAt = now + HeartbeatIntervalMilliseconds / 6;
            callback.HeartbeatDue = now + HeartbeatIntervalMilliseconds;
            callback.PendingHeartbeat = ++callback.HeartbeatSequence;
            int confirmed = callback.ConfirmedHeartbeat;
            callback.ConfirmedHeartbeat = 0;
            TimeSpan originalTimeout = channel.OperationTimeout;

            try
            {
                channel.OperationTimeout = _heartbeatOperationTimeout;
                send(_sessionToken, callback.PendingHeartbeat, confirmed);
            }
            catch (CommunicationException)
            {
                channel.Abort();
                close();
            }
            catch (TimeoutException)
            {
                HandleHeartbeatTimeout(channel, close);
            }
            finally
            {
                RestoreOperationTimeoutIfOpened(channel, originalTimeout);
            }
        }

        private static void HandleHeartbeatTimeout(IClientChannel channel, Action close)
        {
            if (channel.State != CommunicationState.Opened)
            {
                channel.Abort();
                close();
            }
        }

        private static void RestoreOperationTimeoutIfOpened(IClientChannel channel, TimeSpan originalTimeout)
        {
            if (channel.State == CommunicationState.Opened)
            {
                channel.OperationTimeout = originalTimeout;
            }
        }

        internal void NotifyHeartbeatEcho(ServerCallback callback, int sequence, string errorKey)
        {
            long now = _heartbeatClock.ElapsedMilliseconds;

            if (IsExpectedHeartbeatEcho(callback, sequence, now))
            {
                callback.PendingHeartbeat = 0;
                if (errorKey == InvalidSessionErrorKey)
                {
                    DisconnectAndNotifyConnectionLost();
                }
                else if (string.IsNullOrEmpty(errorKey))
                {
                    callback.ConfirmedHeartbeat = sequence;
                    callback.LastHeartbeatEchoAt = now;
                }
            }
        }

        private bool IsExpectedHeartbeatEcho(ServerCallback callback, int sequence, long now)
        {
            bool isMatchingSequence = callback != null && sequence > 0 && callback.PendingHeartbeat == sequence;
            bool isWithinDeadline = callback != null && callback.HeartbeatDue.HasValue
                && now < callback.HeartbeatDue.Value;
            bool isExpected = isMatchingSequence && isWithinDeadline && IsKnownHeartbeatCallback(callback);

            return isExpected;
        }

        private bool IsKnownHeartbeatCallback(ServerCallback callback)
        {
            bool isSocialOrLobbyCallback = ReferenceEquals(callback, _friendsCallback)
                || ReferenceEquals(callback, _lobbyCallback);
            bool isSessionActivityCallback = ReferenceEquals(callback, _chatCallback)
                || ReferenceEquals(callback, _matchCallback);
            bool isKnownCallback = isSocialOrLobbyCallback || isSessionActivityCallback;

            return isKnownCallback;
        }
    }
}
