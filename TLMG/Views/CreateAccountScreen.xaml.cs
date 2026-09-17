using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TLMG;

namespace TLMG.Views
{
    public partial class CreateAccountScreen : UserControl
    {
        private static readonly string[] _allowedEmailDomains = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };
        private static readonly string[] _takenNicknames = { "admin", "whiskers" }; // TODO: sustituir por consulta real a la BD (CU-02, FA-03) cuando exista esa capa
        private static readonly string[] _offensiveWords = { "idiota", "estupido" }; // TODO: sustituir por un filtro de palabras ofensivas real (RC-N-01) cuando exista
        private static readonly string[] _takenEmails = { "test@gmail.com" }; // TODO: sustituir por consulta real a la BD (CU-02, FA-04) cuando exista esa capa

        public CreateAccountScreen()
        {
            InitializeComponent();
        }

        private void PwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordStrengthDisplay();
        }

        private void UpdatePasswordStrengthDisplay()
        {
            string password = pwdPassword.Password;
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

            string[] labelKeys = { "", "CreateAccount_StrengthVeryWeak", "CreateAccount_StrengthWeak", "CreateAccount_StrengthFair", "CreateAccount_StrengthGood", "CreateAccount_StrengthStrong" };

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

        private string ValidateNickname(string nickname)
        {
            if (nickname.Length < 3 || nickname.Length > 12 || !Regex.IsMatch(nickname, "^[a-zA-Z0-9]+$"))
                return (string)Application.Current.Resources["CreateAccount_NicknameFormatError"];

            string nicknameLower = nickname.ToLower();
            if (_offensiveWords.Any(word => nicknameLower.Contains(word)))
                return (string)Application.Current.Resources["CreateAccount_NicknameOffensiveError"];

            if (_takenNicknames.Contains(nicknameLower))
                return (string)Application.Current.Resources["CreateAccount_NicknameTakenError"];

            return null;
        }

        private string ValidateEmailFormat(string email)
        {
            if (Regex.IsMatch(email, @"[%&]"))
                return (string)Application.Current.Resources["CreateAccount_EmailCharactersError"];

            if (!email.Contains("@") || !email.Contains("."))
                return (string)Application.Current.Resources["CreateAccount_EmailFormatError"];

            string domain = email.Substring(email.IndexOf('@') + 1).ToLower();
            if (!_allowedEmailDomains.Contains(domain))
                return (string)Application.Current.Resources["CreateAccount_EmailDomainError"];

            if (_takenEmails.Contains(email.ToLower()))
                return (string)Application.Current.Resources["CreateAccount_EmailTakenError"];

            return null;
        }

        private void BtnVerifyEmail_Click(object sender, RoutedEventArgs e)
        {
            string nickname = txtNickname.Text;
            string email = txtEmail.Text;
            string password = pwdPassword.Password;

            if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show((string)Application.Current.Resources["CreateAccount_IncompleteFieldsError"]);
                return;
            }

            if (CalculatePasswordStrength(password) < 5)
            {
                MessageBox.Show((string)Application.Current.Resources["CreateAccount_PasswordPolicyError"]);
                return;
            }

            string nicknameError = ValidateNickname(nickname);
            if (nicknameError != null)
            {
                MessageBox.Show(nicknameError);
                return;
            }

            string emailError = ValidateEmailFormat(email);
            if (emailError != null)
            {
                MessageBox.Show(emailError);
                return;
            }

            // TODO: conectar con el servidor y crear el registro de Usuario (estado 'pendiente_verificacion') cuando exista esa capa (CU-02).
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.ShowScreen(new VerificationScreen(
                onVerified: () => {
                    MessageBox.Show((string)Application.Current.Resources["CreateAccount_SuccessMessage"]);
                    mainWindow.ShowScreen(new LoginScreen());
                },
                onBack: () => mainWindow.ShowScreen(new CreateAccountScreen())
            ));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                (string)Application.Current.Resources["CreateAccount_ConfirmAbandonMessage"],
                (string)Application.Current.Resources["CreateAccount_ConfirmAbandonTitle"],
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
            }
        }
    }
}