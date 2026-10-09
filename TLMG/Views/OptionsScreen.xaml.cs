using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de configuración de audio e idioma de la aplicación.
    /// </summary>
    public partial class OptionsScreen : LocalizableScreen
    {
        private const double DefaultMusicVolumeLevel = 50.0;
        private const double DefaultSoundEffectsVolumeLevel = 50.0;        
        private const double VolumeScalePercentFactor = 100.0;
        private const string EnglishCultureIdentifier = "en-US";
        private const string SpanishCultureIdentifier = "es-MX";
        private const string ActiveLanguageButtonStyleKey = "LanguageButtonActiveStyle";
        private const string InactiveLanguageButtonStyleKey = "LanguageButtonInactiveStyle";

        public OptionsScreen()
        {
            InitializeComponent();
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

        private void BtnLangEn_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage(EnglishCultureIdentifier);
            btnLanguageEnglish.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLanguageSpanish.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnLangEs_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage(SpanishCultureIdentifier);
            btnLanguageSpanish.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLanguageEnglish.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            sldMusic.Value = DefaultMusicVolumeLevel;
            sldSoundEffects.Value = DefaultSoundEffectsVolumeLevel;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }
    }
}