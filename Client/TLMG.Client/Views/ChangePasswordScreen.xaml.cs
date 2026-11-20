using System.Windows;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class ChangePasswordScreen : LocalizableScreen
    {
        private readonly ChangePasswordViewModel _viewModel;

        public ChangePasswordScreen(ChangePasswordViewModel changePasswordViewModel)
        {
            InitializeComponent();

            _viewModel = changePasswordViewModel;
            DataContext = changePasswordViewModel;

            Loaded += OnScreenLoaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Activate();
        }
    }
}