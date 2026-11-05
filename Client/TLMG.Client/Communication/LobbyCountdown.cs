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
        private bool _hasRequestedStart;
        private bool _hasRetriedStart;
        private bool _isRetryScheduled;

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
        /// <param name="room">Foto vigente, o null al salir.</param>
        public void ApplyRoom(LobbyRoom room)
        {
            bool hasRoomCountdown = room != null && room.CountdownEndsAtUtc.HasValue;
            bool hasActiveCountdown = hasRoomCountdown && !room.InGame;

            if (!hasActiveCountdown)
            {
                Reset();
            }
            else
            {
                bool isSameGeneration = _roomId == room.RoomId && CountdownId == room.CountdownId;

                if (!isSameGeneration)
                {
                    Reset();
                    _roomId = room.RoomId;
                    CountdownId = room.CountdownId;
                }

                IsVisible = true;
                if (!_hasRequestedStart)
                {
                    StartInterval(room.CountdownEndsAtUtc.Value - room.ServerTimeUtc);
                }

                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Programa como máximo un reintento de la misma generación con el intervalo del contrato.</summary>
        /// <param name="result">Respuesta autoritativa a la petición pendiente.</param>
        public void ApplyStartResult(OperationRequestMatchStartResult result)
        {
            bool canRetry = CanRetryStart(result);

            if (canRetry)
            {
                _hasRetriedStart = true;
                _isRetryScheduled = true;
                StartInterval(TimeSpan.FromSeconds(result.RetryAfterSeconds));
            }
        }

        /// <summary>Detiene la temporización al salir o perder conexión.</summary>
        public void Reset()
        {
            _timer.Stop();
            _elapsed.Reset();
            IsVisible = false;
            SecondsRemaining = 0;
            _roomId = 0;
            CountdownId = 0;
            _hasRequestedStart = false;
            _hasRetriedStart = false;
            _isRetryScheduled = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private bool CanRetryStart(OperationRequestMatchStartResult result)
        {
            bool canRetry = false;

            if (result != null)
            {
                bool hasSuccessfulRoom = result.IsSuccess && result.Room != null;

                if (hasSuccessfulRoom)
                {
                    bool isCurrentResponse = IsCurrentCountdownResponse(result.Room);
                    bool hasRetryInterval = result.RetryAfterSeconds > 0;
                    bool hasRetryAvailable = _hasRequestedStart && !_hasRetriedStart;
                    canRetry = isCurrentResponse && hasRetryInterval && hasRetryAvailable;
                }
            }

            return canRetry;
        }

        private bool IsCurrentCountdownResponse(LobbyRoom room)
        {
            bool isCurrentGeneration = room.RoomId == _roomId && room.CountdownId == CountdownId;
            bool isActiveResponse = !room.InGame && room.CountdownEndsAtUtc.HasValue;
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

            if (SecondsRemaining == 0)
            {
                _timer.Stop();
                bool shouldRequestStart = !_hasRequestedStart || _isRetryScheduled;

                if (shouldRequestStart)
                {
                    _hasRequestedStart = true;
                    _isRetryScheduled = false;
                    StartRequested?.Invoke(this, new ServerEventArgs<long>(CountdownId));
                }
            }
        }
    }
}
