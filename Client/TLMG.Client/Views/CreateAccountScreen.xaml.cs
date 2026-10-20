using System;
<<<<<<< HEAD
using System.ServiceModel;
=======
>>>>>>> dev-stella
using System.Windows;
using System.ServiceModel;
using log4net;
<<<<<<< HEAD

using TLMG.Client.Communication;
using TLMG.Client.TLMGService;
=======
using TLMG.Client.AccountRegistrationServiceReference;
>>>>>>> dev-stella

namespace TLMG.Client.Views
{
    /// <summary>
<<<<<<< HEAD
    /// Pantalla de registro de cuentas; valida el formulario y envía el registro al servidor.
=======
    /// Pantalla de registro de cuentas. Solo captura datos, invoca al servidor autoritativo
    /// y reacciona al resultado recibido por callback.
>>>>>>> dev-stella
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public partial class CreateAccountScreen : LocalizableScreen, IAccountRegistrationServiceCallback
    {
<<<<<<< HEAD
        private const string NicknameFormatErrorKey = "CreateAccount_NicknameFormatError";
        private const string NicknameTakenErrorKey = "CreateAccount_NicknameTakenError";
        private const string EmailFormatErrorKey = "CreateAccount_EmailFormatError";
        private const string EmailTakenErrorKey = "CreateAccount_EmailTakenError";
        private const string PasswordPolicyErrorKey = "CreateAccount_PasswordPolicyError";
        private const string ConnectionErrorKey = "CreateAccount_NetworkError";
        private const string UnknownErrorKey = "CreateAccount_DataModelError";
=======
        private const bool IsErrorMessage = true;

        private const string MismatchErrorKey = "CreateAccount_MismatchError";
        private const string NicknameErrorKey = "CreateAccount_NicknameFormatError";
        private const string NicknameTakenErrorKey = "CreateAccount_NicknameTakenError";
        private const string EmailErrorKey = "CreateAccount_EmailFormatError";
        private const string EmailTakenErrorKey = "CreateAccount_EmailTakenError";
        private const string PasswordErrorKey = "CreateAccount_PasswordPolicyError";
        private const string UnknownErrorKey = "CreateAccount_DataModelError";
        private const string ConnectionErrorKey = "CreateAccount_NetworkError";

        private const string SuccessMessageKey = "CreateAccount_SuccessMessage";
        private const string ConfirmAbandonMessageKey = "CreateAccount_ConfirmAbandonMessage";
        private const string ConfirmAbandonTitleKey = "CreateAccount_ConfirmAbandonTitle";
>>>>>>> dev-stella

        private static readonly ILog _log = LogManager.GetLogger(typeof(CreateAccountScreen));
        private AccountRegistrationServiceClient _accountRegistrationClient;

        private AccountRegistrationServiceClient _registrationClient;
        private AccountRegistrationServiceCallback _registrationCallback;

        public CreateAccountScreen()
        {
            InitializeComponent();
            Unloaded += OnScreenUnloaded;
        }

        /// <summary>
        /// Recibe del servidor el resultado del registro y lo traslada al hilo de la interfaz.
        /// </summary>
        /// <param name="operationRegisterPlayerAccountResult">Resultado con éxito o clave de error.</param>
        public void RegisterPlayerAccountResult(
            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult)
        {
            Dispatcher.BeginInvoke(new Action(() => HandleRegistrationResult(operationRegisterPlayerAccountResult)));
        }

        private void PwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            lblPasswordError.Visibility = Visibility.Hidden;
        }

        private void BtnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            HideAllMessages();

            var formDataForRegistration = new RegistrationFormData
=======
            ClearErrorMessages();

            bool doPasswordsMatch = pwdPassword.Password == pwdConfirmPassword.Password;

            if (doPasswordsMatch)
            {
                SendRegistrationRequest();
            }
            else
            {
                ShowStatusMessage(lblConfirmPasswordError, FindMessage(MismatchErrorKey), IsErrorMessage);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                FindMessage(ConfirmAbandonMessageKey),
                FindMessage(ConfirmAbandonTitleKey),
                MessageBoxButton.YesNo);

            bool hasConfirmedAbandon = confirmationResult == MessageBoxResult.Yes;

            if (hasConfirmedAbandon)
            {
                CloseRegistrationClient();
                ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
            }
        }

        private void SendRegistrationRequest()
        {
            PlayerAccountRegistration playerAccountRegistration = new PlayerAccountRegistration
>>>>>>> dev-stella
            {
                Nickname = txtNickname.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Password = pwdPassword.Password,
                HasAcceptedTerms = chkTerms.IsChecked == true
            };

<<<<<<< HEAD
            if (AreFieldsValid(formDataForRegistration))
            {
                SendRegistration(formDataForRegistration);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                TryFindResource("CreateAccount_ConfirmAbandonMessage") as string,
                TryFindResource("CreateAccount_ConfirmAbandonTitle") as string,
                MessageBoxButton.YesNo);

            if (confirmationResult == MessageBoxResult.Yes)
            {
                ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
            }
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            CleanUpClient();
        }

        private void HideAllMessages()
        {
            lblNicknameError.Visibility = Visibility.Hidden;
            lblEmailError.Visibility = Visibility.Hidden;
            lblPasswordError.Visibility = Visibility.Hidden;
            lblConfirmPasswordError.Visibility = Visibility.Hidden;
            lblFormMessage.Visibility = Visibility.Hidden;
        }

        private bool AreFieldsValid(RegistrationFormData formDataForRegistration)
        {
            bool areAllFieldsValid;

            if (AreFieldsEmpty(formDataForRegistration))
            {
                areAllFieldsValid = false;
            }
            else if (HasFormatErrors(formDataForRegistration))
            {
                areAllFieldsValid = false;
            }
            else
            {
                areAllFieldsValid = true;
            }

            return areAllFieldsValid;
        }

        private bool AreFieldsEmpty(RegistrationFormData formDataForRegistration)
        {
            bool hasEmptyFields = string.IsNullOrWhiteSpace(formDataForRegistration.Nickname) ||
                string.IsNullOrWhiteSpace(formDataForRegistration.Email) ||
                string.IsNullOrWhiteSpace(formDataForRegistration.Password) ||
                string.IsNullOrWhiteSpace(formDataForRegistration.ConfirmPassword);

            if (hasEmptyFields)
            {
                ShowStatusMessage(
                    lblFormMessage,
                    TryFindResource("CreateAccount_IncompleteFieldsError") as string,
                    true);
            }

            return hasEmptyFields;
        }

        private bool HasFormatErrors(RegistrationFormData formDataForRegistration)
        {
            bool hasErrors = false;

            string nicknameValidationResult = ValidateNickname(formDataForRegistration.Nickname);
            if (nicknameValidationResult != null)
            {
                ShowStatusMessage(lblNicknameError, nicknameValidationResult, true);
                hasErrors = true;
            }

            string emailValidationResult = ValidateEmailFormat(formDataForRegistration.Email);
            if (emailValidationResult != null)
            {
                ShowStatusMessage(lblEmailError, emailValidationResult, true);
                hasErrors = true;
            }

            if (CalculatePasswordStrength(formDataForRegistration.Password)
                < ValidationHelper.RequiredPasswordStrengthScore)
            {
                ShowStatusMessage(
                    lblPasswordError,
                    TryFindResource("CreateAccount_PasswordPolicyError") as string,
                    true);
                hasErrors = true;
            }
            else if (formDataForRegistration.Password != formDataForRegistration.ConfirmPassword)
            {
                ShowStatusMessage(
                    lblConfirmPasswordError,
                    TryFindResource("CreateAccount_MismatchError") as string,
                    true);
                hasErrors = true;
            }

            if (!formDataForRegistration.HasAcceptedTerms)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_TermsError") as string, true);
                hasErrors = true;
            }

            return hasErrors;
        }

        private void SendRegistration(RegistrationFormData formDataForRegistration)
        {
            RegistrationData registration = new RegistrationData
            {
                Nickname = formDataForRegistration.Nickname,
                Email = formDataForRegistration.Email,
                Password = formDataForRegistration.Password,
                HasAcceptedTerms = formDataForRegistration.HasAcceptedTerms
            };

=======
>>>>>>> dev-stella
            btnCreateAccount.IsEnabled = false;

            try
            {
<<<<<<< HEAD
                _registrationCallback = new AccountRegistrationServiceCallback();
                _registrationCallback.RegistrationResultReceived += OnRegistrationResultReceived;

                InstanceContext context = new InstanceContext(_registrationCallback);

                _registrationClient = new AccountRegistrationServiceClient(context);
                _registrationClient.InnerChannel.Faulted += OnClientChannelFaulted;
                _registrationClient.Register(registration);
            }
            catch (EndpointNotFoundException ex)
            {
                HandleConnectionFailure(ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                HandleConnectionFailure(ex);
            }
            catch (CommunicationException ex)
            {
                HandleConnectionFailure(ex);
            }
            catch (TimeoutException ex)
            {
                HandleConnectionFailure(ex);
            }
            catch (InvalidOperationException ex)
            {
                HandleConnectionFailure(ex);
            }
        }

        private void HandleConnectionFailure(Exception exception)
        {
            _log.Error("No se pudo comunicar con el servidor durante el registro.", exception);

            CleanUpClient();
            btnCreateAccount.IsEnabled = true;
            ShowConnectionError();
        }

        private void OnRegistrationResultReceived(object sender, RegistrationResultEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => ShowRegistrationResult(e.Result)));
        }

        private void OnClientChannelFaulted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ShowConnectionLost));
        }

        private void ShowRegistrationResult(OperationResultData result)
        {
            CleanUpClient();
            btnCreateAccount.IsEnabled = true;

            if (result.IsSuccess)
            {
                NavigateToVerificationScreen();
            }
            else
            {
                ShowServerError(result.ErrorKey);
            }
        }

        private void ShowConnectionLost()
        {
            _log.Warn("El canal con el servidor falló antes de recibir la respuesta del registro.");

            CleanUpClient();
            btnCreateAccount.IsEnabled = true;
            ShowConnectionError();
        }

        private void ShowConnectionError()
        {
            ShowStatusMessage(lblFormMessage, TryFindResource(ConnectionErrorKey) as string, true);
        }

        private void ShowServerError(string errorKey)
        {
            string safeErrorKey = errorKey ?? UnknownErrorKey;
            string message = TryFindResource(safeErrorKey) as string ?? safeErrorKey;

            switch (safeErrorKey)
            {
                case NicknameFormatErrorKey:
                case NicknameTakenErrorKey:
                    ShowStatusMessage(lblNicknameError, message, true);
                    break;
                case EmailFormatErrorKey:
                case EmailTakenErrorKey:
                    ShowStatusMessage(lblEmailError, message, true);
                    break;
                case PasswordPolicyErrorKey:
                    ShowStatusMessage(lblPasswordError, message, true);
                    break;
                default:
                    ShowStatusMessage(lblFormMessage, message, true);
                    break;
            }
        }

        private void CleanUpClient()
        {
            if (_registrationCallback != null)
            {
                _registrationCallback.RegistrationResultReceived -= OnRegistrationResultReceived;
                _registrationCallback = null;
            }

            if (_registrationClient != null)
            {
                _registrationClient.InnerChannel.Faulted -= OnClientChannelFaulted;
                CloseClient(_registrationClient);
                _registrationClient = null;
            }
        }

        private void CloseClient(AccountRegistrationServiceClient client)
        {
            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
            catch (CommunicationException ex)
            {
                _log.Warn("El canal no se cerró limpiamente.", ex);
                client.Abort();
            }
            catch (TimeoutException ex)
            {
                _log.Warn("Se agotó el tiempo al cerrar el canal.", ex);
                client.Abort();
            }
=======
                _accountRegistrationClient = new AccountRegistrationServiceClient(new InstanceContext(this));

                _accountRegistrationClient.RegisterPlayerAccount(playerAccountRegistration);
            }
            catch (EndpointNotFoundException ex)
            {
                _log.Error("No se encontró el endpoint del servidor al registrar la cuenta.", ex);
                HandleConnectionFailure();
            }
            catch (TimeoutException ex)
            {
                _log.Error("El servidor tardó demasiado en recibir la solicitud de registro.", ex);
                HandleConnectionFailure();
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al enviar la solicitud de registro.", ex);
                HandleConnectionFailure();
            }
        }

        private void HandleRegistrationResult(OperationRegisterPlayerAccountResult registrationResult)
        {
            CloseRegistrationClient();
            btnCreateAccount.IsEnabled = true;

            if (registrationResult.IsSuccess)
            {
                NavigateToVerificationScreen();
            }
            else
            {
                ShowRegistrationError(registrationResult.ErrorKey);
            }
        }

        private void HandleConnectionFailure()
        {
            CloseRegistrationClient();
            btnCreateAccount.IsEnabled = true;
            ShowStatusMessage(lblFormMessage, FindMessage(ConnectionErrorKey), IsErrorMessage);
        }

        private void ShowRegistrationError(string errorKey)
        {
            string resourceKey = errorKey ?? UnknownErrorKey;
            string localizedMessage = FindMessage(resourceKey);

            switch (resourceKey)
            {
                case NicknameErrorKey:
                case NicknameTakenErrorKey:
                    ShowStatusMessage(lblNicknameError, localizedMessage, IsErrorMessage);
                    break;
                case EmailErrorKey:
                case EmailTakenErrorKey:
                    ShowStatusMessage(lblEmailError, localizedMessage, IsErrorMessage);
                    break;
                case PasswordErrorKey:
                    ShowStatusMessage(lblPasswordError, localizedMessage, IsErrorMessage);
                    break;
                default:
                    ShowStatusMessage(lblFormMessage, localizedMessage, IsErrorMessage);
                    break;
            }
        }

        private void ClearErrorMessages()
        {
            lblFormMessage.Visibility = Visibility.Hidden;
            lblNicknameError.Visibility = Visibility.Hidden;
            lblEmailError.Visibility = Visibility.Hidden;
            lblPasswordError.Visibility = Visibility.Hidden;
            lblConfirmPasswordError.Visibility = Visibility.Hidden;
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
>>>>>>> dev-stella
        }

        private void NavigateToVerificationScreen()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new VerificationScreen(
                onVerifiedCallbackAction: HandleVerificationCompleted,
                onBackCallbackAction: () => applicationMainWindow.ShowScreen(new CreateAccountScreen())
            ));
        }

        private void HandleVerificationCompleted()
        {
            MessageBox.Show(FindMessage(SuccessMessageKey));
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new LoginScreen());
        }

        private string FindMessage(string resourceKey)
        {
            string localizedMessage = TryFindResource(resourceKey) as string;

<<<<<<< HEAD
        private string ValidateNickname(string inputNicknameString)
        {
            string errorResourceKey = ValidationHelper.GetNicknameValidationErrorKey(inputNicknameString);
            return errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
        }

        private string ValidateEmailFormat(string inputEmailAddress)
        {
            string validationErrorKey = ValidationHelper.GetEmailValidationErrorKey(inputEmailAddress, "CreateAccount");
            return validationErrorKey != null ? TryFindResource(validationErrorKey) as string : null;
        }

        private class RegistrationFormData
        {
            public string Nickname { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
            public bool HasAcceptedTerms { get; set; }
=======
            return localizedMessage;
>>>>>>> dev-stella
        }
    }
}