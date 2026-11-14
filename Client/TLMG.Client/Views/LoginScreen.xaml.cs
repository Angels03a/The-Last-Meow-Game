using System.ComponentModel;
using System.Windows;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de la pantalla de inicio de sesión.
    /// </summary>
    public partial class LoginScreen : LocalizableScreen
    {
        private const string ActiveLanguageButtonStyleKey = "LanguageButtonActiveStyle";
        private const string InactiveLanguageButtonStyleKey = "LanguageButtonInactiveStyle";

        private readonly LoginViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="loginViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public LoginScreen(LoginViewModel loginViewModel)
        {
            InitializeComponent();

            _viewModel = loginViewModel;
            DataContext = loginViewModel;

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
            if (e.PropertyName == nameof(LoginViewModel.IsEnglishActive))
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