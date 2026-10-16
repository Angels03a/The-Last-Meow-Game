using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

using log4net;

using TLMG.DataAccess.DatabaseModel;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de registro de cuentas con validación de reglas para nickname, correo y contraseñas.
    /// Integrada con Entity Framework para persistencia en base de datos.
    /// </summary>
    public partial class CreateAccountScreen : LocalizableScreen
    {
        // FIXME: Usar estado de usuario definido en la base de datos.
        private const string DefaultUserState = "USER_ACTIVE";

        // FIXME: Usar imagen de perfil por defecto definida.
        private const string DefaultProfileImage = "/img/default.png";
        private const int DefaultInitialPoints = 0;
        private const int DefaultInitialWins = 0;
        private const int DefaultInitialGamesPlayed = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(CreateAccountScreen));

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountScreen"/>.
        /// </summary>
        public CreateAccountScreen()
        {
            InitializeComponent();
        }

        private void PwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            lblPasswordError.Visibility = Visibility.Hidden;
        }

        private void BtnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            var formDataForRegistration = new RegistrationFormData
            {
                Nickname = txtNickname.Text,
                Email = txtEmail.Text,
                Password = pwdPassword.Password,
                ConfirmPassword = pwdConfirmPassword.Password,
                HasAcceptedTerms = chkTerms.IsChecked == true
            };

            if (AreFieldsValid(formDataForRegistration))
            {
                if (TryRegisterUser(formDataForRegistration))
                {
                    NavigateToVerificationScreen();
                }
            }
        }

        private bool AreFieldsValid(RegistrationFormData formDataForRegistration)
        {
            bool areAllFieldsValid;

            if (AreFieldsEmpty(formDataForRegistration))
            {
                areAllFieldsValid = false;
            }
            else if (HasFormatErrors(formDataForRegistration))
            {
                areAllFieldsValid = false;
            }
            else
            {
                areAllFieldsValid = true;
            }

            return areAllFieldsValid;
        }

        private bool AreFieldsEmpty(RegistrationFormData formDataForRegistration)
        {
            bool hasEmptyFields = string.IsNullOrWhiteSpace(formDataForRegistration.Nickname) ||
                string.IsNullOrWhiteSpace(formDataForRegistration.Email) ||
                string.IsNullOrWhiteSpace(formDataForRegistration.Password) ||
                string.IsNullOrWhiteSpace(formDataForRegistration.ConfirmPassword);

            if (hasEmptyFields)
            {
                ShowStatusMessage(
                    lblFormMessage,
                    TryFindResource("CreateAccount_IncompleteFieldsError") as string,
                    true);
            }

            return hasEmptyFields;
        }

        private bool HasFormatErrors(RegistrationFormData formDataForRegistration)
        {
            bool hasErrors = false;

            string nicknameValidationResult = ValidateNickname(formDataForRegistration.Nickname);
            if (nicknameValidationResult != null)
            {
                ShowStatusMessage(lblNicknameError, nicknameValidationResult, true);
                hasErrors = true;
            }

            string emailValidationResult = ValidateEmailFormat(formDataForRegistration.Email);
            if (emailValidationResult != null)
            {
                ShowStatusMessage(lblEmailError, emailValidationResult, true);
                hasErrors = true;
            }

            if (CalculatePasswordStrength(formDataForRegistration.Password)
                < ValidationHelper.RequiredPasswordStrengthScore)
            {
                ShowStatusMessage(
                    lblPasswordError,
                    TryFindResource("CreateAccount_PasswordPolicyError") as string,
                    true);
                hasErrors = true;
            }
            else if (formDataForRegistration.Password != formDataForRegistration.ConfirmPassword)
            {
                ShowStatusMessage(
                    lblConfirmPasswordError,
                    TryFindResource("CreateAccount_MismatchError") as string,
                    true);
                hasErrors = true;
            }

            if (!formDataForRegistration.HasAcceptedTerms)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_TermsError") as string, true);
                hasErrors = true;
            }

            return hasErrors;
        }

        private bool TryRegisterUser(RegistrationFormData formDataForRegistration)
        {
            bool isSuccess = false;

            try
            {
                using (var database = new TLMG_DBEntities())
                {
                    bool emailIsTaken = database.User.Any(user => user.email == formDataForRegistration.Email);
                    bool nicknameIsTaken = database.User.Any(user => user.nickname == formDataForRegistration.Nickname);

                    if (emailIsTaken)
                    {
                        ShowStatusMessage(
                            lblEmailError,
                            TryFindResource("CreateAccount_EmailTakenError") as string,
                            true);
                    }
                    else if (nicknameIsTaken)
                    {
                        ShowStatusMessage(
                            lblNicknameError,
                            TryFindResource("CreateAccount_NicknameTakenError") as string,
                            true);
                    }
                    else
                    {
                        InsertUserWithPlayerProfile(database, formDataForRegistration);

                        isSuccess = true;
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_DuplicateDataError") as string, true);
                _log.Error("Fallo al guardar el registro en la base de datos.", ex);
            }
            catch (SqlException ex)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_NetworkError") as string, true);
                _log.Error("Fallo en la conexión con la base de datos.", ex);
            }
            catch (EntityException ex)
            {
                ShowStatusMessage(lblFormMessage, TryFindResource("CreateAccount_DataModelError") as string, true);
                _log.Error("Fallo al interactuar con el modelo de datos de la base de datos.", ex);
            }

            return isSuccess;
        }

        private void InsertUserWithPlayerProfile(
            TLMG_DBEntities database,
            RegistrationFormData formDataForRegistration)
        {
            var newUserAccount = new User
            {
                email = formDataForRegistration.Email,
                nickname = formDataForRegistration.Nickname,
                // FIXME: Integrar algoritmo de hashing antes de insertar en BD.
                password_hash = formDataForRegistration.Password,
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
        }

        private void NavigateToVerificationScreen()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new VerificationScreen(
                onVerifiedCallbackAction: HandleVerificationCompleted,
                onBackCallbackAction: () => applicationMainWindow.ShowScreen(new CreateAccountScreen())
            ));
        }

        private void HandleVerificationCompleted()
        {
            MessageBox.Show(TryFindResource("CreateAccount_SuccessMessage") as string);
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new LoginScreen());
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

        private void BtnBack_Click(object sender, RoutedEventArgs e)
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

        private class RegistrationFormData
        {
            /// <summary>
            /// Obtiene o establece el apodo o nombre del usuario.
            /// </summary>
            public string Nickname { get; set; }

            /// <summary>
            /// Obtiene o establece el correo electrónico del usuario.
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// Obtiene o establece la contraseña en texto plano ingresada.
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Obtiene o establece la confirmación de la contraseña.
            /// </summary>
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// Obtiene o establece un valor que indica si el usuario aceptó los términos.
            /// </summary>
            public bool HasAcceptedTerms { get; set; }
        }
    }
}