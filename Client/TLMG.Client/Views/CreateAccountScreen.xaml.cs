using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de la pantalla de registro de cuentas.
    /// </summary>
    public partial class CreateAccountScreen : LocalizableScreen
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="createAccountViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public CreateAccountScreen(CreateAccountViewModel createAccountViewModel)
        {
            InitializeComponent();
            DataContext = createAccountViewModel;
        }
    }
}