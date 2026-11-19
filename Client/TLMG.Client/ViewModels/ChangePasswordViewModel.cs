using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado y comandos de la pantalla de cambio de contraseña.
    /// </summary>
    public class ChangePasswordViewModel : ViewModelBase
    {
        private readonly INavigationManager _navigation;
        private readonly IDialogManager _dialogs;
        private readonly IAccountsConnection _accountsConnection;
        private readonly RelayCommand _updatePasswordCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _backCommand;

        private string _avatarEmoji;
        private string _nickname;
        private string _email;
        private string _currentPassword;
        private string _newPassword;
        private string _confirmNewPassword;
        private bool _isBusy;

        private string _currentPasswordErrorMessage;
        private bool _hasCurrentPasswordError;

        private string _newPasswordErrorMessage;
        private bool _hasNewPasswordError;

        private string _confirmNewPasswordErrorMessage;
        private bool _hasConfirmNewPasswordError;

        private string _formMessage;
        private bool _hasFormMessage;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ChangePasswordViewModel"/>.
        /// </summary>
        public ChangePasswordViewModel(
            INavigationManager navigation,
            IDialogManager dialogs,
            IAccountsConnection accountsConnection)
        {
            _navigation = navigation;
            _dialogs = dialogs;
            _accountsConnection = accountsConnection;

            _updatePasswordCommand = new RelayCommand(ExecuteUpdatePassword, CanExecuteAction);
            _cancelCommand = new RelayCommand(ExecuteCancel, CanExecuteAction);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteAction);

            // TODO: Cargar los datos de la sesión activa del jugador (AvatarEmoji, Nickname y Email).
            _avatarEmoji = "🐱";
            _nickname = string.Empty;
            _email = string.Empty;
        }

        /// <summary>Comando que solicita actualizar la contraseña.</summary>
        public ICommand UpdatePasswordCommand => _updatePasswordCommand;

        /// <summary>Comando que cancela la operación y limpia o regresa al perfil.</summary>
        public ICommand CancelCommand => _cancelCommand;

        /// <summary>Comando que regresa a la pantalla de perfil.</summary>
        public ICommand BackCommand => _backCommand;

        public string AvatarEmoji
        {
            get { return _avatarEmoji; }
            set { SetProperty(ref _avatarEmoji, value); }
        }

        public string Nickname
        {
            get { return _nickname; }
            set { SetProperty(ref _nickname, value); }
        }

        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        public string CurrentPassword
        {
            get { return _currentPassword; }
            set { SetProperty(ref _currentPassword, value); }
        }

        public string NewPassword
        {
            get { return _newPassword; }
            set { SetProperty(ref _newPassword, value); }
        }

        public string ConfirmNewPassword
        {
            get { return _confirmNewPassword; }
            set { SetProperty(ref _confirmNewPassword, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _updatePasswordCommand.NotifyCanExecuteChanged();
                    _cancelCommand.NotifyCanExecuteChanged();
                    _backCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string CurrentPasswordErrorMessage
        {
            get { return _currentPasswordErrorMessage; }
            private set { SetProperty(ref _currentPasswordErrorMessage, value); }
        }

        public bool HasCurrentPasswordError
        {
            get { return _hasCurrentPasswordError; }
            private set { SetProperty(ref _hasCurrentPasswordError, value); }
        }

        public string NewPasswordErrorMessage
        {
            get { return _newPasswordErrorMessage; }
            private set { SetProperty(ref _newPasswordErrorMessage, value); }
        }

        public bool HasNewPasswordError
        {
            get { return _hasNewPasswordError; }
            private set { SetProperty(ref _hasNewPasswordError, value); }
        }

        public string ConfirmNewPasswordErrorMessage
        {
            get { return _confirmNewPasswordErrorMessage; }
            private set { SetProperty(ref _confirmNewPasswordErrorMessage, value); }
        }

        public bool HasConfirmNewPasswordError
        {
            get { return _hasConfirmNewPasswordError; }
            private set { SetProperty(ref _hasConfirmNewPasswordError, value); }
        }

        public string FormMessage
        {
            get { return _formMessage; }
            private set { SetProperty(ref _formMessage, value); }
        }

        public bool HasFormMessage
        {
            get { return _hasFormMessage; }
            private set { SetProperty(ref _hasFormMessage, value); }
        }

        private void ExecuteUpdatePassword()
        {
            // TODO: Implementar validación de contraseña actual, política de nueva contraseña, confirmación y llamada al servicio de cuentas.
        }

        private void ExecuteCancel()
        {
            _navigation.GoBack();
        }

        private void ExecuteBack()
        {
            _navigation.GoBack();
        }

        private bool CanExecuteAction()
        {
            bool canExecute = !IsBusy;
            return canExecute;
        }
    }
}