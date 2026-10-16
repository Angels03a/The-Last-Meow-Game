using System;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

using log4net;

using TLMG.DatabaseModel;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Representa la pantalla de inicio de sesión de usuarios y acceso de invitados.
    /// Integrada con Entity Framework para validación de credenciales en base de datos.
    /// </summary>
    public partial class LoginScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 1000;
        private static readonly ILog _log = LogManager.GetLogger(typeof(LoginScreen));

        private DispatcherTimer _redirectTimer;

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
                bool isLoginSuccessful = TryAuthenticateUser(inputEmailAddress, inputPasswordText);

                if (isLoginSuccessful)
                {
                    ShowStatusMessage(lblFormMessage, TryFindResource("Login_SuccessMessage") as string, false);
                    _redirectTimer.Start();
                }
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

        private bool TryAuthenticateUser(string email, string password)
        {
            bool isAuthenticationSuccessful = false;

            try
            {
                using (var database = new TLMG_DBEntities())
                {
                    User registeredUser = database.User.FirstOrDefault(user => user.email == email);

                    if (registeredUser == null || registeredUser.password_hash.Trim() != password.Trim())
                    {
                        ShowStatusMessage(
                            lblFormMessage, TryFindResource("Login_InvalidCredentialsError") as string, true);
                    }
                    else
                    {
                        registeredUser.last_time = DateTime.Now;
                        database.SaveChanges();
                        isAuthenticationSuccessful = true;
                    }
                }
            }
            catch (SqlException ex)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_NetworkError") as string, true);
                _log.Error("Fallo en la conexión de la base de datos", ex);
            }
            catch (EntityException ex)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_DataModelError") as string, true);
                _log.Error("Fallo en encontrar el modelo de datos", ex);
            }

            return isAuthenticationSuccessful;
        }

        private string ValidateEmail(string emailAddress)
        {
            string errorResourceKey = ValidationHelper.GetEmailValidationErrorKey(emailAddress, "Login");
            return errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
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