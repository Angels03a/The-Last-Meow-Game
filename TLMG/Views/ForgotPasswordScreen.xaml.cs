using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla de recuperación de contraseña olvidada mediante envío de código de verificación.
    /// </summary>
    public partial class ForgotPasswordScreen : LocalizableScreen
    {
        private const int ScreenTransitionDelayMilliseconds = 1000;

        public ForgotPasswordScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
        
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
            string errorResourceKey = ValidationHelper.GetEmailValidationErrorKey(inputEmailAddress, "ForgotPassword");
            return errorResourceKey != null ? TryFindResource(errorResourceKey) as string : null;
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
                messageElement.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnSendCode_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            string inputEmailAddress = txtEmail.Text;
            lblEmailError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(inputEmailAddress))
            {
                ShowStatusMessage(
                    lblFormMessage,
                    TryFindResource("ForgotPassword_EmailRequiredError") as string,
                    true);
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
                    ShowStatusMessage(
                        lblFormMessage,
                        TryFindResource("ForgotPassword_CodeSentMessage") as string,
                        false);
                    await Task.Delay(ScreenTransitionDelayMilliseconds);

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