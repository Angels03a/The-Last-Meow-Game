using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TLMG.Views
{
    public partial class LoginScreen : UserControl
    {
        public LoginScreen()
        {
            InitializeComponent();
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

        private async void ShowStatusMessage(TextBlock messageElement, string messageContent, bool isErrorMessage = true)
        {
            messageElement.Text = messageContent;
            messageElement.Foreground = isErrorMessage
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            messageElement.Visibility = Visibility.Visible;

            await Task.Delay(4000);

            if (messageElement.Text == messageContent)
            {
                messageElement.Visibility = Visibility.Hidden;
            }
        }

        private async void BtnLogIn_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            string inputEmailAddress = txtEmail.Text;
            string inputPasswordText = pwdPassword.Visibility == Visibility.Visible ? pwdPassword.Password : txtPasswordVisible.Text;

            lblEmailError.Visibility = Visibility.Hidden;

            if (string.IsNullOrWhiteSpace(inputEmailAddress) || string.IsNullOrWhiteSpace(inputPasswordText))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_IncompleteFieldsError") as string, true);
            }
            else
            {
                string emailValidationResult = ValidateEmail(inputEmailAddress);
                if (emailValidationResult != null)
                {
                    ShowStatusMessage(lblEmailError, emailValidationResult, true);
                }
                else
                {
                    ShowStatusMessage(lblFormMessage, TryFindResource("Login_SuccessMessage") as string, false);
                    await Task.Delay(1000);
                    MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                    applicationMainWindow.ShowScreen(new MainMenuScreen());
                }
            }
        }

        private void BtnPlayAsGuest_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ShowStatusMessage(lblFormMessage, "Modo invitado: la pantalla de Menú Principal aún no está construida.", false);
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