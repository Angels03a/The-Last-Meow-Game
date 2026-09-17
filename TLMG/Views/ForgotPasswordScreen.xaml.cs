using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using TLMG;

namespace TLMG.Views
{
    public partial class ForgotPasswordScreen : UserControl
    {
        private static readonly string[] _allowedDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };

        public ForgotPasswordScreen()
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
                return (string)Application.Current.Resources["ForgotPassword_EmailRequiredError"];

            if (Regex.IsMatch(email, @"[%&]"))
                return (string)Application.Current.Resources["ForgotPassword_EmailCharactersError"];

            if (!email.Contains("@") || !email.Contains("."))
                return (string)Application.Current.Resources["ForgotPassword_EmailFormatError"];

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
                return (string)Application.Current.Resources["ForgotPassword_EmailDomainError"];

            return null;
        }

        private void BtnSendCode_Click(object sender, RoutedEventArgs e)
        {
            string errorMessage = ValidateEmail(txtEmail.Text);
            lblEmailError.Text = errorMessage;
            lblEmailError.Visibility = string.IsNullOrEmpty(errorMessage) ? Visibility.Collapsed : Visibility.Visible;

            if (errorMessage != null)
                return;

            MessageBox.Show((string)Application.Current.Resources["ForgotPassword_CodeSentMessage"]);

            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.ShowScreen(new VerificationScreen(
                onVerified: () => mainWindow.ShowScreen(new NewPasswordScreen()),
                onBack: () => mainWindow.ShowScreen(new ForgotPasswordScreen())
            ));
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}