using System;
using System.Diagnostics;
using System.Windows.Threading;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Proyecta el plazo del servidor con tiempo transcurrido; nunca autoriza una partida.</summary>
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

        /// <summary>Crea la temporización en el dispatcher de la interfaz.</summary>
        public LobbyCountdown()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(CountdownRefreshMilliseconds)
            };
            _timer.Tick += OnCountdownTick;
        }

        /// <summary>Se dispara cuando cambia la presentación de la cuenta.</summary>
        public event EventHandler Changed;

        /// <summary>Solicita validar la generación actual al llegar a cero o vencer el único reintento.</summary>
        public event EventHandler<ServerEventArgs<long>> StartRequested;

        /// <summary>Obtiene si la foto del servidor mantiene una cuenta activa.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Obtiene la generación de cuenta vigente.</summary>
        public long CountdownId { get; private set; }

        /// <summary>Obtiene los segundos proyectados, redondeados hacia arriba.</summary>
        public int SecondsRemaining { get; private set; }

        /// <summary>Aplica una foto ya validada por versión; otra generación reinicia la cuenta.</summary>
        /// <param name="lobbyRoom">Foto vigente, o null al salir.</param>
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

        /// <summary>Programa como máximo un reintento de la misma generación con el intervalo del contrato.</summary>
        /// <param name="matchStartRequestResult">Respuesta autoritativa a la petición pendiente.</param>
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
