using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class MainMenuScreen : LocalizableScreen
    {
        private readonly MainMenuViewModel _mainMenuViewModel;

        public MainMenuScreen(MainMenuViewModel mainMenuViewModel)
        {
            InitializeComponent();
            Focusable = true;
            _mainMenuViewModel = mainMenuViewModel;
            DataContext = mainMenuViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
            _mainMenuViewModel.Activate();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _mainMenuViewModel.Deactivate();
        }
    }
}