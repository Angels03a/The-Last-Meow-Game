using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using TLMG.Client.ViewModels;
using TLMG.Client.Navigation;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de perfil de usuario para visualización de estadísticas y edición de avatar o nickname.
    /// Delega su estado y lógica de validación al ViewModel.
    /// </summary>
    public partial class ProfileScreen : LocalizableScreen
    {
        private readonly ProfileViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProfileScreen"/>.
        /// </summary>
        public ProfileScreen()
        {
            InitializeComponent();

            _viewModel = new ProfileViewModel();
            DataContext = _viewModel;

            _viewModel.BackRequested += NavigateToMainMenu;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override void UpdateDynamicLabels()
        {
            string passwordFormat = TryFindResource("Profile_LastPasswordChange") as string;
            _viewModel.UpdateLocalizedFormats(passwordFormat);
        }

        private void BtnSelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button buttonSender)
            {
                _viewModel.ProcessAvatarSelection(buttonSender.Tag);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            bool isNicknameErrorChanged = e.PropertyName == nameof(ProfileViewModel.NicknameErrorKey);

            if (isNicknameErrorChanged)
            {
                UpdateNicknameErrorVisibility();
            }
        }

        private void UpdateNicknameErrorVisibility()
        {
            if (_viewModel.NicknameErrorKey != null)
            {
                lblNicknameError.Text = TryFindResource(_viewModel.NicknameErrorKey) as string;
                lblNicknameError.Visibility = Visibility.Visible;
            }
            else
            {
                lblNicknameError.Visibility = Visibility.Hidden;
            }
        }

        private void NavigateToMainMenu(object sender, EventArgs e)
        {
            ((App)Application.Current).Navigation.NavigateTo(ScreenId.MainMenu);
        }
    }
}