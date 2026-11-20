using System;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Communication;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.Validation;

namespace TLMG.Client.ViewModels
{
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

        public ForgotPasswordViewModel(INavigationManager navigationManager, IPasswordConnection passwordConnection)
        {
            _navigationManager = navigationManager;
            _passwordConnection = passwordConnection;

            EmailError = new StatusMessage();
            FormMessage = new StatusMessage();

            _sendCodeCommand = new RelayCommand(ExecuteSendCode, CanExecuteSendCode);
            _backCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
        }

        public ICommand SendCodeCommand => _sendCodeCommand;

        public ICommand BackCommand => _backCommand;

        public StatusMessage EmailError { get; }

        public StatusMessage FormMessage { get; }

        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

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

        public void ValidateEmailOnly()
        {
            EmailError.Hide();

            if (!string.IsNullOrWhiteSpace(Email))
            {
                string emailErrorKey = InputValidator.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);

                if (emailErrorKey != null)
                {
                    EmailError.ShowError(LanguageManager.FindText(emailErrorKey));
                }
            }
        }

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
                string emailErrorKey = InputValidator.GetEmailValidationErrorKey(Email, ScreenResourcePrefix);

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