using System;
using System.Collections.Generic;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    public sealed class ModerationViewModel : ViewModelBase
    {
        private const string InvalidReportErrorKey = "Moderation_InvalidReportError";
        private const string NotSentErrorKey = "Moderation_NotSentError";
        private const string DefaultReportReasonCode = "OffensiveLanguage";
        private const string InvalidTargetErrorKey = "Moderation_InvalidTargetError";
        private const int MaximumCommentLength = 250;
        private static readonly HashSet<string> _reportReasonCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            DefaultReportReasonCode, "Harassment", "Cheating", "Spam", "InappropriateName", "Other"
        };
        private readonly INavigationManager _navigation;
        private readonly IModerationConnection _moderationConnection;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly RoomModerationNavigation _moderationNavigation;
        private readonly IDialogManager _dialogManager;
        private readonly ModerationNavigationParameter _target;
        private ModerationMode _mode;
        private bool _isBusy;
        private bool _isCompleted;
        private string _reportReasonCode = DefaultReportReasonCode;
        private string _reportComment = string.Empty;

        public ModerationViewModel(INavigationManager navigation, IModerationConnection moderationConnection,
            IDialogManager dialogManager, ModerationNavigationParameter target, ILobbyConnection lobbyConnection)
        {
            _navigation = navigation;
            _moderationConnection = moderationConnection;
            _lobbyConnection = lobbyConnection;
            _dialogManager = dialogManager;
            _target = target;
            _moderationNavigation = new RoomModerationNavigation(lobbyConnection,
                navigation, dialogManager, target.ReturnScreen);
            _mode = target.Mode;
            ReportTabCommand = new RelayCommand(ExecuteReportTab);
            BlockTabCommand = new RelayCommand(ExecuteBlockTab);
            SubmitCommand = new RelayCommand(ExecuteSubmit);
            ReturnCommand = new RelayCommand(ExecuteReturn);
            _moderationConnection.ReportPlayerResultReceived += OnModerationCompleted;
            _moderationConnection.BlockPlayerResultReceived += OnModerationCompleted;
            _moderationConnection.KickVoteResultReceived += OnModerationCompleted;
        }

        public string TargetPlayerName { get { return _target.PlayerName; } }
        public bool CanChangeTab { get { return _target.Mode == ModerationMode.Report || _target.Mode == ModerationMode.Block; } }
        public bool CanEdit { get { return !IsBusy && !IsCompleted; } }

        public ModerationMode Mode
        {
            get { return _mode; }
            private set { SetProperty(ref _mode, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanEdit));
                }
            }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            private set
            {
                if (SetProperty(ref _isCompleted, value))
                {
                    OnPropertyChanged(nameof(CanEdit));
                }
            }
        }

        public string ReportReasonCode
        {
            get { return _reportReasonCode; }
            set { SetProperty(ref _reportReasonCode, value); }
        }

        public string ReportComment
        {
            get { return _reportComment; }
            set { SetProperty(ref _reportComment, value); }
        }

        public ICommand ReportTabCommand { get; }
        public ICommand BlockTabCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand ReturnCommand { get; }

        private void ExecuteReportTab()
        {
            if (CanEdit && CanChangeTab)
            {
                Mode = ModerationMode.Report;
            }
        }

        private void ExecuteBlockTab()
        {
            if (CanEdit && CanChangeTab)
            {
                Mode = ModerationMode.Block;
            }
        }

        private void ExecuteSubmit()
        {
            if (CanEdit)
            {
                if (!CanModerateTarget())
                {
                    _dialogManager.ShowError(InvalidTargetErrorKey);
                }
                else if (Mode == ModerationMode.Report && !IsReportValid())
                {
                    _dialogManager.ShowError(InvalidReportErrorKey);
                }
                else
                {
                    IsBusy = true;
                    if (!SendModeration())
                    {
                        IsBusy = false;
                        _dialogManager.ShowError(NotSentErrorKey);
                    }
                }
            }
        }

        private bool CanModerateTarget()
        {
            bool canModerate = _moderationNavigation.CanOpen(_target.RoomId, _target.TargetUserId, Mode);

            return canModerate;
        }

        private bool IsReportValid()
        {
            bool isValid = _reportReasonCodes.Contains(ReportReasonCode)
                && (ReportComment == null || ReportComment.Length <= MaximumCommentLength);

            return isValid;
        }

        private bool SendModeration()
        {
            bool wasSent = false;

            if (Mode == ModerationMode.Report)
            {
                ReportSubmission submission = new ReportSubmission
                {
                    TargetUserId = _target.TargetUserId,
                    ReasonCode = ReportReasonCode,
                    Comment = ReportComment ?? string.Empty
                };
                wasSent = _moderationConnection.ReportPlayer(submission);
            }
            else if (Mode == ModerationMode.Block)
            {
                wasSent = _moderationConnection.BlockPlayer(_target.TargetUserId);
            }
            else if (Mode == ModerationMode.VoteToKick)
            {
                KickVote vote = new KickVote { RoomId = _target.RoomId, TargetUserId = _target.TargetUserId };
                wasSent = _moderationConnection.VoteToKick(vote);
            }

            return wasSent;
        }

        private void ExecuteReturn()
        {
            if (!IsBusy)
            {
                LobbyRoom room = _lobbyConnection.CurrentRoom;
                ScreenId destination = ScreenId.MainMenu;

                if (room != null && room.RoomId == _target.RoomId)
                {
                    destination = _target.ReturnScreen;
                    if (room.InGame)
                    {
                        destination = ScreenId.Gameplay;
                    }
                }

                _navigation.NavigateToWithoutHistory(destination);
            }
        }

        private void OnModerationCompleted(object sender, ServerEventArgs<OperationResult> eventArguments)
        {
            if (IsBusy)
            {
                OperationResult result = ServerConnectionManager.NormalizeOperationResult(eventArguments?.Payload);
                IsBusy = false;

                if (result.IsSuccess)
                {
                    IsCompleted = true;
                }
                else
                {
                    _dialogManager.ShowError(result.ErrorKey);
                }
            }
        }

        protected override void OnDisposed()
        {
            _moderationConnection.ReportPlayerResultReceived -= OnModerationCompleted;
            _moderationConnection.BlockPlayerResultReceived -= OnModerationCompleted;
            _moderationConnection.KickVoteResultReceived -= OnModerationCompleted;
            base.OnDisposed();
        }
    }
}