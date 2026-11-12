using System;
using System.Globalization;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la pantalla de opciones: volumen de música y de efectos, idioma de la interfaz y regreso a la
    /// pantalla anterior. Los volúmenes todavía no se aplican a ningún sonido: el juego no tiene audio.
    /// </summary>
    public class OptionsViewModel : ViewModelBase
    {
        private const double DefaultMusicVolumeLevel = 50.0;
        private const double DefaultSoundEffectsVolumeLevel = 50.0;
        private const double VolumeScalePercentFactor = 100.0;
        private const string PercentFormat = "P0";
        private const string SpanishCultureCode = "es-MX";

        private readonly INavigationManager _navigationManager;

        private double _musicVolume = DefaultMusicVolumeLevel;
        private double _soundEffectsVolume = DefaultSoundEffectsVolumeLevel;
        private string _musicVolumeText = string.Empty;
        private string _soundEffectsVolumeText = string.Empty;
        private bool _isEnglishActive;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="OptionsViewModel"/>.
        /// </summary>
        /// <param name="navigationManager">Administrador de navegación.</param>
        public OptionsViewModel(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;

            BackCommand = new RelayCommand(ExecuteBack);
            ResetCommand = new RelayCommand(ExecuteReset);
            ChangeToEnglishCommand = new RelayCommand(ExecuteChangeToEnglish);
            ChangeToSpanishCommand = new RelayCommand(ExecuteChangeToSpanish);

            UpdateVolumeTexts();
            UpdateActiveLanguage();
        }

        /// <summary>Comando para regresar a la pantalla anterior.</summary>
        public ICommand BackCommand { get; }

        /// <summary>Comando para restablecer los volúmenes a su valor por defecto.</summary>
        public ICommand ResetCommand { get; }

        /// <summary>Comando para cambiar la interfaz a inglés.</summary>
        public ICommand ChangeToEnglishCommand { get; }

        /// <summary>Comando para cambiar la interfaz a español.</summary>
        public ICommand ChangeToSpanishCommand { get; }

        /// <summary>Volumen de la música, de 0 a 100.</summary>
        public double MusicVolume
        {
            get { return _musicVolume; }
            set
            {
                if (SetProperty(ref _musicVolume, value))
                {
                    UpdateVolumeTexts();
                }
            }
        }

        /// <summary>Volumen de los efectos de sonido, de 0 a 100.</summary>
        public double SoundEffectsVolume
        {
            get { return _soundEffectsVolume; }
            set
            {
                if (SetProperty(ref _soundEffectsVolume, value))
                {
                    UpdateVolumeTexts();
                }
            }
        }

        /// <summary>Volumen de la música como porcentaje en la cultura activa.</summary>
        public string MusicVolumeText
        {
            get { return _musicVolumeText; }
            private set { SetProperty(ref _musicVolumeText, value); }
        }

        /// <summary>Volumen de los efectos como porcentaje en la cultura activa.</summary>
        public string SoundEffectsVolumeText
        {
            get { return _soundEffectsVolumeText; }
            private set { SetProperty(ref _soundEffectsVolumeText, value); }
        }

        /// <summary>Indica si el idioma activo es el inglés; si no, es el español.</summary>
        public bool IsEnglishActive
        {
            get { return _isEnglishActive; }
            private set { SetProperty(ref _isEnglishActive, value); }
        }

        /// <summary>
        /// Empieza a escuchar el cambio de idioma para refrescar el idioma activo y los porcentajes. La vista lo
        /// llama al cargarse.
        /// </summary>
        public void Activate()
        {
            Deactivate();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateVolumeTexts();
            UpdateActiveLanguage();
        }

        /// <summary>Deja de escuchar el cambio de idioma. La vista lo llama al descargarse.</summary>
        public void Deactivate()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>Libera la suscripción al cambio de idioma.</summary>
        protected override void OnDisposed()
        {
            Deactivate();

            base.OnDisposed();
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteReset()
        {
            MusicVolume = DefaultMusicVolumeLevel;
            SoundEffectsVolume = DefaultSoundEffectsVolumeLevel;
        }

        private void ExecuteChangeToEnglish()
        {
            LanguageManager.ChangeLanguage(LanguageManager.EnglishCultureCode);
            UpdateActiveLanguage();
        }

        private void ExecuteChangeToSpanish()
        {
            LanguageManager.ChangeLanguage(SpanishCultureCode);
            UpdateActiveLanguage();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            UpdateVolumeTexts();
            UpdateActiveLanguage();
        }

        private void UpdateActiveLanguage()
        {
            IsEnglishActive = LanguageManager.CurrentCulture == LanguageManager.EnglishCultureCode;
        }

        private void UpdateVolumeTexts()
        {
            MusicVolumeText = FormatVolume(MusicVolume);
            SoundEffectsVolumeText = FormatVolume(SoundEffectsVolume);
        }

        private static string FormatVolume(double volumeLevel)
        {
            string volumeText = (volumeLevel / VolumeScalePercentFactor).ToString(
                PercentFormat,
                CultureInfo.CurrentCulture);

            return volumeText;
        }
    }
}
