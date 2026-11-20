using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using System.Windows.Threading;
using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la pantalla de recuperación de contraseña: valida el correo, avisa que se enviará un código y
    /// pasa a la verificación con el flujo de recuperación (BL-143).
    /// </summary>
    public class ForgotPasswordViewModel : ViewModelBase
    {
        private const string ScreenResourcePrefix = "ForgotPassword";
        private const string EmailRequiredErrorKey = "ForgotPassword_EmailRequiredError";
        private const string CodeSentMessageKey = "ForgotPassword_CodeSentMessage";
        private const int RedirectDelayMilliseconds = 500;

        private readonly INavigationManager _navigationManager;
        private readonly IPasswordConnection _passwordConnection;
        private readonly RelayCommand _sendCodeCommand;
        private readonly RelayCommand _backCommand;

        private string _email;
        private bool _isBusy;
        private DispatcherTimer _redirectTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ForgotPasswordViewModel"/>.
        /// </summary>
        /// <param name="navigationManager">Administrador de navegación.</param>
        /// <param name="passwordConnection">Conexión con el servicio de contraseñas.</param>
        public ForgotPasswordViewModel(INavigationManager navigationManager, IPasswordConnection passwordConnection)
        {
            _navigationManager = navigationManager;
            _passwordConnection = passwordConnection;

            EmailError = new StatusMessage();
            FormMessage = new StatusMessage();

            _sendCodeCommand = new RelayCommand(ExecuteSendCode, CanExecuteSendCode);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        /// <summary>Comando que valida el correo y solicita el envío del código de recuperación.</summary>
        public ICommand SendCodeCommand => _sendCodeCommand;

        /// <summary>Comando que solicita volver al inicio de sesión.</summary>
        public ICommand BackCommand => _backCommand;

        /// <summary>Mensaje de error asociado al campo de correo electrónico.</summary>
        public StatusMessage EmailError { get; }

        /// <summary>Mensaje general del formulario (éxito o error).</summary>
        public StatusMessage FormMessage { get; }

        /// <summary>Correo electrónico capturado.</summary>
        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        /// <summary>Indica si hay una operación en curso; deshabilita la interacción si es true.</summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _sendCodeCommand.NotifyCanExecuteChanged();
                    _backCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Valida el correo de forma aislada; la vista lo llama al perder el foco la caja de correo. Un correo vacío
        /// no marca error hasta que se intenta enviar el código.
        /// </summary>
        public void ValidateEmailOnly()
        {
            EmailError.Hide();

            if (!string.IsNullOrWhiteSpace(Email))
            {
                string emailErrorKey = ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);

                if (emailErrorKey != null)
                {
                    EmailError.ShowError(LanguageManager.FindText(emailErrorKey));
                }
            }
        }

        /// <summary>Detiene el temporizador de redirección y libera los mensajes.</summary>
        protected override void OnDisposed()
        {
            StopRedirectTimer();
            EmailError.Dispose();
            FormMessage.Dispose();

            base.OnDisposed();
        }

        private void ExecuteSendCode()
        {
            EmailError.Hide();
            FormMessage.Hide();

            if (string.IsNullOrWhiteSpace(Email))
            {
                FormMessage.ShowError(LanguageManager.FindText(EmailRequiredErrorKey));
            }
            else
            {
                string emailErrorKey = ValidationHelper.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);

                if (emailErrorKey == null)
                {
                    SendRecoveryCode();
                }
                else
                {
                    EmailError.ShowError(LanguageManager.FindText(emailErrorKey));
                }
            }
        }

        private bool CanExecuteSendCode()
        {
            bool canSendCode = !IsBusy;
            return canSendCode;
        }

        private void SendRecoveryCode()
        {
            IsBusy = true;
            OperationResult requestResult = _passwordConnection.RequestPasswordResetCode(
                Email.Trim(),
                LanguageManager.CurrentCulture);

            if (requestResult.IsSuccess)
            {
                FormMessage.ShowSuccess(LanguageManager.FindText(CodeSentMessageKey));
                StartRedirectTimer();
            }
            else
            {
                IsBusy = false;
                FormMessage.ShowError(LanguageManager.FindText(requestResult.ErrorKey));
            }
        }

        private void ExecuteBack()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.Login);
            _navigationManager.ClearHistory();
        }

        private bool CanExecuteBack()
        {
            bool canBack = !IsBusy;
            return canBack;
        }

        private void StartRedirectTimer()
        {
            StopRedirectTimer();
            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(RedirectDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;
            _redirectTimer.Start();
        }

        private void StopRedirectTimer()
        {
            if (_redirectTimer != null)
            {
                _redirectTimer.Stop();
                _redirectTimer.Tick -= OnRedirectTimerTick;
                _redirectTimer = null;
            }
        }

        private void OnRedirectTimerTick(object sender, EventArgs eventArgs)
        {
            StopRedirectTimer();
            IsBusy = false;
            _navigationManager.NavigateTo(
                ScreenId.Verification,
                new VerificationNavigationParameter(VerificationFlow.PasswordRecovery, Email.Trim()));
        }
    }
}
