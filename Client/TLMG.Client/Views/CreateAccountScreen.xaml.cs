using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de registro de cuentas.
    /// Delega la lógica de captura al ViewModel y gestiona la llamada a WCF.
    /// </summary>
    public partial class CreateAccountScreen : LocalizableScreen
    {
        private const bool IsErrorMessage = true;
        private const string SuccessMessageKey = "CreateAccount_SuccessMessage";
        private const string ConfirmAbandonMessageKey = "CreateAccount_ConfirmAbandonMessage";
        private const string ConfirmAbandonTitleKey = "CreateAccount_ConfirmAbandonTitle";

        private static readonly ILog _log = LogManager.GetLogger(typeof(CreateAccountScreen));

        private readonly CreateAccountViewModel _viewModel;
        private UsersManagerClient _accountRegistrationClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountScreen"/>.
        /// </summary>
        public CreateAccountScreen()
        {
            InitializeComponent();

            _viewModel = new CreateAccountViewModel();
            DataContext = _viewModel;

            _viewModel.RegistrationRequested += OnRegistrationRequested;
            _viewModel.ValidationFailed += OnValidationFailed;
            _viewModel.BackRequested += OnBackRequested;

            Unloaded += OnScreenUnloaded;
        }

        private void PwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = pwdPassword.Password;
            lblPasswordError.Visibility = Visibility.Collapsed;
        }

        private void PwdConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ConfirmPassword = pwdConfirmPassword.Password;
            lblConfirmPasswordError.Visibility = Visibility.Collapsed;
        }

        private void OnRegistrationRequested(object sender, EventArgs e)
        {
            ClearErrorMessages();

            _viewModel.IsBusy = true;
            Mouse.OverrideCursor = Cursors.Wait;

            Dispatcher.BeginInvoke(new Action(SendRegistrationRequest), DispatcherPriority.Render);
        }

        private void OnValidationFailed(object sender, EventArgs e)
        {
            bool hasNicknameError = _viewModel.NicknameErrorKey != null;
            if (hasNicknameError)
            {
                ShowStatusMessage(lblNicknameError, ErrorMessageHelper.FindErrorMessage(this, _viewModel.NicknameErrorKey), IsErrorMessage);
            }

            bool hasEmailError = _viewModel.EmailErrorKey != null;
            if (hasEmailError)
            {
                ShowStatusMessage(lblEmailError, ErrorMessageHelper.FindErrorMessage(this, _viewModel.EmailErrorKey), IsErrorMessage);
            }

            bool hasPasswordError = _viewModel.PasswordErrorKey != null;
            if (hasPasswordError)
            {
                ShowStatusMessage(lblPasswordError, ErrorMessageHelper.FindErrorMessage(this, _viewModel.PasswordErrorKey), IsErrorMessage);
            }

            bool hasConfirmPasswordError = _viewModel.ConfirmPasswordErrorKey != null;
            if (hasConfirmPasswordError)
            {
                ShowStatusMessage(lblConfirmPasswordError, ErrorMessageHelper.FindErrorMessage(this, _viewModel.ConfirmPasswordErrorKey), IsErrorMessage);
            }

            bool hasFormError = _viewModel.FormErrorKey != null;
            if (hasFormError)
            {
                ShowStatusMessage(lblFormMessage, ErrorMessageHelper.FindErrorMessage(this, _viewModel.FormErrorKey), IsErrorMessage);
            }
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                ErrorMessageHelper.FindErrorMessage(this, ConfirmAbandonMessageKey),
                ErrorMessageHelper.FindErrorMessage(this, ConfirmAbandonTitleKey),
                MessageBoxButton.YesNo);

            bool hasConfirmedAbandon = confirmationResult == MessageBoxResult.Yes;

            if (hasConfirmedAbandon)
            {
                CloseRegistrationClient();
                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ShowScreen(new LoginScreen());
            }
        }

        private void SendRegistrationRequest()
        {
            PlayerAccountRegistration playerAccountRegistration = new PlayerAccountRegistration
            {
                Nickname = _viewModel.Nickname,
                Email = _viewModel.Email,
                Password = _viewModel.Password,
                HasAcceptedTerms = _viewModel.HasAcceptedTerms
            };

            try
            {
                _accountRegistrationClient = new UsersManagerClient();

                OperationRegisterPlayerAccountResult registrationResult =
                    _accountRegistrationClient.RegisterPlayerAccount(playerAccountRegistration);

                FinishRegistrationAttempt(registrationResult);
            }
            catch (FaultException<SystemFault> systemFault)
            {
                _log.Fatal("Error de infraestructura reportado por el servidor.", systemFault);
                _viewModel.ApplyServerError(systemFault.Detail.SystemErrorMessage);
                CloseRegistrationClient();
                UnlockUI();
            }
            catch (EndpointNotFoundException endpointNotFoundException)
            {
                _log.Error("No se encontró el endpoint del servidor al registrar la cuenta.", endpointNotFoundException);
                ReportConnectionFailure();
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("El servidor tardó demasiado en recibir la solicitud de registro.", timeoutException);
                ReportConnectionFailure();
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al enviar la solicitud de registro.", communicationException);
                ReportConnectionFailure();
            }
        }

        private void FinishRegistrationAttempt(OperationRegisterPlayerAccountResult registrationResult)
        {
            CloseRegistrationClient();
            UnlockUI();

            if (registrationResult.IsSuccess)
            {
                NavigateToVerificationScreen();
            }
            else
            {
                _viewModel.ApplyServerError(registrationResult.ErrorKey);
            }
        }

        private void ReportConnectionFailure()
        {
            CloseRegistrationClient();
            UnlockUI();
            _viewModel.ReportConnectionFailure();
        }

        private void ClearErrorMessages()
        {
            lblFormMessage.Visibility = Visibility.Collapsed;
            lblNicknameError.Visibility = Visibility.Collapsed;
            lblEmailError.Visibility = Visibility.Collapsed;
            lblPasswordError.Visibility = Visibility.Collapsed;
            lblConfirmPasswordError.Visibility = Visibility.Collapsed;
        }

        private void CloseRegistrationClient()
        {
            if (_accountRegistrationClient != null)
            {
                try
                {
                    if (_accountRegistrationClient.State == CommunicationState.Faulted)
                    {
                        _accountRegistrationClient.Abort();
                    }
                    else
                    {
                        _accountRegistrationClient.Close();
                    }
                }
                catch (CommunicationException ex)
                {
                    _log.Warn("No se pudo cerrar limpiamente el canal de registro.", ex);
                    _accountRegistrationClient.Abort();
                }

                _accountRegistrationClient = null;
            }
        }

        private void NavigateToVerificationScreen()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new VerificationScreen(
                onVerifiedCallbackAction: ReturnToLogInAfterVerification,
                onBackCallbackAction: ReturnToCreateAccountAfterVerification
            ));
        }

        private void ReturnToLogInAfterVerification()
        {
            MessageBox.Show(ErrorMessageHelper.FindErrorMessage(this, SuccessMessageKey));
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new LoginScreen());
        }

        private void ReturnToCreateAccountAfterVerification()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new CreateAccountScreen());
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.RegistrationRequested -= OnRegistrationRequested;
            _viewModel.ValidationFailed -= OnValidationFailed;
            _viewModel.BackRequested -= OnBackRequested;

            CloseRegistrationClient();
        }

        private void UnlockUI()
        {
            Dispatcher.BeginInvoke(new Action(ReleaseUIAfterWCF), DispatcherPriority.ApplicationIdle);
        }

        private void ReleaseUIAfterWCF()
        {
            _viewModel.IsBusy = false;
            Mouse.OverrideCursor = null;
        }
    }
}