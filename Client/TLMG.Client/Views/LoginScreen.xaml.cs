using System;
using System.Windows.Input;
using System.ServiceModel;
using System.Windows;
using System.Windows.Threading;
using log4net;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la vista de la pantalla de inicio de sesión.
    /// Delega la lógica al ViewModel y gestiona la llamada a WCF.
    /// </summary>
    public partial class LoginScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 500;
        private const string SuccessMessageKey = "Login_SuccessMessage";

        private static readonly ILog _log = LogManager.GetLogger(typeof(LoginScreen));

        private readonly LoginViewModel _viewModel;
        private readonly DispatcherTimer _redirectTimer;
        private UsersManagerClient _logInClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoginScreen"/>.
        /// </summary>
        public LoginScreen()
        {
            InitializeComponent();

            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            _viewModel.LogInRequested += OnLogInRequested;
            _viewModel.ValidationFailed += OnValidationFailed;
            _viewModel.GuestPlayRequested += OnGuestPlayRequested;
            _viewModel.ForgotPasswordRequested += OnForgotPasswordRequested;
            _viewModel.CreateAccountRequested += OnCreateAccountRequested;

            Unloaded += OnScreenUnloaded;

            _redirectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ScreenTransitionDelayMilliseconds)
            };
            _redirectTimer.Tick += OnRedirectTimerTick;
        }

        private void PwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            bool isTypingHiddenPassword = !_viewModel.IsPasswordVisible;
            if (isTypingHiddenPassword)
            {
                _viewModel.Password = pwdPassword.Password;
            }
        }

        private void OnLogInRequested(object sender, EventArgs e)
        {
            lblFormMessage.Visibility = Visibility.Collapsed;
            lblEmailError.Visibility = Visibility.Collapsed;

            _viewModel.IsBusy = true;
            Mouse.OverrideCursor = Cursors.Wait;

            Dispatcher.BeginInvoke(new Action(SendLogInRequest), DispatcherPriority.Render);
        }

        private void OnValidationFailed(object sender, EventArgs e)
        {
            bool hasEmailError = _viewModel.EmailErrorKey != null;
            if (hasEmailError)
            {
                ShowStatusMessage(lblEmailError, ErrorMessageHelper.FindErrorMessage(this, _viewModel.EmailErrorKey), true);
            }

            bool hasFormError = _viewModel.FormErrorKey != null;
            if (hasFormError)
            {
                ShowStatusMessage(lblFormMessage, ErrorMessageHelper.FindErrorMessage(this, _viewModel.FormErrorKey), true);
            }
        }

        private void SendLogInRequest()
        {
            OperationLogInRequest operationLogInRequest = new OperationLogInRequest
            {
                Email = _viewModel.Email,
                Password = _viewModel.Password
            };

            try
            {
                _logInClient = new UsersManagerClient();
                OperationLogInResult logInResult = _logInClient.LogIn(operationLogInRequest);
                FinishLogInAttempt(logInResult);
            }
            catch (FaultException<SystemFault> systemFault)
            {
                _log.Fatal("Error de infraestructura reportado por el servidor.", systemFault);
                _viewModel.ApplyServerError(systemFault.Detail.SystemErrorMessage);
                CloseLogInClient();
                UnlockUI();
            }
            catch (EndpointNotFoundException endpointNotFoundException)
            {
                _log.Error("No se encontró el endpoint del servidor al iniciar sesión.", endpointNotFoundException);
                ReportConnectionFailure();
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("El servidor tardó demasiado en recibir la solicitud de inicio de sesión.", timeoutException);
                ReportConnectionFailure();
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al enviar la solicitud de inicio de sesión.", communicationException);
                ReportConnectionFailure();
            }
        }

        private void FinishLogInAttempt(OperationLogInResult operationLogInResult)
        {
            CloseLogInClient();
            UnlockUI();

            if (operationLogInResult.IsSuccess)
            {
                StartSessionAndRedirect(operationLogInResult);
            }
            else
            {
                _viewModel.ApplyServerError(operationLogInResult.ErrorKey);
            }
        }

        private void StartSessionAndRedirect(OperationLogInResult operationLogInResult)
        {
            bool wasSessionStarted = ((App)Application.Current).ServerConnection.StartRegisteredSession(operationLogInResult);

            if (wasSessionStarted)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource(SuccessMessageKey) as string, false);
                _redirectTimer.Start();
            }
            else
            {
                _viewModel.ReportConnectionFailure();
            }
        }

        private void ReportConnectionFailure()
        {
            CloseLogInClient();
            UnlockUI();
            _viewModel.ReportConnectionFailure();
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
                    _log.Warn("No se pudo cerrar limpiamente el canal de inicio de sesión.", communicationException);
                    _logInClient.Abort();
                }

                _logInClient = null;
            }
        }

        private void OnRedirectTimerTick(object sender, EventArgs e)
        {
            _redirectTimer.Stop();
            _log.Info("Se inició sesión correctamente.");

            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }

        private void OnGuestPlayRequested(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }

        private void OnForgotPasswordRequested(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
        }

        private void OnCreateAccountRequested(object sender, EventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new CreateAccountScreen());
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LogInRequested -= OnLogInRequested;
            _viewModel.ValidationFailed -= OnValidationFailed;
            _viewModel.GuestPlayRequested -= OnGuestPlayRequested;
            _viewModel.ForgotPasswordRequested -= OnForgotPasswordRequested;
            _viewModel.CreateAccountRequested -= OnCreateAccountRequested;

            _redirectTimer?.Stop();
            CloseLogInClient();
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