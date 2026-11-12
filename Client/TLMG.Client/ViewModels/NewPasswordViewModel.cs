using System;
using System.Windows.Input;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la pantalla de nueva contraseña: valida la política y la coincidencia, calcula la fuerza y
    /// navega con el administrador de navegación. Todavía no envía la contraseña al servidor.
    /// </summary>
    public class NewPasswordViewModel : ViewModelBase
    {
        private const int VeryWeakScore = 1;
        private const int WeakScore = 2;
        private const int FairScore = 3;
        private const int GoodScore = 4;
        private const int StrongScore = 5;

        private const string WeakBrushKey = "Brush_StrengthWeak";
        private const string FairBrushKey = "Brush_StrengthFair";
        private const string StrongBrushKey = "Brush_StrengthStrong";

        private const string VeryWeakLabelKey = "NewPassword_StrengthVeryWeak";
        private const string WeakLabelKey = "NewPassword_StrengthWeak";
        private const string FairLabelKey = "NewPassword_StrengthFair";
        private const string GoodLabelKey = "NewPassword_StrengthGood";
        private const string StrongLabelKey = "NewPassword_StrengthStrong";

        private const string IncompleteFieldsErrorKey = "NewPassword_IncompleteFieldsError";
        private const string PolicyErrorKey = "NewPassword_PolicyError";
        private const string MismatchErrorKey = "NewPassword_MismatchError";
        private const string SuccessMessageKey = "NewPassword_SuccessMessage";
        private const int SuccessRedirectDelayMilliseconds = 500;

        private readonly INavigationManager _navigationManager;
        private readonly RelayCommand _setPasswordCommand;
        private readonly RelayCommand _backCommand;

        private string _newPassword;
        private string _confirmPassword;
        private int _passwordStrengthScore;
        private string _strengthLabelKey;
        private string _strengthBrushKey;
        private bool _isBusy;
        private DispatcherTimer _redirectTimer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="NewPasswordViewModel"/>.
        /// </summary>
        /// <param name="navigationManager">Administrador de navegación.</param>
        public NewPasswordViewModel(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;

            PasswordError = new StatusMessage();
            ConfirmPasswordError = new StatusMessage();
            FormMessage = new StatusMessage();

            _setPasswordCommand = new RelayCommand(ExecuteSetPassword, CanExecuteSetPassword);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        /// <summary>Comando que valida las contraseñas y solicita guardar la nueva.</summary>
        public ICommand SetPasswordCommand => _setPasswordCommand;

        /// <summary>Comando que solicita volver a la pantalla de contraseña olvidada.</summary>
        public ICommand BackCommand => _backCommand;

        /// <summary>Mensaje de error de la política de contraseña.</summary>
        public StatusMessage PasswordError { get; }

        /// <summary>Mensaje de error de la coincidencia de las contraseñas.</summary>
        public StatusMessage ConfirmPasswordError { get; }

        /// <summary>Mensaje general del formulario (éxito o error).</summary>
        public StatusMessage FormMessage { get; }

        /// <summary>
        /// Nueva contraseña capturada.
        /// </summary>
        public string NewPassword
        {
            get { return _newPassword; }
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    UpdatePasswordStrength();
                }
            }
        }

        /// <summary>
        /// Confirmación de la contraseña capturada.
        /// </summary>
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { SetProperty(ref _confirmPassword, value); }
        }

        /// <summary>
        /// Fuerza de la contraseña actual, de 0 a 5.
        /// </summary>
        public int PasswordStrengthScore
        {
            get { return _passwordStrengthScore; }
            private set { SetProperty(ref _passwordStrengthScore, value); }
        }

        /// <summary>
        /// Clave de recurso de la etiqueta de fuerza; null si la contraseña está vacía.
        /// </summary>
        public string StrengthLabelKey
        {
            get { return _strengthLabelKey; }
            private set { SetProperty(ref _strengthLabelKey, value); }
        }

        /// <summary>
        /// Clave de recurso del pincel del indicador de fuerza; null si la contraseña está vacía.
        /// </summary>
        public string StrengthBrushKey
        {
            get { return _strengthBrushKey; }
            private set { SetProperty(ref _strengthBrushKey, value); }
        }

        /// <summary>Indica si la contraseña se aceptó y se espera la redirección; bloquea los comandos.</summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _setPasswordCommand.NotifyCanExecuteChanged();
                    _backCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>Detiene el temporizador de redirección y libera los mensajes.</summary>
        protected override void OnDisposed()
        {
            StopRedirectTimer();
            PasswordError.Dispose();
            ConfirmPasswordError.Dispose();
            FormMessage.Dispose();

            base.OnDisposed();
        }

        private void ExecuteSetPassword()
        {
            ClearErrors();

            bool isFormValid = ValidateForm();

            if (isFormValid)
            {
                AcceptPassword();
            }
        }

        private bool CanExecuteSetPassword()
        {
            bool canSetPassword = !IsBusy;
            return canSetPassword;
        }

        // TODO: Enviar la nueva contraseña al servidor; hoy solo se valida. (BL-007).
        private void AcceptPassword()
        {
            IsBusy = true;
            FormMessage.ShowSuccess(LanguageManager.FindText(SuccessMessageKey));
            StartRedirectTimer();
        }

        private void ExecuteBack()
        {
            _navigationManager.NavigateToWithoutHistory(ScreenId.ForgotPassword);
        }

        private bool CanExecuteBack()
        {
            bool canBack = !IsBusy;
            return canBack;
        }

        private bool ValidateForm()
        {
            bool hasNewPassword = !string.IsNullOrWhiteSpace(NewPassword);
            bool hasConfirmation = !string.IsNullOrWhiteSpace(ConfirmPassword);
            bool areFieldsFilled = hasNewPassword && hasConfirmation;
            bool isPasswordStrong = PasswordStrengthScore >= ValidationHelper.RequiredPasswordStrengthScore;
            bool doPasswordsMatch = NewPassword == ConfirmPassword;
            bool isFormValid = false;

            if (!areFieldsFilled)
            {
                FormMessage.ShowError(LanguageManager.FindText(IncompleteFieldsErrorKey));
            }
            else if (!isPasswordStrong)
            {
                PasswordError.ShowError(LanguageManager.FindText(PolicyErrorKey));
            }
            else if (!doPasswordsMatch)
            {
                ConfirmPasswordError.ShowError(LanguageManager.FindText(MismatchErrorKey));
            }
            else
            {
                isFormValid = true;
            }

            return isFormValid;
        }

        private void UpdatePasswordStrength()
        {
            int strengthScore = ValidationHelper.CalculatePasswordStrength(NewPassword);

            StrengthLabelKey = GetStrengthLabelKey(strengthScore);
            StrengthBrushKey = GetStrengthBrushKey(strengthScore);
            PasswordStrengthScore = strengthScore;
        }

        private string GetStrengthLabelKey(int strengthScore)
        {
            string labelKey;

            switch (strengthScore)
            {
                case VeryWeakScore:
                    labelKey = VeryWeakLabelKey;
                    break;
                case WeakScore:
                    labelKey = WeakLabelKey;
                    break;
                case FairScore:
                    labelKey = FairLabelKey;
                    break;
                case GoodScore:
                    labelKey = GoodLabelKey;
                    break;
                case StrongScore:
                    labelKey = StrongLabelKey;
                    break;
                default:
                    labelKey = null;
                    break;
            }

            return labelKey;
        }

        private string GetStrengthBrushKey(int strengthScore)
        {
            bool hasStrength = strengthScore > 0;
            bool isWeak = strengthScore <= WeakScore;
            bool isFair = strengthScore == FairScore;
            string brushKey = null;

            if (hasStrength && isWeak)
            {
                brushKey = WeakBrushKey;
            }
            else if (hasStrength && isFair)
            {
                brushKey = FairBrushKey;
            }
            else if (hasStrength)
            {
                brushKey = StrongBrushKey;
            }

            return brushKey;
        }

        private void ClearErrors()
        {
            PasswordError.Hide();
            ConfirmPasswordError.Hide();
            FormMessage.Hide();
        }

        private void StartRedirectTimer()
        {
            StopRedirectTimer();
            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SuccessRedirectDelayMilliseconds)
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
            _navigationManager.NavigateToWithoutHistory(ScreenId.Login);
            _navigationManager.ClearHistory();
        }
    }
}
