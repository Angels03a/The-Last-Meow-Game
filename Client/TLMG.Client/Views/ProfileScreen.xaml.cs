using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista del perfil del jugador (estadísticas, avatar y nickname). No tiene lógica propia: el estado, la
    /// validación y la navegación viven en <see cref="ProfileViewModel"/>.
    /// </summary>
    public partial class ProfileScreen : LocalizableScreen
    {
        private readonly ProfileViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProfileScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="profileViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
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
