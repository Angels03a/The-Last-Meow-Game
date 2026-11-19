using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Views;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado de edición, la validación y el guardado del perfil del jugador. El nickname, el correo
    /// y el avatar vienen del servidor; las estadísticas siguen siendo de muestra (BL-103).
    /// </summary>
    public class ProfileViewModel : ViewModelBase
    {
        private const string LastPasswordChangeFormatKey = "Profile_LastPasswordChange";

        private readonly INavigationManager _navigationManager;
        private readonly IProfileConnection _profileConnection;

        private bool _isEditingAvatar;
        private bool _isEditingNickname;
        private string _currentAvatarKey = AvatarCatalog.DefaultAvatarKey;
        private string _currentAvatar = AvatarCatalog.FindEmoji(AvatarCatalog.DefaultAvatarKey);
        private string _displayNickname;
        private string _email;
        private string _editingNickname;
        private string _lastPasswordChangeLabel;
        private int _daysSincePasswordChange;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProfileViewModel"/>.
        /// </summary>
        /// <param name="navigationManager">Administrador de navegación.</param>
        /// <param name="profileConnection">Conexión con el servidor para leer y guardar el perfil.</param>
        public ProfileViewModel(INavigationManager navigationManager, IProfileConnection profileConnection)
        {
            _navigationManager = navigationManager;
            _profileConnection = profileConnection;
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

        /// <summary>Comando para cambiar el avatar; el parámetro es la clave del avatar elegido.</summary>
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

        /// <summary>Obtiene el emoji del avatar seleccionado.</summary>
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

        /// <summary>Obtiene el correo de la cuenta.</summary>
        public string Email
        {
            get { return _email; }
            private set { SetProperty(ref _email, value); }
        }

        /// <summary>Obtiene o establece el nickname en edición.</summary>
        public string EditingNickname
        {
            get { return _editingNickname; }
            set { SetProperty(ref _editingNickname, value); }
        }

        /// <summary>Mensaje de error del perfil.</summary>
        public StatusMessage NicknameError { get; }

        /// <summary>Obtiene el texto localizado del último cambio de contraseña.</summary>
        public string LastPasswordChangeLabel
        {
            get { return _lastPasswordChangeLabel; }
            private set { SetProperty(ref _lastPasswordChangeLabel, value); }
        }

        /// <summary>
        /// Carga el perfil del servidor y empieza a escuchar el cambio de idioma. La vista lo llama al cargarse.
        /// </summary>
        public void Activate()
        {
            Deactivate();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateLastPasswordChangeLabel();
            LoadProfile();
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
            LastPasswordChangeLabel = string.Format(passwordFormat, _daysSincePasswordChange);
        }

        private void LoadProfile()
        {
            OperationPlayerProfileResult profileResult = _profileConnection.GetPlayerProfile();

            if (profileResult.IsSuccess)
            {
                NicknameError.Hide();
                DisplayNickname = profileResult.Nickname;
                Email = profileResult.Email;
                ApplyAvatar(profileResult.AvatarKey);
                _daysSincePasswordChange = (DateTime.UtcNow - profileResult.PasswordChangedAtUtc).Days;
                UpdateLastPasswordChangeLabel();
            }
            else
            {
                ShowProfileError(profileResult.ErrorKey);
            }
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteToggleAvatarEditMode()
        {
            IsEditingAvatar = !IsEditingAvatar;
        }

        private void ExecuteSelectAvatar(string avatarKey)
        {
            if (!string.IsNullOrEmpty(avatarKey))
            {
                string errorKey = SaveProfile(DisplayNickname, avatarKey);

                if (errorKey == null)
                {
                    NicknameError.Hide();
                    ApplyAvatar(avatarKey);
                    IsEditingAvatar = false;
                }
                else
                {
                    ShowProfileError(errorKey);
                }
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
            string errorKey = ValidationHelper.GetNicknameValidationErrorKey(EditingNickname);

            if (errorKey == null)
            {
                errorKey = SaveProfile(EditingNickname, _currentAvatarKey);
            }

            if (errorKey == null)
            {
                NicknameError.Hide();
                DisplayNickname = EditingNickname;
                IsEditingNickname = false;
            }
            else
            {
                ShowProfileError(errorKey);
            }
        }

        private void ExecuteChangePassword()
        {
            _navigationManager.NavigateTo(ScreenId.ChangePassword);
        }

        private string SaveProfile(string nickname, string avatarKey)
        {
            string errorKey = null;
            OperationResult updateResult = _profileConnection.UpdatePlayerProfile(nickname, avatarKey);

            if (!updateResult.IsSuccess)
            {
                errorKey = updateResult.ErrorKey;
            }

            return errorKey;
        }

        private void ApplyAvatar(string avatarKey)
        {
            _currentAvatarKey = avatarKey;
            CurrentAvatar = AvatarCatalog.FindEmoji(avatarKey);
        }

        private void ShowProfileError(string errorKey)
        {
            NicknameError.ShowError(LanguageManager.FindText(errorKey));
        }

        private ScreenEntry BuildProfileEntry()
        {
            ProfileViewModel profileViewModel = new ProfileViewModel(_navigationManager, _profileConnection);
            ProfileScreen profileScreen = new ProfileScreen(profileViewModel);

            return new ScreenEntry(profileScreen, profileViewModel);
        }
    }
}