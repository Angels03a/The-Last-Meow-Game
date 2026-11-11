using System;
using System.Windows;
using System.Windows.Controls;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla de configuración de audio e idioma de la aplicación.
    /// Delega la navegación al ViewModel y mantiene la lógica visual directa.
    /// </summary>
    public partial class OptionsScreen : LocalizableScreen
    {
        private const double DefaultMusicVolumeLevel = 50.0;
        private const double DefaultSoundEffectsVolumeLevel = 50.0;
        private const double VolumeScalePercentFactor = 100.0;
        private const string SpanishCultureIdentifier = "es-MX";
        private const string ActiveLanguageButtonStyleKey = "LanguageButtonActiveStyle";
        private const string InactiveLanguageButtonStyleKey = "LanguageButtonInactiveStyle";

        private readonly OptionsViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="OptionsScreen"/> con la configuración por defecto.
        /// </summary>
        public OptionsScreen()
        {
            InitializeComponent();

            _viewModel = new OptionsViewModel();
            DataContext = _viewModel;

            _viewModel.BackRequested += NavigateToMainMenu;

            InitializeDefaultSettings();
            InitializeLanguageButtons();
        }

        protected override void UpdateDynamicLabels()
        {
            UpdateVolumeLabel(lblMusicVol, sldMusic.Value);
            UpdateVolumeLabel(lblSoundEffectsVolume, sldSoundEffects.Value);
        }

        private void InitializeDefaultSettings()
        {
            sldMusic.Value = DefaultMusicVolumeLevel;
            sldSoundEffects.Value = DefaultSoundEffectsVolumeLevel;
        }

        private void InitializeLanguageButtons()
        {
            if (LanguageManager.CurrentCulture == LanguageManager.EnglishCultureCode)
            {
                btnLanguageEnglish.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
                btnLanguageSpanish.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
            }
            else
            {
                btnLanguageSpanish.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
                btnLanguageEnglish.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
            }
        }

        private void UpdateVolumeLabel(TextBlock targetLabelElement, double targetVolumeValue)
        {
            targetLabelElement.Text = (targetVolumeValue / VolumeScalePercentFactor).ToString("P0");
        }

        private void SldMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolumeLabel(lblMusicVol, e.NewValue);
        }

        private void SldSoundEffects_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolumeLabel(lblSoundEffectsVolume, e.NewValue);
        }

        private void BtnLanguageEnglish_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage(LanguageManager.EnglishCultureCode);
            btnLanguageEnglish.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLanguageSpanish.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnLanguageSpanish_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage(SpanishCultureIdentifier);
            btnLanguageSpanish.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLanguageEnglish.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            InitializeDefaultSettings();
        }

        private void NavigateToMainMenu(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }
    }
}