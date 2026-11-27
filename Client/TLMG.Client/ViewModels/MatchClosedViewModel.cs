using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    public class MatchClosedViewModel : ViewModelBase
    {
        private const int DemoInactivityTimeoutMinutes = 10;
        private readonly INavigationManager _navigationManager;

        public MatchClosedViewModel(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            MainMenuCommand = new RelayCommand(ExecuteMainMenu);
        }

        public int InactivityTimeoutMinutes => DemoInactivityTimeoutMinutes;

        public ICommand MainMenuCommand { get; }

        private void ExecuteMainMenu()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.MainMenu);
        }
    }
}
