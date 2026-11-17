using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de la pantalla de cambio de contraseña del jugador.
    /// </summary>
    public partial class ChangePasswordScreen : LocalizableScreen
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ChangePasswordScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="changePasswordViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public ChangePasswordScreen(ChangePasswordViewModel changePasswordViewModel)
        {
            InitializeComponent();
            DataContext = changePasswordViewModel;
        }
    }
}