using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la ventana principal: expone la pantalla que se está mostrando.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, INavigationHost
    {
        private object _currentScreen;

        /// <summary>
        /// Pantalla que se está mostrando en la ventana principal.
        /// </summary>
        public object CurrentScreen
        {
            get { return _currentScreen; }
            set { SetProperty(ref _currentScreen, value); }
        }
    }
}