using System;
using System.Diagnostics;
using System.Windows.Threading;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Uses elapsed time to display the server's countdown. Only the server can authorize a match.</summary>
    internal sealed class LobbyCountdown
    {
        private const int CountdownRefreshMilliseconds = 100;

        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _elapsed = new Stopwatch();
        private TimeSpan _remainingAtReceipt;
        private int _roomId;
        private bool _hasRequestedMatchStart;
        private bool _hasRetriedStart;
        private bool _isRetryScheduled;
        private int _defaultSeconds = 0;

        /// <summary>Sets up timing on the UI dispatcher.</summary>
        public LobbyCountdown()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(CountdownRefreshMilliseconds)
            };
            _timer.Tick += OnCountdownTick;
        }

        /// <summary>Raised when the displayed countdown changes.</summary>
        public event EventHandler Changed;

        /// <summary>
        /// Requests a check of the current countdown generation at zero or when its one retry is due.
        /// </summary>
        public event EventHandler<ServerEventArgs<long>> StartRequested;

        /// <summary>Whether the server snapshot shows an active countdown.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Current countdown generation.</summary>
        public long CountdownId { get; private set; }

        /// <summary>Estimated seconds left, rounded up.</summary>
        public int SecondsRemaining { get; private set; }

        /// <summary>
        /// Applies a snapshot whose version has been checked. A new generation restarts the countdown.
        /// </summary>
        /// <param name="lobbyRoom">Current snapshot, or null when leaving.</param>
        public void ApplyRoom(LobbyRoom lobbyRoom)
        {
            bool hasRoomCountdown = lobbyRoom != null && lobbyRoom.CountdownEndsAtUtc.HasValue;
            bool hasActiveCountdown = hasRoomCountdown && !lobbyRoom.InGame;

            if (!hasActiveCountdown)
            {
                ResetTimerForMatchStart();
            }
            else
            {
                bool isSameGeneration = _roomId == lobbyRoom.RoomId && CountdownId == lobbyRoom.CountdownId;

                if (!isSameGeneration)
                {
                    ResetTimerForMatchStart();
                    _roomId = lobbyRoom.RoomId;
                    CountdownId = lobbyRoom.CountdownId;
                }

                IsVisible = true;
                if (!_hasRequestedMatchStart)
                {
                    StartInterval(lobbyRoom.CountdownEndsAtUtc.Value - lobbyRoom.ServerTimeUtc);
                }

                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Schedules at most one retry for the same generation using the interval in the contract.</summary>
        /// <param name="matchStartRequestResult">Server's decision on the pending request.</param>
        public void ApplyMatchStartResult(OperationRequestMatchStartResult matchStartRequestResult)
        {
            bool canRetryMatchStart = CanRetryMatchStart(matchStartRequestResult);

            if (canRetryMatchStart)
            {
                _hasRetriedStart = true;
                _isRetryScheduled = true;
                StartInterval(TimeSpan.FromSeconds(matchStartRequestResult.RetryAfterSeconds));
            }
        }

        public void ResetTimerForMatchStart()
        {
            _timer.Stop();
            _elapsed.Reset();
            IsVisible = false;
            SecondsRemaining = 0;
            _roomId = 0;
            CountdownId = 0;
            _hasRequestedMatchStart = false;
            _hasRetriedStart = false;
            _isRetryScheduled = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private bool CanRetryMatchStart(OperationRequestMatchStartResult matchStartRequestResult)
        {
            bool canRetry = false;

            if (matchStartRequestResult != null)
            {
                bool hasSuccessfullRoom = matchStartRequestResult.IsSuccess && matchStartRequestResult.Room != null;

                if (hasSuccessfullRoom)
                {
                    bool isCurrentResponse = IsCurrentCountdownResponse(matchStartRequestResult.Room);
                    bool hasRetryInterval = matchStartRequestResult.RetryAfterSeconds > _defaultSeconds;
                    bool hasRetryAvailable = _hasRequestedMatchStart && !_hasRetriedStart;
                    canRetry = isCurrentResponse && hasRetryInterval && hasRetryAvailable;
                }
            }

            return canRetry;
        }

        private bool IsCurrentCountdownResponse(LobbyRoom lobbyRoom)
        {
            bool isCurrentGeneration = lobbyRoom.RoomId == _roomId && lobbyRoom.CountdownId == CountdownId;
            bool isActiveResponse = !lobbyRoom.InGame && lobbyRoom.CountdownEndsAtUtc.HasValue;
            bool isCurrentResponse = isCurrentGeneration && isActiveResponse && IsVisible;

            return isCurrentResponse;
        }

        private void StartInterval(TimeSpan remaining)
        {
            _remainingAtReceipt = remaining;
            _elapsed.Restart();
            UpdateSecondsRemaining();
            _timer.Start();
        }

        private void UpdateSecondsRemaining()
        {
            double remainingSeconds = (_remainingAtReceipt - _elapsed.Elapsed).TotalSeconds;
            SecondsRemaining = (int)Math.Ceiling(Math.Max(0, remainingSeconds));
        }

        private void OnCountdownTick(object sender, EventArgs eventArguments)
        {
            UpdateSecondsRemaining();
            Changed?.Invoke(this, EventArgs.Empty);

            if (SecondsRemaining == _defaultSeconds)
            {
                _timer.Stop();
                bool shouldRequestStart = !_hasRequestedMatchStart || _isRetryScheduled;

                if (shouldRequestStart)
                {
                    _hasRequestedMatchStart = true;
                    _isRetryScheduled = false;
                    StartRequested?.Invoke(this, new ServerEventArgs<long>(CountdownId));
                }
            }
        }
    }
}
