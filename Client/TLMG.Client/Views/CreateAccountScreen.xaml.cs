using System;
using System.Windows;
using System.ServiceModel;
using log4net;
using TLMG.Client.AccountRegistrationServiceReference;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de registro de cuentas. Solo captura datos, invoca al servidor autoritativo
    /// y reacciona al resultado recibido por callback.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public partial class CreateAccountScreen : LocalizableScreen, IAccountRegistrationServiceCallback
    {
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

        private static readonly ILog _log = LogManager.GetLogger(typeof(CreateAccountScreen));
        private AccountRegistrationServiceClient _accountRegistrationClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountScreen"/>.
        /// </summary>
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

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            CloseRegistrationClient();
        }

        private void SendRegistrationRequest()
        {
            PlayerAccountRegistration playerAccountRegistration = new PlayerAccountRegistration
            {
                Nickname = txtNickname.Text,
                Email = txtEmail.Text,
                Password = pwdPassword.Password,
                HasAcceptedTerms = chkTerms.IsChecked == true
            };

            btnCreateAccount.IsEnabled = false;

            try
            {
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

            return localizedMessage;
        }
    }
}