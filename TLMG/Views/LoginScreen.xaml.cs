using System;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TLMG.DatabaseModel;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de inicio de sesión de usuarios y acceso de invitados.
    /// Integrada con Entity Framework para validación de credenciales en base de datos.
    /// </summary>
    public partial class LoginScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 1000;

        public LoginScreen()
        {
            InitializeComponent();
        }

        private async void BtnLogIn_Click(object eventSender, RoutedEventArgs eventArguments)
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
                    await Task.Delay(ScreenTransitionDelayMilliseconds);

                    MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                    applicationMainWindow.ShowScreen(new MainMenuScreen());
                }
            }
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
                    var registeredUser = database.User.FirstOrDefault(user => user.email == email);

                    if (registeredUser == null || registeredUser.password_hash.Trim() != password.Trim())
                    {
                        ShowStatusMessage(lblFormMessage, TryFindResource("Login_InvalidCredentialsError") as string, true);
                    }
                    else
                    {
                        registeredUser.last_time = DateTime.Now;
                        database.SaveChanges();
                        isAuthenticationSuccessful = true;
                    }
                }
            }
            catch (SqlException)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_NetworkError") as string, true);
            }
            catch (EntityException)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_DataModelError") as string, true);
            }

            return isAuthenticationSuccessful;
        }

        private string ValidateEmail(string emailAddress)
        {
            string errorResourceKey = ValidationHelper.GetEmailValidationErrorKey(emailAddress, "Login");
            return errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
        }

        private void BtnShowPassword_Click(object eventSender, RoutedEventArgs eventArguments)
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

        private async void ShowStatusMessage(
            TextBlock messageElement,
            string messageContent,
            bool isErrorMessage = true)
        {
            messageElement.Text = messageContent;
            messageElement.Foreground = isErrorMessage
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            messageElement.Visibility = Visibility.Visible;

            await Task.Delay(ValidationHelper.StatusMessageDurationMilliseconds);

            if (messageElement.Text == messageContent)
            {
                messageElement.Visibility = Visibility.Hidden;
            }
        }

        private void BtnPlayAsGuest_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }

        private void BtnForgotPassword_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new ForgotPasswordScreen());
        }

        private void BtnTabCreateAccount_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new CreateAccountScreen());
        }
    }
}