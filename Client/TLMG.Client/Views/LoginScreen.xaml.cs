using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de la pantalla de inicio de sesión.
    /// </summary>
    public partial class LoginScreen : LocalizableScreen
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="loginViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public LoginScreen(LoginViewModel loginViewModel)
        {
            InitializeComponent();
            DataContext = loginViewModel;
        }
    }
}