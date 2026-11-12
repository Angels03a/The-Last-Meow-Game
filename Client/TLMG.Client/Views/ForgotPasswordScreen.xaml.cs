using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de la recuperación de contraseña. Solo avisa al ViewModel cuando la caja de correo pierde el foco; la
    /// validación, los mensajes y la navegación viven en <see cref="ForgotPasswordViewModel"/>.
    /// </summary>
    public partial class ForgotPasswordScreen : LocalizableScreen
    {
        private readonly ForgotPasswordViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ForgotPasswordScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="forgotPasswordViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
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
