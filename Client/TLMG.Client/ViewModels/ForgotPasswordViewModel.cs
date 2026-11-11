using System;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado, la validación y los comandos de la pantalla de recuperación de contraseña.
    /// </summary>
    public class ForgotPasswordViewModel : ViewModelBase
    {
        private const string ScreenResourcePrefix = "ForgotPassword";
        private const string EmailRequiredErrorKey = "ForgotPassword_EmailRequiredError";

        private readonly RelayCommand _sendCodeCommand;
        private readonly RelayCommand _backCommand;
        private string _email;
        private bool _isBusy;
        private string _emailErrorKey;
        private string _formErrorKey;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ForgotPasswordViewModel"/>.
        /// </summary>
        public ForgotPasswordViewModel()
        {
            _sendCodeCommand = new RelayCommand(ExecuteSendCode, CanExecuteSendCode);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        /// <summary>
        /// Se produce cuando las validaciones son exitosas y se solicita el envío del código.
        /// </summary>
        public event EventHandler CodeSentRequested;

        /// <summary>
        /// Se produce cuando hay errores de validación que la vista debe mostrar.
        /// </summary>
        public event EventHandler ValidationFailed;

        /// <summary>
        /// Se produce cuando el usuario solicita regresar a la pantalla de inicio de sesión.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>Comando que valida el correo y solicita el envío del código de recuperación.</summary>
        public ICommand SendCodeCommand => _sendCodeCommand;

        /// <summary>Comando que solicita volver al inicio de sesión.</summary>
        public ICommand BackCommand => _backCommand;

        /// <summary>
        /// Correo electrónico capturado.
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        /// <summary>
        /// Indica si hay una operación en curso; deshabilita la interacción si es true.
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _sendCodeCommand.NotifyCanExecuteChanged();
                    _backCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Clave de error específica del correo; null si no hay error.
        /// </summary>
        public string EmailErrorKey
        {
            get { return _emailErrorKey; }
            private set { SetProperty(ref _emailErrorKey, value); }
        }

        /// <summary>
        /// Clave de error general del formulario; null si no hay error.
        /// </summary>
        public string FormErrorKey
        {
            get { return _formErrorKey; }
            private set { SetProperty(ref _formErrorKey, value); }
        }

        /// <summary>
        /// Valida el correo de forma aislada (usado para el evento LostFocus de la vista).
        /// </summary>
        public void ValidateEmailOnly()
        {
            EmailErrorKey = null;

            if (!string.IsNullOrWhiteSpace(Email))
            {
                EmailErrorKey = ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);

                if (EmailErrorKey != null)
                {
                    ValidationFailed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ExecuteSendCode()
        {
            EmailErrorKey = null;
            FormErrorKey = null;

            if (string.IsNullOrWhiteSpace(Email))
            {
                FormErrorKey = EmailRequiredErrorKey;
                ValidationFailed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                EmailErrorKey = ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);

                if (EmailErrorKey == null)
                {
                    CodeSentRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ValidationFailed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool CanExecuteSendCode()
        {
            bool canSendCode = !IsBusy;
            return canSendCode;
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteBack()
        {
            bool canBack = !IsBusy;
            return canBack;
        }
    }
}