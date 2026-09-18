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

        private void PwdNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordStrengthDisplay();
        }

        private void UpdatePasswordStrengthDisplay()
        {
            string password = pwdNewPassword.Password;
            int score = CalculatePasswordStrength(password);
            var bars = new[] { rectStrength1, rectStrength2, rectStrength3, rectStrength4, rectStrength5 };

            if (password.Length == 0)
            {
                foreach (var bar in bars)
                {
                    bar.Background = (Brush)FindResource("Brush_Border");
                }
                lblStrengthLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                Color scoreColor = score <= 2
                    ? Color.FromRgb(0xEF, 0x44, 0x44)
                    : score == 3
                        ? Color.FromRgb(0xF5, 0x9E, 0x0B)
                        : Color.FromRgb(0x10, 0xB9, 0x81);

                string[] labelKeys = { "", "NewPassword_StrengthVeryWeak", "NewPassword_StrengthWeak", "NewPassword_StrengthFair", "NewPassword_StrengthGood", "NewPassword_StrengthStrong" };

                for (int i = 0; i < bars.Length; i++)
                {
                    bars[i].Background = i < score
                        ? new SolidColorBrush(scoreColor)
                        : (Brush)FindResource("Brush_Border");
                }

                lblStrengthLabel.Text = (string)Application.Current.Resources[labelKeys[score]];
                lblStrengthLabel.Foreground = new SolidColorBrush(scoreColor);
                lblStrengthLabel.Visibility = Visibility.Visible;
            }
        }

        private int CalculatePasswordStrength(string password)
        {
            int score = 0;
            if (password.Length >= 12) score++;
            if (Regex.IsMatch(password, "[A-Z]")) score++;
            if (Regex.IsMatch(password, "[a-z]")) score++;
            if (Regex.IsMatch(password, "[0-9]")) score++;
            if (Regex.IsMatch(password, @"[^A-Za-z0-9]")) score++;
            return score;
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

        private async void BtnSetPassword_Click(object sender, RoutedEventArgs e)
        {
            string password = pwdNewPassword.Password;
            string confirmPassword = pwdConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowStatusMessage(lblFormMessage, (string)Application.Current.Resources["Login_IncompleteFieldsError"], true);
            }
            else if (CalculatePasswordStrength(password) < 5)
            {
                ShowStatusMessage(lblPasswordError, (string)Application.Current.Resources["NewPassword_PolicyError"], true);
            }
            else if (password != confirmPassword)
            {
                ShowStatusMessage(lblMismatchError, (string)Application.Current.Resources["NewPassword_MismatchError"], true);
            }
            else
            {
                lblFormMessage.Text = (string)Application.Current.Resources["NewPassword_SuccessMessage"];
                lblFormMessage.Foreground = (Brush)FindResource("Brush_Accent");
                lblFormMessage.Visibility = Visibility.Visible;

                await Task.Delay(2000);

                ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
            }
        }

        private void BtnBackToForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new ForgotPasswordScreen());
        }
    }
}