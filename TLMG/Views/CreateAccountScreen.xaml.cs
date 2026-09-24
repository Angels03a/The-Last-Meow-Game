using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using TLMG;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla de registro de cuentas con validación de reglas para nickname, correo y contraseñas.
    /// </summary>
    public partial class CreateAccountScreen : UserControl
    {
        private static readonly string[] _unavailableEmailAddresses = { "test@gmail.com" };

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountScreen"/>.
        /// </summary>
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
            return ValidationHelper.CalculatePasswordStrength(inputPasswordString);
        }

        private string ValidateNickname(string inputNicknameString)
        {
            string errorResourceKey = ValidationHelper.GetNicknameValidationErrorKey(inputNicknameString);
            return errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
        }

        private string ValidateEmailFormat(string inputEmailAddress)
        {
            string validationErrorKey = ValidationHelper.GetEmailValidationErrorKey(inputEmailAddress, "CreateAccount");

            if (validationErrorKey != null)
            {
                return TryFindResource(validationErrorKey) as string;
            }

            if (_unavailableEmailAddresses.Contains(inputEmailAddress.ToLower()))
            {
                return TryFindResource("CreateAccount_EmailTakenError") as string;
            }

            return null;
        }

        private async void ShowStatusMessage(
            TextBlock messageElement,
            string messageContent,
            bool isErrorMessage = true)
        {
            messageElement.Text = messageContent;
            messageElement.Foreground = isErrorMessage
                ? (Brush)FindResource("Brush_Error")
                : (Brush)FindResource("Brush_Accent");
            messageElement.Visibility = Visibility.Visible;

            await Task.Delay(ValidationHelper.StatusMessageDurationMilliseconds);

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

            bool hasEmptyNickname = string.IsNullOrWhiteSpace(inputNicknameString);
            bool hasEmptyEmail = string.IsNullOrWhiteSpace(inputEmailAddress);
            bool hasEmptyPassword = string.IsNullOrWhiteSpace(inputPasswordString);
            bool hasEmptyConfirmPassword = string.IsNullOrWhiteSpace(inputConfirmPasswordString);

            if (hasEmptyNickname || hasEmptyEmail || hasEmptyPassword || hasEmptyConfirmPassword)
            {
                ShowStatusMessage(
                    lblFormMessage,
                    TryFindResource("CreateAccount_IncompleteFieldsError") as string,
                    true);
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

                if (CalculatePasswordStrength(inputPasswordString) < ValidationHelper.RequiredPasswordStrengthScore)
                {
                    ShowStatusMessage(
                        lblPasswordError,
                        TryFindResource("CreateAccount_PasswordPolicyError") as string,
                        true);
                    formHasErrors = true;
                }
                else if (inputPasswordString != inputConfirmPasswordString)
                {
                    ShowStatusMessage(
                        lblConfirmPasswordError,
                        TryFindResource("CreateAccount_MismatchError") as string,
                        true);
                    formHasErrors = true;
                }

                if (chkTerms.IsChecked != true)
                {
                    ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_TermsError") as string, true);
                    formHasErrors = true;
                }
            }

            if (!formHasErrors)
            {
                // TODO: conectar con el servidor y crear el registro de Usuario cuando exista esa capa (CU-02).
                MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
                applicationMainWindow.ShowScreen(new VerificationScreen(
                    onVerifiedCallbackAction: () => {
                        MessageBox.Show(TryFindResource("CreateAccount_SuccessMessage") as string);
                        applicationMainWindow.ShowScreen(new LoginScreen());
                    },
                    onBackCallbackAction: () => applicationMainWindow.ShowScreen(new CreateAccountScreen()),
                    showStepsHeader: false
                ));
            }
        }

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            // FA-01 de CU-02: confirmar antes de abandonar el registro.
            MessageBoxResult confirmationResult = MessageBox.Show(
                TryFindResource("CreateAccount_ConfirmAbandonMessage") as string,
                TryFindResource("CreateAccount_ConfirmAbandonTitle") as string,
                MessageBoxButton.YesNo);

            if (confirmationResult == MessageBoxResult.Yes)
            {
                ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
            }
        }
    }
}