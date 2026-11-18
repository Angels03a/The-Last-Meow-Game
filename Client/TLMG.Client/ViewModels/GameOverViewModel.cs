using System;
using System.Windows.Input;
using System.Linq;
using TLMG.Client.UsersManagerReference;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Navigation;
using TLMG.Client.Models;

namespace TLMG.Client.ViewModels
{
    /// <summary>Estado y navegación de GameOver.</summary>
    public class GameOverViewModel : ViewModelBase
    {
        private readonly INavigationManager _navigation;
        /// <summary>Inicializa los comandos de navegación.</summary>
        public GameOverViewModel(INavigationManager navigation, MatchView result)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            WinnerName = result?.Players?.FirstOrDefault(player => player.UserId == result.WinnerUserId)?.Nickname
                ?? string.Empty;
            Standings = result?.FinalStandings;
            HasStandings = Standings != null;
            if (!string.IsNullOrWhiteSpace(WinnerName))
            {
                Notification = GameNotificationFactory.CreatePublic(
                    "GameOver_VictoryNoticeTitle", "GameOver_VictoryNoticeBody", WinnerName, "★", true);
            }
            MainMenuCommand = new RelayCommand(ExecuteMainMenu);
        }
        /// <summary>Continúa a la partida.</summary>
        public GameNotification Notification { get; }
        /// <summary>Regresa al menú principal.</summary>
        public ICommand MainMenuCommand { get; }

        private void ExecuteMainMenu() { _navigation.NavigateToWithoutHistory(ScreenId.MainMenu); }

        /// <summary>Valor de demostración del resultado.</summary>
        public string WinnerName { get; }

        /// <summary>Filas finales entregadas por el servidor; null si no están disponibles.</summary>
        public MatchStandingView[] Standings { get; }

        /// <summary>Indica si el servidor entregó la clasificación, incluso vacía.</summary>
        public bool HasStandings { get; }
    }
}
