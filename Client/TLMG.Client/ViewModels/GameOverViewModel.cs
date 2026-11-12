using System;
using System.Windows.Input;
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
        public GameOverViewModel(INavigationManager navigation)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            ContinueCommand = new RelayCommand(ExecuteContinue);
            MainMenuCommand = new RelayCommand(ExecuteMainMenu);
        }
        /// <summary>Continúa a la partida.</summary>
        public ICommand ContinueCommand { get; }
        /// <summary>Regresa al menú principal.</summary>
        public ICommand MainMenuCommand { get; }
        private void ExecuteContinue() { _navigation.NavigateToWithoutHistory(ScreenId.Gameplay); }
        private void ExecuteMainMenu() { _navigation.NavigateToWithoutHistory(ScreenId.MainMenu); }
        // FIXME: Conservar datos de demostración hasta integrar el resultado real de la partida.
        /// <summary>Valor de demostración del resultado.</summary>
        public string WinnerName => "NyanProwler";
    }
}
