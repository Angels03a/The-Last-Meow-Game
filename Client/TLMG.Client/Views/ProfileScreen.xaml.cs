using System.Windows;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class ProfileScreen : LocalizableScreen
    {
        private readonly ProfileViewModel _viewModel;

        public ProfileScreen(ProfileViewModel profileViewModel)
        {
            InitializeComponent();

            _viewModel = profileViewModel;
            DataContext = profileViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Activate();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Deactivate();
        }
    }
}