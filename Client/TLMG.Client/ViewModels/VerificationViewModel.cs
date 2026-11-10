using System;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado, temporizadores lógicos y comandos de la pantalla de verificación por código.
    /// </summary>
    public class VerificationViewModel : ViewModelBase
    {
        private const int MaximumVerificationAttempts = 3;
        private const int VerificationCodeExpirySeconds = 300;
        private const int CodeResendCooldownSeconds = 60;
        private const int TimeExpired = 0;
        private const int InitialFailedAttempts = 0;
        private const int SecondsPerMinute = 60;

        private const string SimulatedVerificationCode = "123456";
        private const string TimerFormat = "{0:00}:{1:00}";
        private const string ExpiredCodeErrorKey = "Verification_ExpiredCodeError";
        private const string AttemptsExhaustedErrorKey = "Verification_AttemptsExhaustedError";
        private const string IncorrectCodeErrorKey = "Verification_IncorrectCodeError";

        private string _digit1;
        private string _digit2;
        private string _digit3;
        private string _digit4;
        private string _digit5;
        private string _digit6;
        private int _failedAttempts;
        private int _remainingSecondsUntilExpiry;
        private int _remainingSecondsUntilResend;
        private bool _areDigitsEnabled;
        private bool _isCountdownRunning;
        private string _expirationText;
        private string _formErrorKey;
        private string _verificationErrorKey;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="VerificationViewModel"/>.
        /// </summary>
        public VerificationViewModel()
        {
            _remainingSecondsUntilExpiry = VerificationCodeExpirySeconds;
            _remainingSecondsUntilResend = CodeResendCooldownSeconds;
            _failedAttempts = InitialFailedAttempts;
            _areDigitsEnabled = true;

            VerifyCodeCommand = new RelayCommand(ExecuteVerifyCode, CanExecuteVerifyCode);
            ResendCodeCommand = new RelayCommand(ExecuteResendCode, CanExecuteResendCode);
            BackCommand = new RelayCommand(ExecuteBack);

            UpdateExpirationText();
        }

        /// <summary>
        /// Se produce cuando el código capturado es correcto.
        /// </summary>
        public event EventHandler VerificationSucceeded;

        /// <summary>
        /// Se produce cuando el jugador solicita volver a la pantalla anterior.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>
        /// Se produce cuando cambian las claves de error que la vista debe mostrar.
        /// </summary>
        public event EventHandler MessagesChanged;

        /// <summary>
        /// Se produce cuando cambia el número de intentos fallidos.
        /// </summary>
        public event EventHandler AttemptsChanged;

        /// <summary>
        /// Se produce cuando se vacían los dígitos y la vista debe enfocar la primera caja.
        /// </summary>
        public event EventHandler DigitsCleared;

        /// <summary>
        /// Se produce cuando la cuenta regresiva debe iniciar o detenerse en la vista.
        /// </summary>
        public event EventHandler CountdownStateChanged;

        /// <summary>
        /// Se produce en cada segundo de la cuenta regresiva.
        /// </summary>
        public event EventHandler CountdownUpdated;

        /// <summary>
        /// Comando que compara el código capturado con el esperado.
        /// </summary>
        public RelayCommand VerifyCodeCommand { get; }

        /// <summary>
        /// Comando que reinicia el código, los intentos y los plazos.
        /// </summary>
        public RelayCommand ResendCodeCommand { get; }

        /// <summary>
        /// Comando que solicita volver a la pantalla anterior.
        /// </summary>
        public RelayCommand BackCommand { get; }

        /// <summary>
        /// Primer dígito del código.
        /// </summary>
        public string Digit1
        {
            get { return _digit1; }
            set { SetDigit(ref _digit1, value); }
        }

        /// <summary>
        /// Segundo dígito del código.
        /// </summary>
        public string Digit2
        {
            get { return _digit2; }
            set { SetDigit(ref _digit2, value); }
        }

        /// <summary>
        /// Tercer dígito del código.
        /// </summary>
        public string Digit3
        {
            get { return _digit3; }
            set { SetDigit(ref _digit3, value); }
        }

        /// <summary>
        /// Cuarto dígito del código.
        /// </summary>
        public string Digit4
        {
            get { return _digit4; }
            set { SetDigit(ref _digit4, value); }
        }

        /// <summary>
        /// Quinto dígito del código.
        /// </summary>
        public string Digit5
        {
            get { return _digit5; }
            set { SetDigit(ref _digit5, value); }
        }

        /// <summary>
        /// Sexto dígito del código.
        /// </summary>
        public string Digit6
        {
            get { return _digit6; }
            set { SetDigit(ref _digit6, value); }
        }

        /// <summary>
        /// Intentos fallidos acumulados.
        /// </summary>
        public int FailedAttempts
        {
            get { return _failedAttempts; }
            private set { SetProperty(ref _failedAttempts, value); }
        }

        /// <summary>
        /// Segundos restantes para poder reenviar el código.
        /// </summary>
        public int ResendSecondsRemaining
        {
            get { return _remainingSecondsUntilResend; }
            private set
            {
                if (SetProperty(ref _remainingSecondsUntilResend, value))
                {
                    ResendCodeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Indica si las cajas de dígitos aceptan captura.
        /// </summary>
        public bool AreDigitsEnabled
        {
            get { return _areDigitsEnabled; }
            private set
            {
                if (SetProperty(ref _areDigitsEnabled, value))
                {
                    VerifyCodeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Indica si la cuenta regresiva está corriendo.
        /// </summary>
        public bool IsCountdownRunning
        {
            get { return _isCountdownRunning; }
            private set { SetProperty(ref _isCountdownRunning, value); }
        }

        /// <summary>
        /// Tiempo restante de vigencia del código en formato mm:ss.
        /// </summary>
        public string ExpirationText
        {
            get { return _expirationText; }
            private set { SetProperty(ref _expirationText, value); }
        }

        /// <summary>
        /// Clave del error que bloquea el formulario; null si no hay bloqueo.
        /// </summary>
        public string FormErrorKey
        {
            get { return _formErrorKey; }
            private set { SetProperty(ref _formErrorKey, value); }
        }

        /// <summary>
        /// Clave del error temporal de código incorrecto; null si no hay error.
        /// </summary>
        public string VerificationErrorKey
        {
            get { return _verificationErrorKey; }
            private set { SetProperty(ref _verificationErrorKey, value); }
        }

        /// <summary>
        /// Inicia la cuenta regresiva de vigencia y de reenvío.
        /// </summary>
        public void StartCountdown()
        {
            IsCountdownRunning = true;
            CountdownStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Avanza un segundo la cuenta regresiva. La vista lo invoca desde su temporizador.
        /// </summary>
        public void Tick()
        {
            if (_remainingSecondsUntilExpiry > TimeExpired)
            {
                _remainingSecondsUntilExpiry--;
            }

            if (ResendSecondsRemaining > TimeExpired)
            {
                ResendSecondsRemaining--;
            }

            UpdateExpirationText();
            CountdownUpdated?.Invoke(this, EventArgs.Empty);

            if (_remainingSecondsUntilExpiry == TimeExpired)
            {
                StopCountdown();
                LockForm(ExpiredCodeErrorKey);
                MessagesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ExecuteVerifyCode()
        {
            string enteredCode = GetEnteredCode();
            bool isCodeCorrect = enteredCode == SimulatedVerificationCode;

            VerificationErrorKey = null;

            if (isCodeCorrect)
            {
                StopCountdown();
                MessagesChanged?.Invoke(this, EventArgs.Empty);
                VerificationSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RecordFailedAttempt();
            }
        }

        private bool CanExecuteVerifyCode()
        {
            bool canVerify = AreDigitsEnabled && AreAllDigitsProvided();
            return canVerify;
        }

        private void ExecuteResendCode()
        {
            _remainingSecondsUntilExpiry = VerificationCodeExpirySeconds;
            ResendSecondsRemaining = CodeResendCooldownSeconds;
            FailedAttempts = InitialFailedAttempts;
            FormErrorKey = null;
            VerificationErrorKey = null;
            AreDigitsEnabled = true;

            ClearDigits();
            UpdateExpirationText();

            if (!IsCountdownRunning)
            {
                StartCountdown();
            }

            AttemptsChanged?.Invoke(this, EventArgs.Empty);
            CountdownUpdated?.Invoke(this, EventArgs.Empty);
            MessagesChanged?.Invoke(this, EventArgs.Empty);
            DigitsCleared?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteResendCode()
        {
            bool canResend = ResendSecondsRemaining <= TimeExpired;
            return canResend;
        }

        private void ExecuteBack()
        {
            StopCountdown();
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RecordFailedAttempt()
        {
            FailedAttempts++;

            bool areAttemptsExhausted = FailedAttempts >= MaximumVerificationAttempts;

            if (areAttemptsExhausted)
            {
                StopCountdown();
                LockForm(AttemptsExhaustedErrorKey);
            }
            else
            {
                VerificationErrorKey = IncorrectCodeErrorKey;
            }

            ClearDigits();

            AttemptsChanged?.Invoke(this, EventArgs.Empty);
            MessagesChanged?.Invoke(this, EventArgs.Empty);
            DigitsCleared?.Invoke(this, EventArgs.Empty);
        }

        private void LockForm(string errorKey)
        {
            FormErrorKey = errorKey;
            AreDigitsEnabled = false;
        }

        private void StopCountdown()
        {
            IsCountdownRunning = false;
            CountdownStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetDigit(ref string digitField, string newValue)
        {
            if (SetProperty(ref digitField, newValue))
            {
                VerifyCodeCommand.NotifyCanExecuteChanged();
            }
        }

        private string[] GetDigits()
        {
            string[] digits = { Digit1, Digit2, Digit3, Digit4, Digit5, Digit6 };
            return digits;
        }

        private string GetEnteredCode()
        {
            string enteredCode = string.Empty;

            foreach (string digit in GetDigits())
            {
                enteredCode += digit;
            }

            return enteredCode;
        }

        private bool AreAllDigitsProvided()
        {
            bool areAllProvided = true;

            foreach (string digit in GetDigits())
            {
                if (string.IsNullOrEmpty(digit))
                {
                    areAllProvided = false;
                }
            }

            return areAllProvided;
        }

        private void ClearDigits()
        {
            Digit1 = string.Empty;
            Digit2 = string.Empty;
            Digit3 = string.Empty;
            Digit4 = string.Empty;
            Digit5 = string.Empty;
            Digit6 = string.Empty;
        }

        private void UpdateExpirationText()
        {
            int minutes = _remainingSecondsUntilExpiry / SecondsPerMinute;
            int seconds = _remainingSecondsUntilExpiry % SecondsPerMinute;

            ExpirationText = string.Format(TimerFormat, minutes, seconds);
        }
    }
}