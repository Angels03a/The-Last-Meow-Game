using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

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

        private const string TimerFormat = "{0:00}:{1:00}";
        private const string ExpiredCodeErrorKey = "Verification_ExpiredCodeError";
        private const string AttemptsExhaustedErrorKey = "Verification_AttemptsExhaustedError";
        private const string IncorrectCodeErrorKey = "Verification_IncorrectCodeError";
        private const string UnexpectedErrorKey = "CreateAccount_DataModelError";

        private readonly IAccountsConnection _accountsConnection;
        private readonly IPasswordConnection _passwordConnection;
        private readonly VerificationNavigationParameter _navigationParameter;
        private readonly RelayCommand _verifyCodeCommand;
        private readonly RelayCommand _resendCodeCommand;

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
        private bool _isBusy;
        private string _expirationText;
        private string _formErrorKey;
        private string _verificationErrorKey;

        public VerificationViewModel()
            : this(null, null, null)
        {
        }

        public VerificationViewModel(
            IAccountsConnection accountsConnection,
            IPasswordConnection passwordConnection,
            VerificationNavigationParameter navigationParameter)
        {
            _accountsConnection = accountsConnection;
            _passwordConnection = passwordConnection;
            _navigationParameter = navigationParameter;

            _remainingSecondsUntilExpiry = VerificationCodeExpirySeconds;
            _remainingSecondsUntilResend = CodeResendCooldownSeconds;
            _failedAttempts = InitialFailedAttempts;
            _areDigitsEnabled = true;

            _verifyCodeCommand = new RelayCommand(ExecuteVerifyCode, CanExecuteVerifyCode);
            _resendCodeCommand = new RelayCommand(ExecuteResendCode, CanExecuteResendCode);
            BackCommand = new RelayCommand(ExecuteBack);

            UpdateExpirationText();
        }

        public event EventHandler VerificationSucceeded;

        public event EventHandler BackRequested;

        public event EventHandler MessagesChanged;

        public event EventHandler AttemptsChanged;

        public event EventHandler DigitsCleared;

        public event EventHandler CountdownStateChanged;

        public event EventHandler CountdownUpdated;

        public ICommand VerifyCodeCommand => _verifyCodeCommand;

        public ICommand ResendCodeCommand => _resendCodeCommand;

        public ICommand BackCommand { get; }

        public string Digit1
        {
            get { return _digit1; }
            set { SetDigit(ref _digit1, value); }
        }

        public string Digit2
        {
            get { return _digit2; }
            set { SetDigit(ref _digit2, value); }
        }

        public string Digit3
        {
            get { return _digit3; }
            set { SetDigit(ref _digit3, value); }
        }

        public string Digit4
        {
            get { return _digit4; }
            set { SetDigit(ref _digit4, value); }
        }

        public string Digit5
        {
            get { return _digit5; }
            set { SetDigit(ref _digit5, value); }
        }

        public string Digit6
        {
            get { return _digit6; }
            set { SetDigit(ref _digit6, value); }
        }

        public int FailedAttempts
        {
            get { return _failedAttempts; }
            private set { SetProperty(ref _failedAttempts, value); }
        }

        public int ResendSecondsRemaining
        {
            get { return _remainingSecondsUntilResend; }
            private set
            {
                if (SetProperty(ref _remainingSecondsUntilResend, value))
                {
                    _resendCodeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool AreDigitsEnabled
        {
            get { return _areDigitsEnabled; }
            private set
            {
                if (SetProperty(ref _areDigitsEnabled, value))
                {
                    _verifyCodeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _verifyCodeCommand.NotifyCanExecuteChanged();
                    _resendCodeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsCountdownRunning
        {
            get { return _isCountdownRunning; }
            private set { SetProperty(ref _isCountdownRunning, value); }
        }

        public string ExpirationText
        {
            get { return _expirationText; }
            private set { SetProperty(ref _expirationText, value); }
        }

        public string FormErrorKey
        {
            get { return _formErrorKey; }
            private set { SetProperty(ref _formErrorKey, value); }
        }

        public string VerificationErrorKey
        {
            get { return _verificationErrorKey; }
            private set { SetProperty(ref _verificationErrorKey, value); }
        }

        public void StartCountdown()
        {
            IsCountdownRunning = true;
            CountdownStateChanged?.Invoke(this, EventArgs.Empty);
        }

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
            VerificationErrorKey = null;
            FormErrorKey = null;

            if (_accountsConnection == null || _navigationParameter == null)
            {
                FormErrorKey = UnexpectedErrorKey;
                MessagesChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (_navigationParameter.VerificationFlow == VerificationFlow.AccountRegistration)
            {
                VerifyRegistrationCode(enteredCode);
            }
            else if (_navigationParameter.VerificationFlow == VerificationFlow.PasswordRecovery)
            {
                VerifyRecoveryCode(enteredCode);
            }
        }

        private void VerifyRegistrationCode(string enteredCode)
        {
            IsBusy = true;

            OperationResult registrationResult =
                _accountsConnection.CompletePlayerAccountRegistration(
                    _navigationParameter.PendingRegistration,
                    enteredCode);

            IsBusy = false;
            HandleVerificationResult(registrationResult);
        }

        private bool CanExecuteVerifyCode()
        {
            bool canVerify = !IsBusy && AreDigitsEnabled && AreAllDigitsProvided();
            return canVerify;
        }

        private void ExecuteResendCode()
        {
            if (_accountsConnection != null && _navigationParameter != null)
            {
                VerificationErrorKey = null;
                FormErrorKey = null;
                MessagesChanged?.Invoke(this, EventArgs.Empty);

                IsBusy = true;

                OperationResult resendResult =
                    _accountsConnection.ResendAccountVerificationCode(
                        _navigationParameter.TargetEmail,
                        LanguageManager.CurrentCulture);

                IsBusy = false;

                if (resendResult.IsSuccess)
                {
                    ResetVerificationStateAfterResend();
                }
                else
                {
                    FormErrorKey = resendResult.ErrorKey ?? UnexpectedErrorKey;
                    MessagesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ResetVerificationStateAfterResend()
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
            bool canResend = !IsBusy && ResendSecondsRemaining <= TimeExpired;
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

            if (FailedAttempts >= MaximumVerificationAttempts)
            {
                ExhaustVerificationAttempts();
            }
            else
            {
                VerificationErrorKey = IncorrectCodeErrorKey;
                ClearDigits();
                AttemptsChanged?.Invoke(this, EventArgs.Empty);
                MessagesChanged?.Invoke(this, EventArgs.Empty);
                DigitsCleared?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ExhaustVerificationAttempts()
        {
            FailedAttempts = MaximumVerificationAttempts;
            StopCountdown();
            LockForm(AttemptsExhaustedErrorKey);
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
                _verifyCodeCommand.NotifyCanExecuteChanged();
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

        private void VerifyRecoveryCode(string enteredCode)
        {
            IsBusy = true;

            OperationResult recoveryResult = _passwordConnection.VerifyPasswordResetCode(
                _navigationParameter.TargetEmail,
                enteredCode);

            IsBusy = false;

            if (recoveryResult.IsSuccess)
            {
                _navigationParameter.VerifiedCode = enteredCode;
            }

            HandleVerificationResult(recoveryResult);
        }

        private void HandleVerificationResult(OperationResult verificationResult)
        {
            if (verificationResult.IsSuccess)
            {
                StopCountdown();
                MessagesChanged?.Invoke(this, EventArgs.Empty);
                VerificationSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else if (verificationResult.ErrorKey == IncorrectCodeErrorKey)
            {
                RecordFailedAttempt();
            }
            else if (verificationResult.ErrorKey == AttemptsExhaustedErrorKey)
            {
                ExhaustVerificationAttempts();
            }
            else if (verificationResult.ErrorKey == ExpiredCodeErrorKey)
            {
                StopCountdown();
                LockForm(ExpiredCodeErrorKey);
                MessagesChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                FormErrorKey = verificationResult.ErrorKey ?? UnexpectedErrorKey;
                MessagesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}