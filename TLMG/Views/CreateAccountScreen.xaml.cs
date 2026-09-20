using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TLMG.Views
{
    public partial class CreateAccountScreen : UserControl
    {
        private static readonly string[] PermittedEmailDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };
        private static readonly string[] UnavailableNicknames = { "admin", "whiskers" };
        private static readonly string[] OffensiveVocabularyWords = { "idiota", "estupido" };
        private static readonly string[] UnavailableEmailAddresses = { "test@gmail.com" };

        public CreateAccountScreen()
        {
            InitializeComponent();
        }

        private void PwdPassword_PasswordChanged(object eventSender, RoutedEventArgs eventArguments)
        {
            lblPasswordError.Visibility = Visibility.Hidden;
        }

        private int CalculatePasswordStrength(string inputPasswordString)
        {
            int passwordStrengthScore = 0;
            if (inputPasswordString.Length >= 12) passwordStrengthScore++;
            if (Regex.IsMatch(inputPasswordString, "[A-Z]")) passwordStrengthScore++;
            if (Regex.IsMatch(inputPasswordString, "[a-z]")) passwordStrengthScore++;
            if (Regex.IsMatch(inputPasswordString, "[0-9]")) passwordStrengthScore++;
            if (Regex.IsMatch(inputPasswordString, @"[^A-Za-z0-9]")) passwordStrengthScore++;
            return passwordStrengthScore;
        }

        private string ValidateNickname(string inputNicknameString)
        {
            string validationErrorMessage = null;

            if (inputNicknameString.Length < 3 || inputNicknameString.Length > 12 || !Regex.IsMatch(inputNicknameString, "^[a-zA-Z0-9]+$"))
            {
                validationErrorMessage = TryFindResource("CreateAccount_NicknameFormatError") as string;
            }
            else
            {
                string lowercaseNicknameString = inputNicknameString.ToLower();
                if (OffensiveVocabularyWords.Any(offensiveWord => lowercaseNicknameString.Contains(offensiveWord)))
                {
                    validationErrorMessage = TryFindResource("CreateAccount_NicknameOffensiveError") as string;
                }
                else if (UnavailableNicknames.Contains(lowercaseNicknameString))
                {
                    validationErrorMessage = TryFindResource("CreateAccount_NicknameTakenError") as string;
                }
            }

            return validationErrorMessage;
        }

        private string ValidateEmailFormat(string inputEmailAddress)
        {
            string validationErrorMessage = null;

            if (Regex.IsMatch(inputEmailAddress, @"[%&]"))
            {
                validationErrorMessage = TryFindResource("CreateAccount_EmailCharactersError") as string;
            }
            else if (!inputEmailAddress.Contains("@") || !inputEmailAddress.Contains("."))
            {
                validationErrorMessage = TryFindResource("CreateAccount_EmailFormatError") as string;
            }
            else
            {
                string extractedDomainName = inputEmailAddress.Substring(inputEmailAddress.IndexOf('@') + 1).ToLower();
                if (!PermittedEmailDomains.Contains(extractedDomainName))
                {
                    validationErrorMessage = TryFindResource("CreateAccount_EmailDomainError") as string;
                }
                else if (UnavailableEmailAddresses.Contains(inputEmailAddress.ToLower()))
                {
                    validationErrorMessage = TryFindResource("CreateAccount_EmailTakenError") as string;
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
                messageElement.Visibility = Visibility.Hidden;
            }
        }

        private void BtnCreateAccount_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            string inputNicknameString = txtNickname.Text;
            string inputEmailAddress = txtEmail.Text;
            string inputPasswordString = pwdPassword.Password;
            string inputConfirmPasswordString = pwdConfirmPassword.Password;
            bool formHasErrors = false;

            if (string.IsNullOrWhiteSpace(inputNicknameString) || string.IsNullOrWhiteSpace(inputEmailAddress) || string.IsNullOrWhiteSpace(inputPasswordString) || string.IsNullOrWhiteSpace(inputConfirmPasswordString))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_IncompleteFieldsError") as string, true);
                formHasErrors = true;
            }
            else
            {
                string nicknameValidationResult = ValidateNickname(inputNicknameString);
                if (nicknameValidationResult != null)
                {
                    ShowStatusMessage(lblNicknameError, nicknameValidationResult, true);
                    formHasErrors = true;
                }

                string emailValidationResult = ValidateEmailFormat(inputEmailAddress);
                if (emailValidationResult != null)
                {
                    ShowStatusMessage(lblEmailError, emailValidationResult, true);
                    formHasErrors = true;
                }

                if (CalculatePasswordStrength(inputPasswordString) < 5)
                {
                    ShowStatusMessage(lblPasswordError, TryFindResource("CreateAccount_PasswordPolicyError") as string, true);
                    formHasErrors = true;
                }
                else if (inputPasswordString != inputConfirmPasswordString)
                {
                    ShowStatusMessage(lblConfirmPasswordError, TryFindResource("NewPassword_MismatchError") as string, true);
                    formHasErrors = true;
                }

                if (chkTerms.IsChecked != true)
                {
                    ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_TermsError") as string ?? "Debe aceptar los términos y condiciones.", true);
                    formHasErrors = true;
                }
            }

            if (!formHasErrors)
            {
                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ShowScreen(new VerificationScreen(
                    onVerifiedCallbackAction: () => {
                        applicationMainWindow.ShowScreen(new MainMenuScreen());
                    },
                    onBackCallbackAction: () => applicationMainWindow.ShowScreen(new CreateAccountScreen()),
                    showStepsHeader: false
                ));
            }
        }

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MessageBoxResult confirmationResult = MessageBox.Show(
                TryFindResource("CreateAccount_ConfirmAbandonMessage") as string ?? "Are you sure?",
                TryFindResource("CreateAccount_ConfirmAbandonTitle") as string ?? "Confirm",
                MessageBoxButton.YesNo);

            if (confirmationResult == MessageBoxResult.Yes)
            {
                ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
            }
        }

        private void BtnBackToLogin_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}