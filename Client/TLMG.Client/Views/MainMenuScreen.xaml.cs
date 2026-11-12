using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista del menú principal. Solo toma el foco al cargarse para que el acceso temporal con F8 responda; el
    /// nickname y la navegación viven en <see cref="MainMenuViewModel"/>.
    /// </summary>
    public partial class MainMenuScreen : LocalizableScreen
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainMenuScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="mainMenuViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public MainMenuScreen(MainMenuViewModel mainMenuViewModel)
        {
            InitializeComponent();
            Focusable = true;
            DataContext = mainMenuViewModel;

            Loaded += OnScreenLoaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }
    }
}
