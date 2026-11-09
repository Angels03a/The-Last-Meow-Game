using System;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado de edición, validación de perfil y comandos de navegación de la pantalla de perfil.
    /// </summary>
    public class ProfileViewModel : ViewModelBase
    {
        private const int DaysSincePasswordChange = 45;
        private const string ProfileScreenResourcePrefix = "CreateAccount"; 
        private const string DefaultLastPasswordFormat = "{0}";

        private bool _isEditingAvatar;
        private bool _isEditingNickname;
        private string _currentAvatar = "🐱";
        private string _displayNickname = "WhiskerKnight";
        private string _editingNickname;
        private string _nicknameErrorKey;
        private string _lastPasswordChangeLabel;
        private string _currentPasswordFormat = DefaultLastPasswordFormat;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProfileViewModel"/>.
        /// </summary>
        public ProfileViewModel()
        {
            BackCommand = new RelayCommand(ExecuteBack);
            ToggleAvatarEditModeCommand = new RelayCommand(ExecuteToggleAvatarEditMode);
            SelectAvatarCommand = new RelayCommand(ExecuteSelectAvatar);
            BeginNicknameEditCommand = new RelayCommand(ExecuteBeginNicknameEdit);
            SaveNicknameCommand = new RelayCommand(ExecuteSaveNickname);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        }

        /// <summary>
        /// Se produce cuando se solicita la navegación de regreso.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>
        /// Comando para regresar a la pantalla anterior.
        /// </summary>
        public RelayCommand BackCommand { get; }

        /// <summary>
        /// Comando para alternar entre el estado de lectura y edición de avatar.
        /// </summary>
        public RelayCommand ToggleAvatarEditModeCommand { get; }

        /// <summary>
        /// Comando para cambiar el avatar actual por el recibido desde la interfaz.
        /// </summary>
        public RelayCommand SelectAvatarCommand { get; }

        /// <summary>
        /// Comando para entrar al modo de edición de nickname.
        /// </summary>
        public RelayCommand BeginNicknameEditCommand { get; }

        /// <summary>
        /// Comando para validar y guardar el nuevo nickname.
        /// </summary>
        public RelayCommand SaveNicknameCommand { get; }

        /// <summary>
        /// Comando para navegar a la pantalla de cambio de contraseña.
        /// </summary>
        public RelayCommand ChangePasswordCommand { get; }

        public bool IsEditingAvatar
        {
            get { return _isEditingAvatar; }
            private set { SetProperty(ref _isEditingAvatar, value); }
        }

        public bool IsEditingNickname
        {
            get { return _isEditingNickname; }
            private set { SetProperty(ref _isEditingNickname, value); }
        }

        public string CurrentAvatar
        {
            get { return _currentAvatar; }
            private set { SetProperty(ref _currentAvatar, value); }
        }

        public string DisplayNickname
        {
            get { return _displayNickname; }
            private set { SetProperty(ref _displayNickname, value); }
        }

        public string EditingNickname
        {
            get { return _editingNickname; }
            set { SetProperty(ref _editingNickname, value); }
        }

        public string NicknameErrorKey
        {
            get { return _nicknameErrorKey; }
            private set { SetProperty(ref _nicknameErrorKey, value); }
        }

        public string LastPasswordChangeLabel
        {
            get { return _lastPasswordChangeLabel; }
            private set { SetProperty(ref _lastPasswordChangeLabel, value); }
        }

        /// <summary>
        /// Actualiza el formato del texto localizado de "Último cambio de contraseña".
        /// </summary>
        public void UpdateLocalizedFormats(string passwordFormat)
        {
            _currentPasswordFormat = passwordFormat ?? DefaultLastPasswordFormat;
            LastPasswordChangeLabel = string.Format(_currentPasswordFormat, DaysSincePasswordChange);
        }

        /// <summary>
        /// Procesa la selección de un nuevo avatar, validando que el parámetro enviado por el CommandParameter sea texto.
        /// </summary>
        public void ProcessAvatarSelection(object commandParameterAvatar)
        {
            if (commandParameterAvatar is string newAvatarText)
            {
                CurrentAvatar = newAvatarText;
                IsEditingAvatar = false;
            }
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteToggleAvatarEditMode()
        {
            IsEditingAvatar = !IsEditingAvatar;
        }

        private void ExecuteSelectAvatar()
        {
            
        }

        private void ExecuteBeginNicknameEdit()
        {
            EditingNickname = DisplayNickname;
            NicknameErrorKey = null;
            IsEditingNickname = true;
        }

        private void ExecuteSaveNickname()
        {
            NicknameErrorKey = ValidationHelper.GetNicknameValidationErrorKey(EditingNickname);

            if (NicknameErrorKey == null)
            {
                DisplayNickname = EditingNickname;
                IsEditingNickname = false;
            }
        }

        private void ExecuteChangePassword()
        {
            // TODO: Navegar a ChangePasswordScreen cuando esté implementada. (BL-046).
        }
    }
}