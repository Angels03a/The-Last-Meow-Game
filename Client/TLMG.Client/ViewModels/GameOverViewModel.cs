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
    public class GameOverViewModel : ViewModelBase
    {
        private const int NoWinnerUserId = 0;
        private const string UnexpectedErrorKey = "Common_UnexpectedError";
        private const string VictoryTitleKey = "GameOver_VictoryNoticeTitle";
        private const string VictoryBodyKey = "GameOver_VictoryNoticeBody";
        private const string VictoryIcon = "★";

        private readonly INavigationManager _navigationManager;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly IDialogManager _dialogManager;
        private readonly RelayCommand _mainMenuCommand;
        private readonly Guid? _finishedMatchId;
        private bool _isLeavingMatch;
        private bool _isDisposed;

        public GameOverViewModel(
            INavigationManager navigationManager,
            ILobbyConnection lobbyConnection,
            IDialogManager dialogManager,
            MatchView finishedMatch)
        {
            _navigationManager = navigationManager;
            _lobbyConnection = lobbyConnection;
            _dialogManager = dialogManager;
            _finishedMatchId = finishedMatch?.MatchId;

            WinnerName = FindWinnerName(finishedMatch);
            EndedByCollectiveAbandonment = finishedMatch != null
                && finishedMatch.Phase == MatchRoundPhase.MatchFinished
                && finishedMatch.WinnerUserId == NoWinnerUserId;
            Standings = finishedMatch?.FinalStandings;
            StandingEntries = CreateStandingEntries(Standings);

            if (!string.IsNullOrWhiteSpace(WinnerName))
            {
                Notification = new GameNotification
                {
                    TitleKey = VictoryTitleKey,
                    BodyKey = VictoryBodyKey,
                    Subject = WinnerName,
                    Icon = VictoryIcon,
                    IsSuccess = true
                };
            }

            _mainMenuCommand = new RelayCommand(ExecuteMainMenu, CanExecuteMainMenu);
            _lobbyConnection.FinishedMatchLeaveResultReceived += OnFinishedMatchLeaveResultReceived;
        }

        public GameNotification Notification { get; }

        public ICommand MainMenuCommand => _mainMenuCommand;

        public string WinnerName { get; }

        public bool EndedByCollectiveAbandonment { get; }

        public MatchStandingView[] Standings { get; }

        public IReadOnlyList<StandingEntry> StandingEntries { get; }

        public bool HasStandings => Standings != null;

        protected override void OnDisposed()
        {
            _isDisposed = true;
            _lobbyConnection.FinishedMatchLeaveResultReceived -= OnFinishedMatchLeaveResultReceived;
            base.OnDisposed();
        }

        private static string FindWinnerName(MatchView finishedMatch)
        {
            string winnerName = string.Empty;

            if (finishedMatch != null && finishedMatch.Players != null
                && finishedMatch.WinnerUserId != NoWinnerUserId)
            {
                foreach (MatchPlayerView player in finishedMatch.Players)
                {
                    if (player.UserId == finishedMatch.WinnerUserId)
                    {
                        winnerName = DisplayNameHelper.Resolve(player.UserId, player.Nickname);
                        break;
                    }
                }
            }

            return winnerName;
        }

        private static IReadOnlyList<StandingEntry> CreateStandingEntries(MatchStandingView[] standings)
        {
            List<StandingEntry> entries = new List<StandingEntry>();

            if (standings != null)
            {
                foreach (MatchStandingView standing in standings)
                {
                    entries.Add(new StandingEntry
                    {
                        Position = standing.Position,
                        DisplayName = DisplayNameHelper.Resolve(standing.UserId, standing.Nickname),
                        Points = standing.Points
                    });
                }
            }

            return entries.AsReadOnly();
        }

        private bool CanExecuteMainMenu()
        {
            bool canExecuteMainMenu = !_isLeavingMatch && !_isDisposed;

            return canExecuteMainMenu;
        }

        private void ExecuteMainMenu()
        {
            if (CanExecuteMainMenu())
            {
                if (!_finishedMatchId.HasValue && _lobbyConnection.CurrentRoom == null)
                {
                    NavigateToMainMenu();
                }
                else
                {
                    SetLeavingMatchState(true);
                    bool wasLeaveSent = _finishedMatchId.HasValue
                        && _lobbyConnection.LeaveFinishedMatch(_finishedMatchId.Value);

                    if (!wasLeaveSent && !_isDisposed && _isLeavingMatch)
                    {
                        SetLeavingMatchState(false);
                        _dialogManager.ShowError(UnexpectedErrorKey);
                    }
                }
            }
        }

        private void OnFinishedMatchLeaveResultReceived(
            object sender, ServerEventArgs<OperationResult> eventArgs)
        {
            if (_isLeavingMatch && !_isDisposed)
            {
                SetLeavingMatchState(false);
                OperationResult leaveConfirmation = eventArgs.Payload;

                if (leaveConfirmation != null && leaveConfirmation.IsSuccess)
                {
                    NavigateToMainMenu();
                }
                else
                {
                    _dialogManager.ShowError(leaveConfirmation?.ErrorKey ?? UnexpectedErrorKey);
                }
            }
        }

        private void SetLeavingMatchState(bool isLeavingMatch)
        {
            _isLeavingMatch = isLeavingMatch;
            _mainMenuCommand.NotifyCanExecuteChanged();
        }

        private void NavigateToMainMenu()
        {
            _navigationManager.ClearHistory();
            _navigationManager.NavigateToWithoutHistory(ScreenId.MainMenu);
        }
    }
}