using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    public partial class LoginScreen : UserControl
    {
        private static readonly string[] _allowedDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };

        public LoginScreen()
        {
            InitializeComponent();
        }

        private void TxtEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            string errorMessage = ValidateEmail(txtEmail.Text);
            lblEmailError.Text = errorMessage;
            lblEmailError.Visibility = string.IsNullOrEmpty(errorMessage) ? Visibility.Collapsed : Visibility.Visible;
        }

        private string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (string)Application.Current.Resources["Login_EmailRequiredError"];

            if (Regex.IsMatch(email, @"[%&]"))
                return (string)Application.Current.Resources["Login_EmailCharactersError"];

            if (!email.Contains("@") || !email.Contains("."))
                return (string)Application.Current.Resources["Login_EmailFormatError"];

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
                return (string)Application.Current.Resources["Login_EmailDomainError"];

            return null;
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

        private void BtnLogIn_Click(object sender, RoutedEventArgs e)
        {
            string emailErrorMessage = ValidateEmail(txtEmail.Text);
            string password = pwdPassword.Visibility == Visibility.Visible ? pwdPassword.Password : txtPasswordVisible.Text;

            if (emailErrorMessage != null || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show((string)Application.Current.Resources["Login_IncompleteFieldsError"]);
                return;
            }

            // TODO: conectar con el servidor (TCP) y validar las credenciales reales (CU-03) cuando exista esa capa.
            MessageBox.Show((string)Application.Current.Resources["Login_SuccessMessage"]);
        }
        private void BtnPlayAsGuest_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navegar a GUI-MenúPrincipal (modo invitado) cuando esa pantalla exista.
            MessageBox.Show("Modo invitado: la pantalla de Menú Principal aún no está construida.");
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