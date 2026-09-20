using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TLMG.Views
{
    public partial class ForgotPasswordScreen : UserControl
    {
        private static readonly string[] PermittedEmailDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };

        public ForgotPasswordScreen()
        {
            InitializeComponent();
        }

        private void TxtEmail_LostFocus(object eventSender, RoutedEventArgs eventArguments)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                lblEmailError.Visibility = Visibility.Collapsed;
            }
            else
            {
                string validationErrorMessage = ValidateEmail(txtEmail.Text);
                if (validationErrorMessage != null)
                {
                    ShowStatusMessage(lblEmailError, validationErrorMessage, true);
                }
                else
                {
                    lblEmailError.Visibility = Visibility.Collapsed;
                }
            }
        }

        private string ValidateEmail(string inputEmailAddress)
        {
            string validationErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(inputEmailAddress))
            {
                if (Regex.IsMatch(inputEmailAddress, @"[%&]"))
                {
                    validationErrorMessage = TryFindResource("ForgotPassword_EmailCharactersError") as string;
                }
                else if (!inputEmailAddress.Contains("@") || !inputEmailAddress.Contains("."))
                {
                    validationErrorMessage = TryFindResource("ForgotPassword_EmailFormatError") as string;
                }
                else
                {
                    string extractedDomainName = inputEmailAddress.Substring(inputEmailAddress.IndexOf('@') + 1).ToLower();
                    bool isDomainValid = false;

                    foreach (string validDomain in PermittedEmailDomains)
                    {
                        if (extractedDomainName == validDomain)
                        {
                            isDomainValid = true;
                            break;
                        }
                    }

                    if (!isDomainValid)
                    {
                        validationErrorMessage = TryFindResource("ForgotPassword_EmailDomainError") as string;
                    }
                }
            }

            return validationErrorMessage;
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
                messageElement.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnSendCode_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            string inputEmailAddress = txtEmail.Text;
            lblEmailError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(inputEmailAddress))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("ForgotPassword_EmailRequiredError") as string, true);
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
                    ShowStatusMessage(lblFormMessage, TryFindResource("ForgotPassword_CodeSentMessage") as string, false);
                    await Task.Delay(1000);

                    MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                    applicationMainWindow.ShowScreen(new VerificationScreen(
                        onVerifiedCallbackAction: () => {
                            applicationMainWindow.ShowScreen(new NewPasswordScreen());
                        },
                        onBackCallbackAction: () => {
                            applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
                        }
                    ));
                }
            }
        }

        private void BtnBackToLogin_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new LoginScreen());
        }
    }
}