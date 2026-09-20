using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TLMG.Views
{
    public partial class NewPasswordScreen : UserControl
    {
        public NewPasswordScreen()
        {
            InitializeComponent();
        }

        private void PwdNewPassword_PasswordChanged(object eventSender, RoutedEventArgs eventArguments)
        {
            UpdatePasswordStrengthDisplay();
        }

        private void UpdatePasswordStrengthDisplay()
        {
            string currentPasswordText = pwdNewPassword.Password;
            int passwordStrengthScore = CalculatePasswordStrength(currentPasswordText);
            var strengthBarsArray = new[] { BorderStrength1, BorderStrength2, BorderStrength3, BorderStrength4, BorderStrength5 };

            if (currentPasswordText.Length == 0)
            {
                foreach (var strengthBorderElement in strengthBarsArray)
                {
                    strengthBorderElement.Background = (Brush)FindResource("Brush_Border");
                }
                lblStrengthLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                Color indicatorColor = passwordStrengthScore <= 2
                    ? Color.FromRgb(0xEF, 0x44, 0x44)
                    : passwordStrengthScore == 3
                        ? Color.FromRgb(0xF5, 0x9E, 0x0B)
                        : Color.FromRgb(0x10, 0xB9, 0x81);

                string[] strengthLabelResourceKeys = { "", "NewPassword_StrengthVeryWeak", "NewPassword_StrengthWeak", "NewPassword_StrengthFair", "NewPassword_StrengthGood", "NewPassword_StrengthStrong" };

                for (int collectionIndex = 0; collectionIndex < strengthBarsArray.Length; collectionIndex++)
                {
                    strengthBarsArray[collectionIndex].Background = collectionIndex < passwordStrengthScore
                        ? new SolidColorBrush(indicatorColor)
                        : (Brush)FindResource("Brush_Border");
                }

                lblStrengthLabel.Text = TryFindResource(strengthLabelResourceKeys[passwordStrengthScore]) as string ?? strengthLabelResourceKeys[passwordStrengthScore];
                lblStrengthLabel.Foreground = new SolidColorBrush(indicatorColor);
                lblStrengthLabel.Visibility = Visibility.Visible;
            }
        }

        private int CalculatePasswordStrength(string inputPasswordText)
        {
            int calculatedStrengthScore = 0;
            if (inputPasswordText.Length >= 12) calculatedStrengthScore++;
            if (Regex.IsMatch(inputPasswordText, "[A-Z]")) calculatedStrengthScore++;
            if (Regex.IsMatch(inputPasswordText, "[a-z]")) calculatedStrengthScore++;
            if (Regex.IsMatch(inputPasswordText, "[0-9]")) calculatedStrengthScore++;
            if (Regex.IsMatch(inputPasswordText, @"[^A-Za-z0-9]")) calculatedStrengthScore++;
            return calculatedStrengthScore;
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

        private async void BtnSetPassword_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            string inputNewPassword = pwdNewPassword.Password;
            string inputConfirmPassword = pwdConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(inputNewPassword) || string.IsNullOrWhiteSpace(inputConfirmPassword))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("Login_IncompleteFieldsError") as string, true);
            }
            else if (CalculatePasswordStrength(inputNewPassword) < 5)
            {
                ShowStatusMessage(lblPasswordError, TryFindResource("NewPassword_PolicyError") as string, true);
            }
            else if (inputNewPassword != inputConfirmPassword)
            {
                ShowStatusMessage(lblMismatchError, TryFindResource("NewPassword_MismatchError") as string, true);
            }
            else
            {
                lblFormMessage.Text = TryFindResource("NewPassword_SuccessMessage") as string;
                lblFormMessage.Foreground = (Brush)FindResource("Brush_Accent");
                lblFormMessage.Visibility = Visibility.Visible;

                await Task.Delay(2000);

                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ShowScreen(new LoginScreen());
            }
        }

        private void BtnBackToForgotPassword_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
        }
    }
}