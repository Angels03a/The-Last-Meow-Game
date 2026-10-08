using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de restablecimiento y configuración de una nueva contraseña.
    /// </summary>
    public partial class NewPasswordScreen : LocalizableScreen
    {
        private const int WeakPasswordThresholdScore = 2;
        private const int FairPasswordScore = 3;
        private const int SuccessRedirectDelayMilliseconds = 2000;

        public NewPasswordScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
            UpdatePasswordStrengthDisplay();
        }

        private void PwdNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordStrengthDisplay();
        }

        private void UpdatePasswordStrengthDisplay()
        {
            string currentPasswordText = pwdNewPassword.Password;
            int passwordStrengthScore = CalculatePasswordStrength(currentPasswordText);
            Border[] strengthBarsArray = new[]
            {
                brdStrength1, brdStrength2, brdStrength3, brdStrength4, brdStrength5
            };

            if (string.IsNullOrEmpty(currentPasswordText))
            {
                foreach (Border strengthBorderElement in strengthBarsArray)
                {
                    strengthBorderElement.Background = (Brush)FindResource("Brush_Border");
                }
                lblStrengthLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                string indicatorBrushKey = passwordStrengthScore <= WeakPasswordThresholdScore
                    ? "Brush_StrengthWeak"
                    : passwordStrengthScore == FairPasswordScore
                        ? "Brush_StrengthFair"
                        : "Brush_StrengthStrong";

                Brush indicatorBrush = (Brush)FindResource(indicatorBrushKey);

                string[] strengthLabelResourceKeys =
                {
                    "",
                    "NewPassword_StrengthVeryWeak",
                    "NewPassword_StrengthWeak",
                    "NewPassword_StrengthFair",
                    "NewPassword_StrengthGood",
                    "NewPassword_StrengthStrong"
                };

                for (int collectionIndex = 0; collectionIndex < strengthBarsArray.Length; collectionIndex++)
                {
                    strengthBarsArray[collectionIndex].Background = collectionIndex < passwordStrengthScore
                        ? indicatorBrush
                        : (Brush)FindResource("Brush_Border");
                }

                lblStrengthLabel.Text =
                    TryFindResource(strengthLabelResourceKeys[passwordStrengthScore]) as string
                    ?? strengthLabelResourceKeys[passwordStrengthScore];
                lblStrengthLabel.Foreground = indicatorBrush;
                lblStrengthLabel.Visibility = Visibility.Visible;
            }
        }

        private int CalculatePasswordStrength(string inputPasswordText)
        {
            return ValidationHelper.CalculatePasswordStrength(inputPasswordText);
        }

        private async void BtnSetPassword_Click(object sender, RoutedEventArgs e)
        {
            string inputNewPassword = pwdNewPassword.Password;
            string inputConfirmPassword = pwdConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(inputNewPassword) || string.IsNullOrWhiteSpace(inputConfirmPassword))
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("NewPassword_IncompleteFieldsError") as string, true);
            }
            else if (CalculatePasswordStrength(inputNewPassword) < ValidationHelper.RequiredPasswordStrengthScore)
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

                await Task.Delay(SuccessRedirectDelayMilliseconds);

                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ShowScreen(new LoginScreen());
            }
        }

        private void BtnBackToForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new ForgotPasswordScreen());
        }
    }
}