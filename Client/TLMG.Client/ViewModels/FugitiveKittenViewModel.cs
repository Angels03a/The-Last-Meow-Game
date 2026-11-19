using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>Presenta el minijuego confirmado por la partida actual.</summary>
    public sealed class FugitiveKittenViewModel : ViewModelBase
    {
        private const int TickMilliseconds = 100;
        private const int ResultMilliseconds = 2800;
        private const int DeadlineRetryMilliseconds = 1000;
        private readonly INavigationManager _navigation;
        private readonly IMatchConnection _match;
        private readonly ILobbyConnection _lobby;
        private readonly Guid _matchId;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();
        private DateTime _serverAtReceipt;
        private TimeSpan _elapsedAtReceipt;
        private readonly RelayCommand _catchCommand;
        private FugitiveKittenView _kitten;
        private KittenSceneState _sceneState;
        private KittenColor _kittenColor = KittenColor.Orange;
        private int _remainingSeconds;
        private string _winnerNickname = string.Empty;
        private string _origin;
        private string _destination;
        private double _progress;
        private bool _isCatchAvailable;
        private bool _isConnected;
        private bool _isWaiting;
        private bool _alternatePose;
        private string _glowPoint;
        private bool _catchPending;
        private bool _deadlinePending;
        private bool _activated;
        private bool _navigated;
        private long _nextDeadlineCheck;
        private long? _resultAt;
        private string _recoveryMessage = string.Empty;

        public FugitiveKittenViewModel(INavigationManager navigation, IMatchConnection match, ILobbyConnection lobby,
            Guid matchId)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _match = match ?? throw new ArgumentNullException(nameof(match));
            _lobby = lobby ?? throw new ArgumentNullException(nameof(lobby));
            _matchId = matchId;
            _catchCommand = new RelayCommand(ExecuteCatchKitten, CanCatchKitten);
            RetryRecoveryCommand = new RelayCommand(_match.RetryMatchRecovery);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TickMilliseconds) };
            _timer.Tick += OnTimerTick;
        }

        public ICommand CatchKittenCommand => _catchCommand;
        public ICommand RetryRecoveryCommand { get; }
        public bool CanRetryRecovery => _match.IsMatchRecoveryExhausted;
        public string RecoveryMessage
        {
            get => _recoveryMessage;
            private set => SetProperty(ref _recoveryMessage, value);
        }
        public bool IsCatchAvailable
        {
            get => _isCatchAvailable;
            private set
            {
                if (SetProperty(ref _isCatchAvailable, value))
                {
                    _catchCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    _catchCommand.NotifyCanExecuteChanged();
                }
            }
        }
        public bool IsWaiting { get => _isWaiting; private set => SetProperty(ref _isWaiting, value); }
        public bool AlternatePose { get => _alternatePose; private set => SetProperty(ref _alternatePose, value); }
        public string GlowPoint { get => _glowPoint; private set => SetProperty(ref _glowPoint, value); }
        public KittenSceneState SceneState { get => _sceneState; private set => SetProperty(ref _sceneState, value); }
        public KittenColor KittenColor { get => _kittenColor; private set => SetProperty(ref _kittenColor, value); }
        public int RemainingSeconds
        {
            get => _remainingSeconds;
            private set => SetProperty(ref _remainingSeconds, value);
        }
        public string WinnerNickname { get => _winnerNickname; private set => SetProperty(ref _winnerNickname, value); }
        public string Origin { get => _origin; private set => SetProperty(ref _origin, value); }
        public string Destination { get => _destination; private set => SetProperty(ref _destination, value); }
        public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }

        public void Activate()
        {
            if (!_activated)
            {
                _activated = true;
                _catchPending = false;
                _deadlinePending = false;
                _match.MatchViewChanged += OnMatchViewChanged;
                _match.MatchAvailabilityChanged += OnAvailabilityChanged;
                _match.RecoveryStateChanged += UpdateRecoveryDisplay;
                _match.FugitiveKittenCatchResultReceived += OnCatchResult;
                _match.FugitiveKittenDeadlineResultReceived += OnDeadlineResult;
                IsConnected = _match.IsMatchSubscribed;
                if (IsConnected)
                {
                    ApplyView(_match.CurrentMatchView);
                }
                UpdateRecoveryDisplay(this, EventArgs.Empty);
                if (!_match.IsMatchSubscribed && !_match.SubscribeToCurrentMatch()) IsConnected = false;
                _timer.Start();
            }
        }

        public void Deactivate()
        {
            if (_activated)
            {
                _activated = false;
                _timer.Stop();
                _match.MatchViewChanged -= OnMatchViewChanged;
                _match.MatchAvailabilityChanged -= OnAvailabilityChanged;
                _match.RecoveryStateChanged -= UpdateRecoveryDisplay;
                _match.FugitiveKittenCatchResultReceived -= OnCatchResult;
                _match.FugitiveKittenDeadlineResultReceived -= OnDeadlineResult;
            }
        }

        protected override void OnDisposed()
        {
            Deactivate();
            _timer.Tick -= OnTimerTick;
            base.OnDisposed();
        }

        private void OnMatchViewChanged(object sender, ServerEventArgs<MatchView> args) { ApplyView(args.Payload); }
        private void OnAvailabilityChanged(object sender, EventArgs args)
        {
            IsConnected = _match.IsMatchSubscribed;
            if (!IsConnected)
            {
                _catchPending = false;
                _deadlinePending = false;
                IsCatchAvailable = false;
            }
            UpdateRecoveryDisplay(this, EventArgs.Empty);
        }
        private void OnCatchResult(object sender, ServerEventArgs<OperationResult> args)
        {
            _catchPending = false;
            _catchCommand.NotifyCanExecuteChanged();
        }
        private void OnDeadlineResult(object sender, ServerEventArgs<OperationResult> args)
        {
            _deadlinePending = false;
        }
        private void ApplyView(MatchView view)
        {
            if (view == null || view.MatchId != _matchId || _navigated) return;
            if (view.Phase == MatchRoundPhase.MatchFinished)
            {
                _navigated = true;
                _navigation.NavigateToWithoutHistory(ScreenId.GameOver, view);
                return;
            }
            if (view.FugitiveKitten == null || _resultAt.HasValue) return;
            if (!IsConnected && _match.IsMatchRecoveryPending && view.FugitiveKitten.Result != FugitiveKittenResult.InProgress)
            {
                return;
            }
            _kitten = view.FugitiveKitten;
            _serverAtReceipt = _kitten.ServerTimeUtc.Add(_match.CurrentMatchViewAge);
            _elapsedAtReceipt = _elapsed.Elapsed;
            if (Enum.TryParse(_kitten.Color, out KittenColor color)) KittenColor = color;
            if (_kitten.Result != FugitiveKittenResult.InProgress)
            {
                _resultAt = _elapsed.ElapsedMilliseconds;
                WinnerNickname = _kitten.WinnerNickname ?? string.Empty;
                if (_kitten.Result == FugitiveKittenResult.Escaped)
                {
                    SceneState = KittenSceneState.Escaped;
                }
                else if (_kitten.WinnerUserId == _lobby.PlayerUserId)
                {
                    SceneState = KittenSceneState.Caught;
                }
                else
                {
                    SceneState = KittenSceneState.CaughtByOther;
                }
                IsCatchAvailable = false;
                IsWaiting = false;
                GlowPoint = null;
            }
            UpdatePresentation();
        }

        private void OnTimerTick(object sender, EventArgs args)
        {
            UpdateRecoveryDisplay(this, EventArgs.Empty);
            if (IsConnected && _kitten?.PausedAtUtc == null)
            {
                AlternatePose = !AlternatePose;
            }
            UpdatePresentation();
        }
        private void UpdateRecoveryDisplay(object sender, EventArgs args)
        {
            RecoveryMessage = MatchRecoveryText.Describe(_match);
            OnPropertyChanged(nameof(CanRetryRecovery));
        }

        private void UpdatePresentation()
        {
            if (_kitten == null || !IsConnected) return;
            if (_resultAt.HasValue)
            {
                if (!_navigated && _elapsed.ElapsedMilliseconds - _resultAt.Value >= ResultMilliseconds)
                {
                    _navigated = true;
                    _navigation.NavigateToWithoutHistory(ScreenId.Gameplay);
                }
                return;
            }
            DateTime estimatedServerUtc = _kitten.PausedAtUtc
                ?? _serverAtReceipt.Add(_elapsed.Elapsed - _elapsedAtReceipt);
            double milliseconds = (estimatedServerUtc - _kitten.StartedAtUtc).TotalMilliseconds;
            RemainingSeconds = Math.Max(0, (int)Math.Ceiling((_kitten.ExpiresAtUtc - estimatedServerUtc).TotalSeconds));
            KittenSegmentView segment = _kitten.Segments?.FirstOrDefault(item =>
                milliseconds >= item.StartMilliseconds && milliseconds < item.EndMilliseconds);
            if (segment != null)
            {
                Origin = segment.Origin;
                Destination = segment.Destination;
                Progress = Math.Max(0, Math.Min(1, (milliseconds - segment.StartMilliseconds)
                    / (segment.EndMilliseconds - segment.StartMilliseconds)));
                if (segment.Kind == "Run") SceneState = KittenSceneState.Running;
                else if (segment.Kind == "Start") SceneState = KittenSceneState.Intro;
                else if (segment.Kind == "Escape") SceneState = KittenSceneState.Leaving;
                else SceneState = KittenSceneState.Hidden;
                IsCatchAvailable = segment.IsCatchable && !_kitten.PausedAtUtc.HasValue;
                GlowPoint = !segment.IsCatchable && (segment.Kind == "Hide" || segment.Kind == "Tunnel")
                    && segment.EndMilliseconds - milliseconds <= 300 ? segment.Destination : null;
            }
            else
            {
                IsCatchAvailable = false;
                GlowPoint = null;
            }
            IsWaiting = milliseconds >= (_kitten.ExpiresAtUtc - _kitten.StartedAtUtc).TotalMilliseconds;
            if (_deadlinePending && _elapsed.ElapsedMilliseconds >= _nextDeadlineCheck)
            {
                _deadlinePending = false;
            }
            if ((_kitten.PausedAtUtc.HasValue || IsWaiting)
                && IsConnected && !_deadlinePending && _elapsed.ElapsedMilliseconds >= _nextDeadlineCheck)
            {
                _deadlinePending = _match.CheckFugitiveKittenDeadline();
                _nextDeadlineCheck = _elapsed.ElapsedMilliseconds + DeadlineRetryMilliseconds;
            }
        }
        private bool CanCatchKitten() => IsConnected && IsCatchAvailable && !_catchPending && !_resultAt.HasValue;
        private void ExecuteCatchKitten()
        {
            _catchPending = _match.TryCatchFugitiveKitten();
            _catchCommand.NotifyCanExecuteChanged();
        }
    }
}
