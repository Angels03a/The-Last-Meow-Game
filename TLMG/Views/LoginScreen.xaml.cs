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
        private static readonly string[] _allowedDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };

        public LoginScreen()
        {
            InitializeComponent();
        }

        private string ValidateEmail(string email)
        {
            string resultMessage = null;

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (Regex.IsMatch(email, @"[%&]"))
                {
                    resultMessage = (string)Application.Current.Resources["Login_EmailCharactersError"];
                }
                else if (!email.Contains("@") || !email.Contains("."))
                {
                    resultMessage = (string)Application.Current.Resources["Login_EmailFormatError"];
                }
                else
                {
                    string domain = email.Substring(email.IndexOf('@') + 1).ToLower();
                    bool isDomainValid = false;

                    foreach (string allowedDomain in _allowedDomains)
                    {
                        if (domain == allowedDomain)
                        {
                            isDomainValid = true;
                            break;
                        }
                    }

                    if (!isDomainValid)
                    {
                        resultMessage = (string)Application.Current.Resources["Login_EmailDomainError"];
                    }
                }
            }

            return resultMessage;
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

        private async void ShowStatusMessage(TextBlock label, string message, bool isError = true)
        {
            label.Text = message;
            label.Foreground = isError
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            label.Visibility = Visibility.Visible;

            await Task.Delay(4000);

            if (label.Text == message)
            {
                label.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLogIn_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            string password = pwdPassword.Visibility == Visibility.Visible ? pwdPassword.Password : txtPasswordVisible.Text;

            lblEmailError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatusMessage(lblFormMessage, (string)Application.Current.Resources["Login_IncompleteFieldsError"], true);
            }
            else
            {
                string emailErrorMessage = ValidateEmail(email);
                if (emailErrorMessage != null)
                {
                    ShowStatusMessage(lblEmailError, emailErrorMessage, true);
                }
                else
                {
                    ShowStatusMessage(lblFormMessage, (string)Application.Current.Resources["Login_SuccessMessage"], false);
                }
            }
        }

        private void BtnPlayAsGuest_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage(lblFormMessage, "Modo invitado: la pantalla de Menú Principal aún no está construida.", false);
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