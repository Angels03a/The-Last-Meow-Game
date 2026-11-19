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
            if (!HasRegisteredSession)
            {
                _heartbeatTimer.Stop();
                return;
            }

            // At most one synchronous send per Dispatcher tick.
            if (PulseLink(_friendsCallback, _friendsChannel, 0, (token, sequence, confirmed) => _friendsClient.FriendsHeartbeat(token, sequence, confirmed),
                CloseFriendsChannel)) return;
            if (PulseLink(_lobbyCallback, _lobbyChannel, 750, (token, sequence, confirmed) => _lobbyClient.LobbyHeartbeat(token, sequence, confirmed),
                () => DisconnectIfCurrentChannelEnded(_lobbyChannel))) return;
            if (PulseLink(_chatCallback, _chatClient?.InnerChannel, 1500, (token, sequence, confirmed) => _chatClient.ChatHeartbeat(token, sequence, confirmed),
                CloseChatChannel)) return;
            PulseLink(_matchCallback, _matchChannel, 2250, (token, sequence, confirmed) => _matchClient.MatchHeartbeat(token, sequence, confirmed),
                () => CloseMatchChannelIfCurrent(_matchChannel));
        }

        private bool PulseLink(ServerCallback callback, IClientChannel channel, int offset,
            Action<string, int, int> send, Action close)
        {
            if (callback == null || channel?.State != CommunicationState.Opened || send == null)
            {
                return false;
            }

            long now = _heartbeatClock.ElapsedMilliseconds;
            if (!callback.HeartbeatDue.HasValue)
            {
                callback.HeartbeatDue = now + offset;
                callback.LastHeartbeatEchoAt = now;
            }

            if (now - callback.LastHeartbeatEchoAt >= HeartbeatSilenceMilliseconds)
            {
                channel.Abort();
                close();
                return true;
            }

            if (now < callback.HeartbeatDue.Value || now < _nextHeartbeatSendAt)
            {
                return false;
            }

            // A stalled Dispatcher can make several links due. Keep their sends separated when it resumes.
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
                if (channel.State != CommunicationState.Opened)
                {
                    channel.Abort();
                    close();
                }
            }
            finally
            {
                if (channel.State == CommunicationState.Opened)
                {
                    channel.OperationTimeout = originalTimeout;
                }
            }

            return true;
        }

        internal void NotifyHeartbeatEcho(ServerCallback callback, int sequence, string errorKey)
        {
            long now = _heartbeatClock.ElapsedMilliseconds;
            if (callback.PendingHeartbeat != sequence || sequence <= 0
                || !callback.HeartbeatDue.HasValue || now >= callback.HeartbeatDue.Value
                || (!ReferenceEquals(callback, _friendsCallback) && !ReferenceEquals(callback, _lobbyCallback)
                    && !ReferenceEquals(callback, _chatCallback) && !ReferenceEquals(callback, _matchCallback)))
            {
                return;
            }

            callback.PendingHeartbeat = 0;
            if (errorKey == "Common_InvalidSessionError")
            {
                DisconnectAndNotifyConnectionLost();
            }
            else if (errorKey == null)
            {
                callback.ConfirmedHeartbeat = sequence;
                callback.LastHeartbeatEchoAt = now;
            }
        }
    }
}
