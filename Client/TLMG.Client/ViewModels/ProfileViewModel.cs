using System;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado de edición, la validación del nickname y la navegación de la pantalla de perfil. Los datos
    /// del perfil siguen siendo de muestra: el servidor aún no los entrega (BL-103).
    /// </summary>
    public class ProfileViewModel : ViewModelBase
    {
        private const int DaysSincePasswordChange = 45;
        private const string ProfileScreenResourcePrefix = "CreateAccount"; 
        private const string LastPasswordChangeFormatKey = "Profile_LastPasswordChange";

        private bool _isEditingAvatar;
        private bool _isEditingNickname;
        private string _currentAvatar = "🐱";
        private string _displayNickname = "WhiskerKnight";
        private string _editingNickname;
        private string _lastPasswordChangeLabel;
        private readonly INavigationManager _navigationManager;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProfileViewModel"/>.
        /// </summary>
        /// <param name="navigationManager">Administrador de navegación.</param>
        public ProfileViewModel(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            NicknameError = new StatusMessage();

            BackCommand = new RelayCommand(ExecuteBack);
            ToggleAvatarEditModeCommand = new RelayCommand(ExecuteToggleAvatarEditMode);
            SelectAvatarCommand = new RelayCommand<string>(ExecuteSelectAvatar);
            BeginNicknameEditCommand = new RelayCommand(ExecuteBeginNicknameEdit);
            SaveNicknameCommand = new RelayCommand(ExecuteSaveNickname);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        }

        /// <summary>Comando para regresar a la pantalla anterior.</summary>
        public ICommand BackCommand { get; }

        /// <summary>Comando para alternar entre el estado de lectura y edición de avatar.</summary>
        public ICommand ToggleAvatarEditModeCommand { get; }

        /// <summary>Comando para cambiar el avatar actual; el parámetro es el texto del avatar elegido.</summary>
        public ICommand SelectAvatarCommand { get; }

        /// <summary>Comando para entrar al modo de edición de nickname.</summary>
        public ICommand BeginNicknameEditCommand { get; }

        /// <summary>Comando para validar y guardar el nuevo nickname.</summary>
        public ICommand SaveNicknameCommand { get; }

        /// <summary>Comando para navegar a la pantalla de cambio de contraseña.</summary>
        public ICommand ChangePasswordCommand { get; }

        /// <summary>Obtiene si se está editando el avatar.</summary>
        public bool IsEditingAvatar
        {
            get { return _isEditingAvatar; }
            private set { SetProperty(ref _isEditingAvatar, value); }
        }

        /// <summary>Obtiene si se está editando el nickname.</summary>
        public bool IsEditingNickname
        {
            get { return _isEditingNickname; }
            private set { SetProperty(ref _isEditingNickname, value); }
        }

        /// <summary>Obtiene el avatar seleccionado.</summary>
        public string CurrentAvatar
        {
            get { return _currentAvatar; }
            private set { SetProperty(ref _currentAvatar, value); }
        }

        /// <summary>Obtiene el nickname mostrado.</summary>
        public string DisplayNickname
        {
            get { return _displayNickname; }
            private set { SetProperty(ref _displayNickname, value); }
        }

        /// <summary>Obtiene o establece el nickname en edición.</summary>
        public string EditingNickname
        {
            get { return _editingNickname; }
            set { SetProperty(ref _editingNickname, value); }
        }

        /// <summary>Mensaje de error de validación del nickname.</summary>
        public StatusMessage NicknameError { get; }

        /// <summary>Obtiene el texto localizado del último cambio de contraseña.</summary>
        public string LastPasswordChangeLabel
        {
            get { return _lastPasswordChangeLabel; }
            private set { SetProperty(ref _lastPasswordChangeLabel, value); }
        }

        /// <summary>
        /// Empieza a escuchar el cambio de idioma para refrescar el texto del último cambio de contraseña. La vista
        /// lo llama al cargarse.
        /// </summary>
        public void Activate()
        {
            Deactivate();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateLastPasswordChangeLabel();
        }

        /// <summary>Deja de escuchar el cambio de idioma. La vista lo llama al descargarse.</summary>
        public void Deactivate()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>Libera la suscripción al cambio de idioma y el temporizador del mensaje.</summary>
        protected override void OnDisposed()
        {
            Deactivate();
            NicknameError.Dispose();

            base.OnDisposed();
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            UpdateLastPasswordChangeLabel();
        }

        private void UpdateLastPasswordChangeLabel()
        {
            string passwordFormat = LanguageManager.FindText(LastPasswordChangeFormatKey);
            LastPasswordChangeLabel = string.Format(passwordFormat, DaysSincePasswordChange);
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteToggleAvatarEditMode()
        {
            IsEditingAvatar = !IsEditingAvatar;
        }

        private void ExecuteSelectAvatar(string avatarText)
        {
            if (!string.IsNullOrEmpty(avatarText))
            {
                CurrentAvatar = avatarText;
                IsEditingAvatar = false;
            }
        }

        private void ExecuteBeginNicknameEdit()
        {
            EditingNickname = DisplayNickname;
            NicknameError.Hide();
            IsEditingNickname = true;
        }

        private void ExecuteSaveNickname()
        {
            string nicknameErrorKey = ValidationHelper.GetNicknameValidationErrorKey(EditingNickname);

            if (nicknameErrorKey == null)
            {
                NicknameError.Hide();
                DisplayNickname = EditingNickname;
                IsEditingNickname = false;
            }
            else
            {
                NicknameError.ShowError(LanguageManager.FindText(nicknameErrorKey));
            }
        }

        private void ExecuteChangePassword()
        {
            // TODO: Navegar a ChangePasswordScreen cuando esté implementada. (BL-046).
        }
    }
}