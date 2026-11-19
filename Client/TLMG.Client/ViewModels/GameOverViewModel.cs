using System;
using System.Windows.Input;
using System.Linq;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Communication;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Navigation;
using TLMG.Client.Models;

namespace TLMG.Client.ViewModels
{
    /// <summary>Estado y navegación de GameOver.</summary>
    public class GameOverViewModel : ViewModelBase
    {
        private readonly INavigationManager _navigation;
        private readonly ILobbyConnection _lobbyConnection;
        private readonly IDialogManager _dialogs;
        private readonly RelayCommand _mainMenuCommand;
        private readonly Guid? _matchId;
        private bool _isLeaving;
        private bool _isDisposed;
        /// <summary>Inicializa los comandos de navegación.</summary>
        public GameOverViewModel(
            INavigationManager navigation, ILobbyConnection lobbyConnection, IDialogManager dialogs, MatchView result)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _lobbyConnection = lobbyConnection ?? throw new ArgumentNullException(nameof(lobbyConnection));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _matchId = result?.MatchId;
            _lobbyConnection.FinishedMatchLeaveResultReceived += OnFinishedMatchLeaveResultReceived;
            WinnerName = result?.Players?.FirstOrDefault(player => player.UserId == result.WinnerUserId)?.Nickname
                ?? string.Empty;
            Standings = result?.FinalStandings;
            HasStandings = Standings != null;
            if (!string.IsNullOrWhiteSpace(WinnerName))
            {
                Notification = GameNotificationFactory.CreatePublic(
                    "GameOver_VictoryNoticeTitle", "GameOver_VictoryNoticeBody", WinnerName, "★", true);
            }
            _mainMenuCommand = new RelayCommand(ExecuteMainMenu, CanExecuteMainMenu);
        }
        /// <summary>Continúa a la partida.</summary>
        public GameNotification Notification { get; }
        /// <summary>Regresa al menú principal.</summary>
        public ICommand MainMenuCommand => _mainMenuCommand;

        private bool CanExecuteMainMenu() => !_isLeaving;

        private void ExecuteMainMenu()
        {
            if (_isLeaving || _isDisposed)
            {
                return;
            }

            if (!_matchId.HasValue && _lobbyConnection.CurrentRoom == null)
            {
                NavigateToMainMenu();
                return;
            }

            _isLeaving = true;
            _mainMenuCommand.NotifyCanExecuteChanged();
            bool wasSent = _matchId.HasValue && _lobbyConnection.LeaveFinishedMatch(_matchId.Value);
            if (!wasSent && !_isDisposed)
            {
                _isLeaving = false;
                _mainMenuCommand.NotifyCanExecuteChanged();
                _dialogs.ShowError("Common_UnexpectedError");
            }
        }

        private void OnFinishedMatchLeaveResultReceived(object sender, ServerEventArgs<OperationResult> eventArgs)
        {
            if (!_isLeaving || _isDisposed)
            {
                return;
            }

            _isLeaving = false;
            _mainMenuCommand.NotifyCanExecuteChanged();
            if (eventArgs.Payload != null && eventArgs.Payload.IsSuccess)
            {
                NavigateToMainMenu();
            }
            else
            {
                _dialogs.ShowError(eventArgs.Payload?.ErrorKey ?? "Common_UnexpectedError");
            }
        }

        private void NavigateToMainMenu()
        {
            _navigation.ClearHistory();
            _navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }

        protected override void OnDisposed()
        {
            _isDisposed = true;
            _lobbyConnection.FinishedMatchLeaveResultReceived -= OnFinishedMatchLeaveResultReceived;
            base.OnDisposed();
        }

        /// <summary>Valor de demostración del resultado.</summary>
        public string WinnerName { get; }

        /// <summary>Filas finales entregadas por el servidor; null si no están disponibles.</summary>
        public MatchStandingView[] Standings { get; }

        /// <summary>Indica si el servidor entregó la clasificación, incluso vacía.</summary>
        public bool HasStandings { get; }
    }
}
