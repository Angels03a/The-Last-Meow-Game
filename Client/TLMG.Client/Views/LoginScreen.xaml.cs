using System.ComponentModel;
using System.Windows;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class LoginScreen : LocalizableScreen
    {
        private const string ActiveLanguageButtonStyleKey = "LanguageButtonActiveStyle";
        private const string InactiveLanguageButtonStyleKey = "LanguageButtonInactiveStyle";

        private readonly LoginViewModel _viewModel;

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