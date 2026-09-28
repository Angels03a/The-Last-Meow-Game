using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de configuración de audio e idioma de la aplicación.
    /// </summary>
    public partial class OptionsScreen : LocalizableScreen
    {
        private const double DefaultMusicVolumeLevel = 70.0;
        private const double DefaultSfxVolumeLevel = 85.0;
        private const double VolumeScalePercentFactor = 100.0;
        private const string EnglishCultureIdentifier = "en-US";
        private const string SpanishCultureIdentifier = "es-MX";
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
            if (LanguageManager.CurrentCulture == EnglishCultureIdentifier)
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
            if (targetLabelElement != null)
            {
                targetLabelElement.Text = (targetVolumeValue / VolumeScalePercentFactor).ToString("P0");
            }
        }

        private void SldMusic_ValueChanged(object eventSender, RoutedPropertyChangedEventArgs<double> eventArguments)
        {
            UpdateVolumeLabel(lblMusicVol, eventArguments.NewValue);
        }

        private void SldSfx_ValueChanged(object eventSender, RoutedPropertyChangedEventArgs<double> eventArguments)
        {
            UpdateVolumeLabel(lblSfxVol, eventArguments.NewValue);
        }

        private void BtnLangEn_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            LanguageManager.ChangeLanguage(EnglishCultureIdentifier);
            btnLangEn.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLangEs.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnLangEs_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            LanguageManager.ChangeLanguage(SpanishCultureIdentifier);
            btnLangEs.Style = (Style)FindResource(ActiveLanguageButtonStyleKey);
            btnLangEn.Style = (Style)FindResource(InactiveLanguageButtonStyleKey);
        }

        private void BtnReset_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            sldMusic.Value = DefaultMusicVolumeLevel;
            sldSfx.Value = DefaultSfxVolumeLevel;
        }

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }
    }
}