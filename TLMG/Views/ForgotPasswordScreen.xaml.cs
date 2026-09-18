using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                lblEmailError.Visibility = Visibility.Collapsed;
            }
            else
            {
                string errorMessage = ValidateEmail(txtEmail.Text);
                if (errorMessage != null)
                {
                    ShowStatusMessage(lblEmailError, errorMessage, true);
                }
                else
                {
                    lblEmailError.Visibility = Visibility.Collapsed;
                }
            }
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

        private void BtnSendCode_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            lblEmailError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(email))
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
                    // TODO: Conectar con el servidor para enviar código (CU-04)
                    ShowStatusMessage(lblFormMessage, "Código enviado al correo.", false);
                }
            }
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}