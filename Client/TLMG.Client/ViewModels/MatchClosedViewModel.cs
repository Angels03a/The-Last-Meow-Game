using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>Estado y navegación de la pantalla de partida cerrada.</summary>
    public class MatchClosedViewModel : ViewModelBase
    {
        private const int DemoInactivityTimeoutMinutes = 10;
        private readonly INavigationManager _navigation;

        /// <summary>Inicializa la navegación de la pantalla.</summary>
        /// <param name="navigation">Administrador de navegación.</param>
        public MatchClosedViewModel(INavigationManager navigation)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            MainMenuCommand = new RelayCommand(ExecuteMainMenu);
        }

        /// <summary>Tiempo de inactividad mostrado por la demostración.</summary>
        public int InactivityTimeoutMinutes => DemoInactivityTimeoutMinutes;

        /// <summary>Regresa al menú principal.</summary>
        public ICommand MainMenuCommand { get; }

        private void ExecuteMainMenu()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.MainMenu);
        }
    }
}
