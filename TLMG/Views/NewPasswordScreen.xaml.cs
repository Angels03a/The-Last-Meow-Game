using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TLMG;

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
                    bar.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x4A));

                lblStrengthLabel.Visibility = Visibility.Collapsed;
                return;
            }

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
                    : new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x4A));
            }

            lblStrengthLabel.Text = (string)Application.Current.Resources[labelKeys[score]];
            lblStrengthLabel.Foreground = new SolidColorBrush(scoreColor);
            lblStrengthLabel.Visibility = Visibility.Visible;
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

        private void BtnSetPassword_Click(object sender, RoutedEventArgs e)
        {
            string password = pwdNewPassword.Password;
            string confirmPassword = pwdConfirmPassword.Password;

            if (CalculatePasswordStrength(password) < 5)
            {
                MessageBox.Show((string)Application.Current.Resources["NewPassword_PolicyError"]);
                return;
            }

            if (password != confirmPassword)
            {
                lblMismatchError.Text = (string)Application.Current.Resources["NewPassword_MismatchError"];
                lblMismatchError.Visibility = Visibility.Visible;
                return;
            }

            lblMismatchError.Visibility = Visibility.Collapsed;

            // TODO: conectar con el servidor y actualizar Usuario.password_hashed (CU-06) cuando exista esa capa.
            MessageBox.Show((string)Application.Current.Resources["NewPassword_SuccessMessage"]);
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }

        private void BtnBackToForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new ForgotPasswordScreen());
        }
    }
}