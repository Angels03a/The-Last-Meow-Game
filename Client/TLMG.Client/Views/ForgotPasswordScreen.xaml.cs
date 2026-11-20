using System.Windows;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class ForgotPasswordScreen : LocalizableScreen
    {
        private readonly ForgotPasswordViewModel _viewModel;

        public ForgotPasswordScreen(ForgotPasswordViewModel forgotPasswordViewModel)
        {
            InitializeComponent();

            _viewModel = forgotPasswordViewModel;
            DataContext = forgotPasswordViewModel;
        }

        private void TxtEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.ValidateEmailOnly();
        }
    }
}