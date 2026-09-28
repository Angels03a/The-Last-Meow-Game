using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TLMG.DatabaseModel;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla de registro de cuentas con validación de reglas para nickname, correo y contraseñas.
    /// Integrada con Entity Framework para persistencia en base de datos.
    /// </summary>
    public partial class CreateAccountScreen : LocalizableScreen
    {
        private const string DefaultUserState = "USER_ACTIVE";
        private const string DefaultProfileImage = "/img/default.png";
        private const int DefaultInitialPoints = 0;
        private const int DefaultInitialWins = 0;
        private const int DefaultInitialGamesPlayed = 0;

        private class RegistrationFormData
        {
            public string Nickname { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
            public bool HasAcceptedTerms { get; set; }
        }

        public CreateAccountScreen()
        {
            InitializeComponent();
        }

        protected override void UpdateDynamicLabels()
        {
        }

        private void PwdPassword_PasswordChanged(object eventSender, RoutedEventArgs eventArguments)
        {
            lblPasswordError.Visibility = Visibility.Hidden;
        }

        private void BtnCreateAccount_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            var formData = new RegistrationFormData
            {
                Nickname = txtNickname.Text,
                Email = txtEmail.Text,
                Password = pwdPassword.Password,
                ConfirmPassword = pwdConfirmPassword.Password,
                HasAcceptedTerms = chkTerms.IsChecked == true
            };

            if (AreFieldsValid(formData))
            {
                if (TryRegisterUser(formData))
                {
                    NavigateToVerificationScreen();
                }
            }
        }

        private bool AreFieldsValid(RegistrationFormData data)
        {
            bool areAllFieldsValid;

            if (AreFieldsEmpty(data))
            {
                areAllFieldsValid = false;
            }
            else if (HasFormatErrors(data))
            {
                areAllFieldsValid = false;
            }
            else
            {
                areAllFieldsValid = true;
            }

            return areAllFieldsValid;
        }

        private bool AreFieldsEmpty(RegistrationFormData data)
        {
            bool hasEmptyFields = string.IsNullOrWhiteSpace(data.Nickname) ||
                string.IsNullOrWhiteSpace(data.Email) ||
                string.IsNullOrWhiteSpace(data.Password) ||
                string.IsNullOrWhiteSpace(data.ConfirmPassword);

            if (hasEmptyFields)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_IncompleteFieldsError") as string, true);
            }

            return hasEmptyFields;
        }

        private bool HasFormatErrors(RegistrationFormData data)
        {
            bool hasErrors = false;

            string nicknameValidationResult = ValidateNickname(data.Nickname);
            if (nicknameValidationResult != null)
            {
                ShowStatusMessage(lblNicknameError, nicknameValidationResult, true);
                hasErrors = true;
            }

            string emailValidationResult = ValidateEmailFormat(data.Email);
            if (emailValidationResult != null)
            {
                ShowStatusMessage(lblEmailError, emailValidationResult, true);
                hasErrors = true;
            }

            if (CalculatePasswordStrength(data.Password) < ValidationHelper.RequiredPasswordStrengthScore)
            {
                ShowStatusMessage(lblPasswordError, TryFindResource("CreateAccount_PasswordPolicyError") as string, true);
                hasErrors = true;
            }
            else if (data.Password != data.ConfirmPassword)
            {
                ShowStatusMessage(lblConfirmPasswordError, TryFindResource("CreateAccount_MismatchError") as string, true);
                hasErrors = true;
            }

            if (!data.HasAcceptedTerms)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_TermsError") as string, true);
                hasErrors = true;
            }

            return hasErrors;
        }

        private bool TryRegisterUser(RegistrationFormData data)
        {
            bool isSuccess = false;

            try
            {
                using (var database = new TLMG_DBEntities())
                {
                    bool emailIsTaken = database.User.Any(user => user.email == data.Email);
                    bool nicknameIsTaken = database.User.Any(user => user.nickname == data.Nickname);

                    if (emailIsTaken)
                    {
                        ShowStatusMessage(lblEmailError, TryFindResource("CreateAccount_EmailTakenError") as string, true);
                    }
                    else if (nicknameIsTaken)
                    {
                        ShowStatusMessage(lblNicknameError, TryFindResource("CreateAccount_NicknameTakenError") as string, true);
                    }
                    else
                    {
                        var newUserAccount = new User
                        {
                            email = data.Email,
                            nickname = data.Nickname,
                            password_hash = data.Password, // FIXME: Integrar algoritmo de hashing antes de insertar en BD.
                            state = DefaultUserState,
                            profile_image = DefaultProfileImage,
                            last_time = DateTime.Now
                        };

                        database.User.Add(newUserAccount);
                        database.SaveChanges();

                        var newPlayerProfile = new Player
                        {
                            id_user = newUserAccount.id_user,
                            total_points = DefaultInitialPoints,
                            total_wins = DefaultInitialWins,
                            total_games_played = DefaultInitialGamesPlayed
                        };

                        database.Player.Add(newPlayerProfile);
                        database.SaveChanges();

                        isSuccess = true;
                    }
                }
            }
            catch (DbUpdateException)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_DuplicateEmailError") as string, true);
            }
            catch (SqlException)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_NetworkError") as string, true);
            }
            catch (EntityException)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_DataModelError") as string, true);
            }

            return isSuccess;
        }

        private void NavigateToVerificationScreen()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new VerificationScreen(
                onVerifiedCallbackAction: () => {
                    MessageBox.Show(TryFindResource("CreateAccount_SuccessMessage") as string);
                    applicationMainWindow.ShowScreen(new LoginScreen());
                },
                onBackCallbackAction: () => applicationMainWindow.ShowScreen(new CreateAccountScreen())
            ));
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
            return validationErrorKey != null ? TryFindResource(validationErrorKey) as string : null;
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

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
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