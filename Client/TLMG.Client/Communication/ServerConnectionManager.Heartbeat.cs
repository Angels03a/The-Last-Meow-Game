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
        private const int HeartbeatTimerIntervalMilliseconds = 100;
        private const int HeartbeatSendSpacingDivisor = 6;
        private const int RecoveryStepSpacingDivisor = 3;
        private const int FriendsHeartbeatOffsetMilliseconds = 0;
        private const int LobbyHeartbeatOffsetMilliseconds = 750;
        private const int ChatHeartbeatOffsetMilliseconds = 1500;
        private const int MatchHeartbeatOffsetMilliseconds = 2250;
        private const int NoHeartbeatSequence = 0;
        private const long NoScheduledHeartbeat = 0;
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
                    Interval = TimeSpan.FromMilliseconds(HeartbeatTimerIntervalMilliseconds)
                };
                _heartbeatTimer.Tick += OnHeartbeatTick;
            }
            _heartbeatTimer.Start();
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Stop();
            _nextHeartbeatSendAt = NoScheduledHeartbeat;
        }

        private void OnHeartbeatTick(object sender, EventArgs args)
        {
            _moderationChannel.CheckTimeout();
            if (!HasSession)
            {
                _heartbeatTimer.Stop();
            }
            else if (AdvanceConnectionRecovery(_heartbeatClock.ElapsedMilliseconds))
            {
                long now = _heartbeatClock.ElapsedMilliseconds;
                _nextHeartbeatSendAt = now + HeartbeatIntervalMilliseconds / HeartbeatSendSpacingDivisor;
                _nextRecoveryStepAt = now + HeartbeatIntervalMilliseconds / RecoveryStepSpacingDivisor;
            }
            else
            {
                PulseNextActiveLink();
            }
        }

        private void PulseNextActiveLink()
        {
            bool wasPulsed = false;

            void CloseLobbyLink()
            {
                DisconnectIfCurrentChannelEnded(_lobbyChannel);
            }

            void CloseMatchLink()
            {
                CloseMatchChannelIfCurrent(_matchChannel);
            }

            if (_friendsClient != null)
            {
                wasPulsed = PulseLink(new HeartbeatLink(_friendsCallback, _friendsChannel,
                    FriendsHeartbeatOffsetMilliseconds, _friendsClient.FriendsHeartbeat, CloseFriendsChannel));
            }

            if (!wasPulsed && _lobbyClient != null)
            {
                wasPulsed = PulseLink(new HeartbeatLink(_lobbyCallback, _lobbyChannel,
                    LobbyHeartbeatOffsetMilliseconds, _lobbyClient.LobbyHeartbeat, CloseLobbyLink));
            }

            if (!wasPulsed && _chatClient != null)
            {
                wasPulsed = PulseLink(new HeartbeatLink(_chatCallback, _chatClient.InnerChannel,
                    ChatHeartbeatOffsetMilliseconds, _chatClient.ChatHeartbeat, CloseChatChannel));
            }

            if (!wasPulsed && _matchClient != null)
            {
                PulseLink(new HeartbeatLink(_matchCallback, _matchChannel,
                    MatchHeartbeatOffsetMilliseconds, _matchClient.MatchHeartbeat, CloseMatchLink));
            }
        }

        private bool PulseLink(HeartbeatLink link)
        {
            bool wasPulsed = false;

            if (IsLinkReadyForHeartbeat(link))
            {
                long now = _heartbeatClock.ElapsedMilliseconds;
                InitializeHeartbeatScheduleIfNeeded(link.Callback, link.OffsetMilliseconds, now);

                if (now - link.Callback.LastHeartbeatEchoAt >= HeartbeatSilenceMilliseconds)
                {
                    _log.WarnFormat("Heartbeat confirmation deadline exceeded for {0}; closing the channel.",
                        link.Send.Method.Name);
                    link.Channel.Abort();
                    link.Close();
                    wasPulsed = true;
                }
                else if (now >= link.Callback.HeartbeatDue.Value && now >= _nextHeartbeatSendAt)
                {
                    SendHeartbeatPacket(link, now);
                    wasPulsed = true;
                }
            }

            return wasPulsed;
        }

        private static bool IsLinkReadyForHeartbeat(HeartbeatLink link)
        {
            bool hasCallbackAndSender = link.Callback != null && link.Send != null;
            bool isChannelOpened = link.Channel != null && link.Channel.State == CommunicationState.Opened;
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

        private void SendHeartbeatPacket(HeartbeatLink link, long now)
        {
            ServerCallback callback = link.Callback;
            IClientChannel channel = link.Channel;
            _nextHeartbeatSendAt = now + HeartbeatIntervalMilliseconds / HeartbeatSendSpacingDivisor;
            callback.HeartbeatDue = now + HeartbeatIntervalMilliseconds;
            callback.PendingHeartbeat = ++callback.HeartbeatSequence;
            int confirmed = callback.ConfirmedHeartbeat;
            callback.ConfirmedHeartbeat = NoHeartbeatSequence;
            TimeSpan originalTimeout = channel.OperationTimeout;

            try
            {
                channel.OperationTimeout = _heartbeatOperationTimeout;
                link.Send(_sessionToken, callback.PendingHeartbeat, confirmed);
            }
            catch (CommunicationException comunnicationException)
            {
                _log.Warn(string.Format("Heartbeat communication failed for {0}; closing the channel.",
                    link.Send.Method.Name), comunnicationException);
                channel.Abort();
                link.Close();
            }
            catch (TimeoutException timeoutException)
            {
                HandleHeartbeatTimeout(link, timeoutException);
            }
            finally
            {
                RestoreOperationTimeoutIfOpened(channel, originalTimeout);
            }
        }

        private static void HandleHeartbeatTimeout(HeartbeatLink link, TimeoutException timeoutException)
        {
            if (link.Channel.State != CommunicationState.Opened)
            {
                _log.Warn(string.Format("Heartbeat timed out for {0}; closing the channel.",
                    link.Send.Method.Name), timeoutException);
                link.Channel.Abort();
                link.Close();
            }
            else
            {
                _log.Debug(string.Format("Heartbeat timed out for {0}; the channel remains open.",
                    link.Send.Method.Name), timeoutException);
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
                callback.PendingHeartbeat = NoHeartbeatSequence;
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
            bool isMatchingSequence = callback != null && sequence > NoHeartbeatSequence && callback.PendingHeartbeat == sequence;
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