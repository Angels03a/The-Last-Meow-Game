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
        private const double DefaultSfxVolumeLevel = 50.0;
        private const double VolumeScalePercentFactor = 100.0;
        private const string ActiveLanguageButtonStyleKey = "LangButtonActiveStyle";
        private const string InactiveLanguageButtonStyleKey = "LangButtonInactiveStyle";

        public OptionsScreen()
        {
            InitializeComponent();
            InitializeDefaultSettings();
            InitializeLanguageButtons();
        }

        protected override void UpdateDynamicLabels()
        {
            UpdateVolumeLabel(lblMusicVol, sldMusic.Value);
            UpdateVolumeLabel(lblSfxVol, sldSfx.Value);
        }

        private void InitializeDefaultSettings()
        {
            sldMusic.Value = DefaultMusicVolumeLevel;
            sldSfx.Value = DefaultSfxVolumeLevel;
        }

        private void InitializeLanguageButtons()
        {
            if (LanguageManager.CurrentCulture == LanguageManager.EnglishCultureCode)
            {
                btnLangEn.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
                btnLangEs.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
            }
            else
            {
                btnLangEs.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
                btnLangEn.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
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

        private void SldSfx_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolumeLabel(lblSfxVol, e.NewValue);
        }

        private void BtnLangEn_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage(LanguageManager.EnglishCultureCode);
            btnLangEn.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLangEs.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnLangEs_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage(LanguageManager.DefaultCultureCode);
            btnLangEs.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLangEn.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            sldMusic.Value = DefaultMusicVolumeLevel;
            sldSfx.Value = DefaultSfxVolumeLevel;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }
    }
}