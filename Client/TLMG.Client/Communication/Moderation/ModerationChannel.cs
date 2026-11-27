using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Windows.Threading;

using log4net;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed class ModerationChannel
    {
        private const int ConfirmationTimeoutMilliseconds = 10000;
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private static readonly TimeSpan _sendTimeout = TimeSpan.FromSeconds(5);
        private static readonly ILog _log = LogManager.GetLogger(typeof(ModerationChannel));
        private readonly Dispatcher _dispatcher;
        private readonly Stopwatch _clock;
        private ModerationManagerClient _client;
        private ModerationCallback _callback;
        private ModerationOperation _pendingOperation;
        private long? _confirmationDeadline;

        internal ModerationChannel(Dispatcher dispatcher, Stopwatch clock)
        {
            _dispatcher = dispatcher;
            _clock = clock;
        }

        internal event Action<ModerationOperation, OperationResult> Completed;

        internal bool Send(ModerationOperation operation, string sessionToken,
            Action<ModerationManagerClient, string> send)
        {
            bool wasSent = false;

            if (_pendingOperation == ModerationOperation.None && operation != ModerationOperation.None
                && !string.IsNullOrWhiteSpace(sessionToken) && send != null)
            {
                try
                {
                    _callback = new ModerationCallback(this);
                    _client = new ModerationManagerClient(new InstanceContext(_callback));
                    _pendingOperation = operation;
                    ServerConnectionManager.OpenConnection(_client);
                    _client.InnerChannel.OperationTimeout = _sendTimeout;
                    send(_client, sessionToken);
                    _confirmationDeadline = _clock.ElapsedMilliseconds + ConfirmationTimeoutMilliseconds;
                    wasSent = true;
                }
                catch (CommunicationException exception)
                {
                    _log.Error("Could not send the moderation operation.", exception);
                }
                catch (TimeoutException exception)
                {
                    _log.Error("The moderation operation timed out.", exception);
                }
                catch (ObjectDisposedException exception)
                {
                    _log.Error("The moderation channel was disposed.", exception);
                }
                finally
                {
                    if (!wasSent)
                    {
                        Close();
                    }
                }
            }

            return wasSent;
        }

        internal void QueueCompletion(ModerationCallback callback, ModerationOperation operation,
            OperationResult result)
        {
            _dispatcher.BeginInvoke(new Action<ModerationCallback, ModerationOperation, OperationResult>(Complete),
                callback, operation, result);
        }

        private void Complete(ModerationCallback callback, ModerationOperation operation, OperationResult result)
        {
            bool isCurrent = callback != null && ReferenceEquals(callback, _callback)
                && operation == _pendingOperation && operation != ModerationOperation.None;

            if (isCurrent)
            {
                OperationResult normalizedResult = NormalizeCompletion(operation, result);
                Close();
                Completed?.Invoke(operation, normalizedResult);
            }
        }

        private static OperationResult NormalizeCompletion(ModerationOperation operation, OperationResult result)
        {
            OperationResult normalizedResult = ServerConnectionManager.NormalizeOperationResult(result);

            if (operation == ModerationOperation.GetBlockedPlayers)
            {
                BlockedPlayerList blockedPlayers = normalizedResult as BlockedPlayerList;

                if (blockedPlayers == null || (blockedPlayers.IsSuccess && blockedPlayers.Players == null))
                {
                    string errorKey = normalizedResult.ErrorKey;
                    if (normalizedResult.IsSuccess)
                    {
                        _log.Warn("The server returned an invalid blocked-player list.");
                        errorKey = UnexpectedErrorKey;
                    }

                    normalizedResult = new BlockedPlayerList { IsSuccess = false, ErrorKey = errorKey };
                }
            }

            return normalizedResult;
        }

        internal void CheckTimeout()
        {
            if (_client != null && _confirmationDeadline.HasValue
                && (_clock.ElapsedMilliseconds >= _confirmationDeadline.Value
                    || _client.State == CommunicationState.Faulted
                    || _client.State == CommunicationState.Closed))
            {
                _log.Warn("The moderation operation was not confirmed by the server.");
                Complete(_callback, _pendingOperation,
                    new OperationResult { IsSuccess = false, ErrorKey = UnexpectedErrorKey });
            }
        }

        internal void Close()
        {
            ModerationManagerClient client = _client;
            _client = null;
            _callback = null;
            _pendingOperation = ModerationOperation.None;
            _confirmationDeadline = null;
            client?.Abort();
        }
    }
}