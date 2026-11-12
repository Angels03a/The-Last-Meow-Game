using System.ComponentModel;
using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de la configuración de audio e idioma. Solo conserva lógica visual: marcar el botón del idioma activo.
    /// Los volúmenes, el idioma y la navegación viven en <see cref="OptionsViewModel"/>.
    /// </summary>
    public partial class OptionsScreen : LocalizableScreen
    {
        private const string ActiveLanguageButtonStyleKey = "LanguageButtonActiveStyle";
        private const string InactiveLanguageButtonStyleKey = "LanguageButtonInactiveStyle";

        private readonly OptionsViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="OptionsScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="optionsViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public OptionsScreen(OptionsViewModel optionsViewModel)
        {
            InitializeComponent();

            _viewModel = optionsViewModel;
            DataContext = optionsViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Activate();
            ShowActiveLanguageButton();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Deactivate();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionsViewModel.IsEnglishActive))
            {
                ShowActiveLanguageButton();
            }
        }

        private void ShowActiveLanguageButton()
        {
            Style activeStyle = (Style)FindResource(ActiveLanguageButtonStyleKey);
            Style inactiveStyle = (Style)FindResource(InactiveLanguageButtonStyleKey);

            btnLanguageEnglish.Style = _viewModel.IsEnglishActive ? activeStyle : inactiveStyle;
            btnLanguageSpanish.Style = _viewModel.IsEnglishActive ? inactiveStyle : activeStyle;
        }
    }
}
