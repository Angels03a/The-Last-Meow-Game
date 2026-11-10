using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la ventana principal: expone la pantalla que se está mostrando.
    /// </summary>
    public class ShellViewModel : ViewModelBase, INavigationHost
    {
        private object _currentScreen;

        public object CurrentScreen
        {
            get { return _currentScreen; }
            set { SetProperty(ref _currentScreen, value); }
        }
    }
}