using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Threading;
using log4net;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla de inicio de sesión de usuarios y acceso de invitados.
    /// Envía las credenciales al servidor autoritativo y reacciona al resultado.
    /// </summary>
    public partial class LoginScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 800;
        private const string SuccessMessageKey = "Login_SuccessMessage";
        private const string ConnectionErrorKey = "Login_NetworkError";
        private const string UnknownErrorKey = "Login_DataModelError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(LoginScreen));

        private DispatcherTimer _redirectTimer;
        private UsersManagerClient _logInClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginScreen"/>.
        /// </summary>
        public LoginScreen()
        {
            InitializeComponent();
            Unloaded += OnScreenUnloaded;

            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ScreenTransitionDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _redirectTimer?.Stop();
            CloseLogInClient();
        }

        private void BtnLogIn_Click(object sender, RoutedEventArgs e)
        {
            string inputEmailAddress = txtEmail.Text;
            string inputPasswordText = pwdPassword.Visibility == Visibility.Visible
                ? pwdPassword.Password
                : txtPasswordVisible.Text;

            lblEmailError.Visibility = Visibility.Hidden;

            if (AreCredentialsValidFormat(inputEmailAddress, inputPasswordText))
            {
                SendLogInRequest(inputEmailAddress, inputPasswordText);
            }
        }

        private void OnRedirectTimerTick(object sender, EventArgs e)
        {
            _redirectTimer.Stop();

            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());

            _log.Info("Se inició sesión correctamente.");
        }

        private bool AreCredentialsValidFormat(string email, string password)
        {
            bool areCredentialsValid = true;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_IncompleteFieldsError") as string, true);
                areCredentialsValid = false;
            }
            else
            {
                string emailValidationResult = ValidateEmail(email);
                if (emailValidationResult != null)
                {
                    ShowStatusMessage(lblEmailError, emailValidationResult, true);
                    areCredentialsValid = false;
                }
            }

            return areCredentialsValid;
        }

        private void SendLogInRequest(string email, string password)
        {
            OperationLogInRequest operationLogInRequest = new OperationLogInRequest
            {
                Email = email,
                Password = password
            };

            btnLogIn.IsEnabled = false;

            try
            {
                _logInClient = new UsersManagerClient();

                OperationLogInResult logInResult = _logInClient.LogIn(operationLogInRequest);

                FinishLogInAttempt(logInResult);
            }
            catch (FaultException<SystemFault> systemFault)
            {
                _log.Fatal("Error de infraestructura reportado por el servidor.", systemFault);

                string errorMessage = TryFindResource(systemFault.Detail.SystemErrorMessage) as string
                                   ?? TryFindResource(UnknownErrorKey) as string;
                ShowStatusMessage(lblFormMessage, errorMessage, true);

                CloseLogInClient();
                btnLogIn.IsEnabled = true;
            }
            catch (EndpointNotFoundException endpointNotFoundException)
            {
                _log.Error("No se encontró el endpoint del servidor al iniciar sesión.", 
                    endpointNotFoundException);
                ShowConnectionError();
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("El servidor tardó demasiado en recibir la solicitud de inicio de sesión.", 
                    timeoutException);
                ShowConnectionError();
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al enviar la solicitud de inicio de sesión.",
                    communicationException);
                ShowConnectionError();
            }
        }

        private void FinishLogInAttempt(OperationLogInResult operationLogInResult)
        {
            CloseLogInClient();
            btnLogIn.IsEnabled = true;

            if (operationLogInResult.IsSuccess)
            {
                StartSessionAndRedirect(operationLogInResult);
            }
            else
            {
                string resourceKey = operationLogInResult.ErrorKey ?? UnknownErrorKey;
                ShowStatusMessage(lblFormMessage, TryFindResource(resourceKey) as string, true);
            }
        }

        private void StartSessionAndRedirect(OperationLogInResult operationLogInResult)
        {
            if (((App)Application.Current).ServerConnection.StartRegisteredSession(operationLogInResult))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource(SuccessMessageKey) as string, false);
                _redirectTimer.Start();
            }
            else
            {
                ShowStatusMessage(lblFormMessage, TryFindResource(ConnectionErrorKey) as string, true);
            }
        }

        private void ShowConnectionError()
        {
            CloseLogInClient();
            btnLogIn.IsEnabled = true;
            ShowStatusMessage(lblFormMessage, TryFindResource(ConnectionErrorKey) as string, true);
        }

        private void CloseLogInClient()
        {
            if (_logInClient != null)
            {
                try
                {
                    if (_logInClient.State == CommunicationState.Faulted)
                    {
                        _logInClient.Abort();
                    }
                    else
                    {
                        _logInClient.Close();
                    }
                }
                catch (CommunicationException communicationException)
                {
                    _log.Warn("No se pudo cerrar limpiamente el canal de inicio de sesión.", 
                        communicationException);
                    _logInClient.Abort();
                }

                _logInClient = null;
            }
        }

        private string ValidateEmail(string emailAddress)
        {
            string errorResourceKey = ValidationHelper.GetEmailValidationErrorKey(emailAddress, "Login");
            string errorMessage = errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
            return errorMessage;
        }

        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPasswordVisible.Visibility == Visibility.Collapsed)
            {
                txtPasswordVisible.Text = pwdPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                pwdPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                pwdPassword.Password = txtPasswordVisible.Text;
                pwdPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnPlayAsGuest_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }

        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new ForgotPasswordScreen());
        }

        private void BtnTabCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new CreateAccountScreen());
        }
    }
}